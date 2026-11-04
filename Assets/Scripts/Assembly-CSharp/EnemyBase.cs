using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AttachableTarget))]
[RequireComponent(typeof(Health))]
public class EnemyBase : MonoBehaviour
{
	public enum EnemyState
	{
		Idle = 0,
		Chasing = 1,
		Attacking = 2,
		Dead = 3
	}

	[Header("Configuration")]
	[SerializeField]
	private EnemyData enemyData;

	[SerializeField]
	private LayerMask unitLayer;

	[SerializeField]
	private LayerMask playerLayer;

	[SerializeField]
	private GameObject lootPrefab;

	[Header("Combat Feel")]
	[SerializeField, Range(0f, 1f)] private float firstStrikeCharge = 0.7f;
	[SerializeField] private float backstabMultiplier = 3f;
	[SerializeField] private float stunnedDamageMultiplier = 2f;

	[Header("Shake-off")]
	[SerializeField] private bool canShakeOff = true;
	[SerializeField] private float shakeOffDelay = 5f;
	[SerializeField] private float shakeOffTelegraph = 1.5f;
	[SerializeField] private float throwDamage = 15f;
	[SerializeField] private float knockbackDistance = 4f;
	[SerializeField] private float troopStunTime = 2.5f;
	[SerializeField] private Color telegraphColor = new Color(1f, 0.9f, 0.2f);

	[Header("Debug")]
	[SerializeField]
	private EnemyState currentState;

	private Health _health;

	private NavMeshAgent _navAgent;

	private AttachableTarget _myAttachableSelf;

	private Transform _currentTarget;

	private float _attackTimer;

	private float _scanTimer;

	private Vector3 _startPos;

	private float _attachedTimer;
	private float _telegraphTimer = -1f;
	private float _stunnedUntil;
	private bool _wasStunned;
	private Renderer[] _renderers;
	private MaterialPropertyBlock _mpb;

	private const float SCAN_RATE = 0.5f;

	private void Awake()
	{
		_health = GetComponent<Health>();
		_navAgent = GetComponent<NavMeshAgent>();
		_myAttachableSelf = GetComponent<AttachableTarget>();
		_startPos = base.transform.position;
		_renderers = GetComponentsInChildren<Renderer>();
		_mpb = new MaterialPropertyBlock();
		if (enemyData != null)
		{
			_health.Initialize(enemyData.maxHP);
			_navAgent.speed = enemyData.moveSpeed;
		}
		_health.OnDeath.AddListener(Die);
	}

	private void Update()
	{
		if (currentState == EnemyState.Dead) return;
		if (IsStunned) return;
		if (_wasStunned) { _wasStunned = false; _navAgent.isStopped = false; }
		HandleStateMachine();
		HandleShakeOff();
	}

	private void HandleStateMachine()
	{
		switch (currentState)
		{
		case EnemyState.Idle:
			HandleIdleState();
			break;
		case EnemyState.Chasing:
			HandleChasingState();
			break;
		case EnemyState.Attacking:
			HandleAttackingState();
			break;
		}
	}

	private void HandleIdleState()
	{
		Patrol();
		HandleTargetScan();
	}

	private void HandleTargetScan()
	{
		_scanTimer += Time.deltaTime;
		if (_scanTimer > 0.5f)
		{
			_scanTimer = 0f;
			if (FindClosestTarget())
			{
				TransitionToState(EnemyState.Chasing);
			}
		}
	}

	private void HandleChasingState()
	{
		HandleTargetScan();
		if (!IsTargetStillValid(_currentTarget))
		{
			TransitionToState(EnemyState.Idle);
			return;
		}
		if (Vector3.Distance(base.transform.position, _currentTarget.position) <= enemyData.attackRange)
		{
			TransitionToState(EnemyState.Attacking);
			return;
		}
		_navAgent.isStopped = false;
		_navAgent.SetDestination(_currentTarget.position);
	}

	private void HandleAttackingState()
	{
		if (!IsTargetStillValid(_currentTarget))
		{
			TransitionToState(EnemyState.Idle);
			return;
		}
		if (Vector3.Distance(base.transform.position, _currentTarget.position) > enemyData.attackRange + 0.5f)
		{
			TransitionToState(EnemyState.Chasing);
			return;
		}
		_navAgent.isStopped = true;
		Vector3 normalized = (_currentTarget.position - base.transform.position).normalized;
		normalized.y = 0f;
		if (normalized != Vector3.zero)
		{
			base.transform.rotation = Quaternion.Slerp(base.transform.rotation, Quaternion.LookRotation(normalized), 10f * Time.deltaTime);
		}
		_attackTimer += Time.deltaTime;
		if (_attackTimer > enemyData.attackInterval)
		{
			_attackTimer = 0f;
			PerformAttack();
		}
	}

	private void TransitionToState(EnemyState newState)
	{
		currentState = newState;
		if (newState == EnemyState.Attacking)
		{
			_attackTimer = enemyData.attackInterval * firstStrikeCharge;
		}
		if (newState == EnemyState.Idle)
		{
			_navAgent.isStopped = false;
		}
	}

	public bool IsStunned => Time.time < _stunnedUntil;
	public bool IsUnaware => currentState == EnemyState.Idle;

	public void Stun(float seconds)
	{
		if (currentState == EnemyState.Dead) return;
		_stunnedUntil = Time.time + seconds;
		_wasStunned = true;
		_telegraphTimer = -1f;
		EndGlow();
		if (_navAgent.isOnNavMesh) _navAgent.isStopped = true;
	}

	public float GetDamageMultiplierFrom(Vector3 attackerPos)
	{
		float m = 1f;
		if (IsStunned) m *= stunnedDamageMultiplier;
		if (IsUnaware)
		{
			Vector3 to = attackerPos - transform.position;
			to.y = 0f;
			if (to.sqrMagnitude > 0.01f && Vector3.Dot(transform.forward, to.normalized) < -0.3f)
				m *= backstabMultiplier;
		}
		return m;
	}

	private void HandleShakeOff()
	{
		if (!canShakeOff) return;
		if (_telegraphTimer >= 0f)
		{
			_telegraphTimer -= Time.deltaTime;
			float k = (Mathf.Sin(Time.time * 18f) + 1f) * 0.5f;
			SetGlow(Color.Lerp(Color.white, telegraphColor, k));
			if (_telegraphTimer < 0f)
			{
				EndGlow();
				PerformShakeOff();
				_attachedTimer = 0f;
			}
			return;
		}
		if (_myAttachableSelf.GetAllAttachedUnits().Count > 0)
		{
			_attachedTimer += Time.deltaTime;
			if (_attachedTimer >= shakeOffDelay) _telegraphTimer = shakeOffTelegraph;
		}
		else _attachedTimer = 0f;
	}

	private void PerformShakeOff()
	{
		foreach (UnitAgent troop in _myAttachableSelf.GetAllAttachedUnits())
		{
			if (troop == null) continue;
			Vector3 dir = troop.transform.position - transform.position;
			dir.y = 0f;
			if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
			troop.GetThrown(dir.normalized, throwDamage, knockbackDistance, troopStunTime, transform);
		}
	}

	private void SetGlow(Color c)
	{
		_mpb.SetColor("_BaseColor", c);
		_mpb.SetColor("_Color", c);
		foreach (Renderer r in _renderers) if (r != null) r.SetPropertyBlock(_mpb);
	}

	private void EndGlow()
	{
		foreach (Renderer r in _renderers) if (r != null) r.SetPropertyBlock(null);
	}

	private void Patrol()
	{
		if (!_navAgent.pathPending && _navAgent.remainingDistance < 0.5f)
		{
			SetRandomPatrolPoint();
		}
	}

	private bool FindClosestTarget()
	{
		if (_myAttachableSelf.AttachedUnitCount > 0)
		{
			UnitAgent priorityAttachedUnit = _myAttachableSelf.GetPriorityAttachedUnit();
			if (priorityAttachedUnit != null)
			{
				_currentTarget = priorityAttachedUnit.transform;
				return true;
			}
		}
		float num = float.MaxValue;
		Collider collider = null;
		Collider[] array = Physics.OverlapSphere(base.transform.position, enemyData.aggroRange, unitLayer);
		foreach (Collider collider2 in array)
		{
			float num2 = Vector3.Distance(base.transform.position, collider2.transform.position);
			if (num2 < num)
			{
				UnitAgent component = collider2.GetComponent<UnitAgent>();
				if (!(component == null) && component.CurrentStateEnum != UnitState.Dead)
				{
					num = num2;
					collider = collider2;
				}
			}
		}
		array = Physics.OverlapSphere(base.transform.position, enemyData.aggroRange, playerLayer);
		foreach (Collider collider3 in array)
		{
			float num3 = Vector3.Distance(base.transform.position, collider3.transform.position);
			if (num3 < num)
			{
				Health component2 = collider3.GetComponent<Health>();
				if (!(component2 == null) && !component2.IsDead)
				{
					num = num3;
					collider = collider3;
				}
			}
		}
		if (collider != null)
		{
			_currentTarget = collider.transform;
			return true;
		}
		return false;
	}

	private void SetRandomPatrolPoint()
	{
		if (NavMesh.SamplePosition(Random.insideUnitSphere * enemyData.patrolRadius + _startPos, out var hit, enemyData.patrolRadius, 1))
		{
			_navAgent.SetDestination(hit.position);
		}
	}

	private void PerformAttack()
	{
		if (_currentTarget == null)
		{
			return;
		}
		Health component = _currentTarget.GetComponent<Health>();
		TargetableEntity component2 = _currentTarget.GetComponent<TargetableEntity>();
		TargetTeam targetTeam = ((component2 != null) ? component2.Team : TargetTeam.Friendly);
		if (!CombatValidation.IsValidTarget(component, TargetTeam.Enemy, targetTeam))
		{
			_currentTarget = null;
			return;
		}
		Debug.Log("Enemy hits " + _currentTarget.name + "!");
		UnitAgent component3 = _currentTarget.GetComponent<UnitAgent>();
		if (component3 != null)
		{
			component3.ReceiveAggro(_myAttachableSelf);
		}
		DamageInfo info = new DamageInfo
		{
			amount = enemyData.attackDamage,
			attackerReference = base.transform,
			type = DamageType.Normal
		};
		component.TakeDamage(info);
	}

	private bool IsTargetStillValid(Transform targetTransform)
	{
		if (targetTransform == null || !targetTransform.gameObject.activeInHierarchy)
		{
			return false;
		}
		Health component = targetTransform.GetComponent<Health>();
		if (component != null)
		{
			return !component.IsDead;
		}
		return false;
	}

	private void Die()
	{
		currentState = EnemyState.Dead;
		_navAgent.isStopped = true;
		_myAttachableSelf.EjectAllUnits();
		if (lootPrefab != null)
		{
			Object.Instantiate(lootPrefab, base.transform.position, Quaternion.identity);
		}
		Object.Destroy(base.gameObject, 0.5f);
	}

	private void OnDestroy()
	{
		_health.OnDeath.RemoveListener(Die);
	}

	private void OnDrawGizmosSelected()
	{
		if (enemyData != null)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(base.transform.position, enemyData.aggroRange);
		}
	}
}
