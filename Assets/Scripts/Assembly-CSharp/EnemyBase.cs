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

	[Header("Awareness / Direction")]
	[Tooltip("Dot product above this = attacker is in front.")]
	[SerializeField] private float frontDotThreshold = 0.3f;
	[Tooltip("Dot product below this = attacker is behind.")]
	[SerializeField] private float backDotThreshold = -0.3f;

	[Header("Frontal Defense")]
	[SerializeField] private bool canCounter = true;
	[Tooltip("Troops engaged at once needed to overwhelm the counter.")]
	[SerializeField] private int overwhelmCount = 3;
	[Tooltip("After a counter, further frontal attempts get through for this long.")]
	[SerializeField] private float counterCooldown = 1.5f;
	[SerializeField] private float counterDamage = 5f;
	[SerializeField] private float counterKnockback = 2.5f;
	[SerializeField] private float counterStun = 1f;

	private float _nextCounterTime;
	public string LastDefense { get; private set; } = "";
	public float LastDefenseTime { get; private set; } = -999f;

	[Header("Player Attachment")]
	[Tooltip("Damage multiplier when the player has no free attach point.")]
	[SerializeField] private float unattachedDamageMultiplier = 0.5f;
	private int _pointIndex = -1;
	private UnitAgent _reservedTroop;

	[Header("Stealth")]
	[Tooltip("0 = off. After being hit while unaware, the enemy turns on its attacker after this many seconds.")]
	[SerializeField] private float alertAfterHit = 0f;
	private float _alertAt = -1f;

	[Header("Test")]
	[Tooltip("Test lanes only: enemy never targets the player, only troops.")]
	[SerializeField] private bool ignorePlayer;
	public void SetIgnorePlayer(bool v) { ignorePlayer = v; }

	[Header("Shake-off")]
	[SerializeField] private bool canShakeOff = true;
	[SerializeField] private float shakeOffDelay = 5f;
	[SerializeField] private float shakeOffTelegraph = 1.5f;
	[SerializeField] private float throwDamage = 15f;
	[SerializeField] private float knockbackDistance = 4f;
	[SerializeField] private float troopStunTime = 2.5f;
	[SerializeField] private Color telegraphColor = new Color(1f, 0.9f, 0.2f);

	[Header("Target Priority")]
	[SerializeField] private float retargetCooldown = 1.5f;

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
	private float _interruptedUntil;
	private bool _wasInterrupted;
	private Renderer[] _renderers;
	private MaterialPropertyBlock _mpb;

	private Transform _lastAttacker;
	private float _lastRetargetTime = -999f;

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
		_health.OnDamaged += HandleDamaged;
	}

	private void Update()
	{
		if (currentState == EnemyState.Dead) return;
		if (IsStunned) return;
		if (IsInterrupted) return;
		if (_wasInterrupted)
		{
			_wasInterrupted = false;
			if (_navAgent.isOnNavMesh) _navAgent.isStopped = false;
		}
		if (_wasStunned)
		{
			_wasStunned = false;
			_navAgent.isStopped = false;
			_attachedTimer = 0f;
			if (CombatDebug.Enabled) Debug.Log($"[Stun] {name} recovered (attached={AttachedCount})");
		}
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
		if (_alertAt >= 0f && Time.time >= _alertAt)
		{
			_alertAt = -1f;
			if (_lastAttacker != null && IsTargetStillValid(_lastAttacker) && AssignTarget(_lastAttacker))
			{
				TransitionToState(EnemyState.Chasing);
				return;
			}
		}
		Patrol();
		HandleTargetScan();
	}

	private PlayerAttachment GetPlayerAttach(Transform t)
	{
		return t != null ? t.GetComponentInParent<PlayerAttachment>() : null;
	}

	private void ReleasePoint()
	{
		if (_pointIndex >= 0)
		{
			if (PlayerAttachment.Instance != null) PlayerAttachment.Instance.Release(this);
			_pointIndex = -1;
		}
	}

	private bool AssignTarget(Transform t)
	{
		UnitAgent troop = t != null ? t.GetComponent<UnitAgent>() : null;
		if (troop != null && troop != _reservedTroop && !troop.TryReserveAttacker(this)) return false;
		if (_reservedTroop != null && _reservedTroop != troop) _reservedTroop.ReleaseAttacker(this);
		_reservedTroop = troop;
		ReleasePoint();
		_currentTarget = t;
		return true;
	}

	private void ReleaseTroop()
	{
		if (_reservedTroop != null) _reservedTroop.ReleaseAttacker(this);
		_reservedTroop = null;
	}

	private void HandleTargetScan()
	{
		if (enemyData.passive) return;
		_scanTimer += Time.deltaTime;
		if (_scanTimer <= SCAN_RATE) return;
		_scanTimer = 0f;

		Transform best = ChooseTarget();
		if (best == null) return;

		if (best != _currentTarget)
		{
			bool current = _currentTarget != null && IsTargetStillValid(_currentTarget);
			if (current && Time.time - _lastRetargetTime < retargetCooldown) return;
			if (!AssignTarget(best)) return;
			_lastRetargetTime = Time.time;
			if (CombatDebug.Enabled) Debug.Log("[Enemy] " + name + " target -> " + best.name);
			if (currentState == EnemyState.Attacking) TransitionToState(EnemyState.Chasing);
		}
		if (currentState == EnemyState.Idle) TransitionToState(EnemyState.Chasing);
	}

	private void HandleDamaged(DamageInfo info)
	{
		// Only a true Heavy hit staggers. Light never does, and Attachment never does (it hits every second).
		if (info.attackType == AttackType.Heavy && info.interruptSeconds > 0f) Interrupt(info.interruptSeconds);
		if (info.attackerReference == null) return;
		_lastAttacker = info.attackerReference;
		if (alertAfterHit > 0f && currentState == EnemyState.Idle && _alertAt < 0f)
			_alertAt = Time.time + alertAfterHit;
	}

	private Transform ChooseTarget()
	{
		// 1. carts
		Transform cart = null;
		float cartD = enemyData.aggroRange;
		foreach (MovableObject m in MovableObject.Carts)
		{
			if (m == null || !m.isActiveAndEnabled) continue;
			Health h = m.GetComponent<Health>();
			if (h == null || h.IsDead) continue;
			float d = Vector3.Distance(transform.position, m.transform.position);
			if (d <= cartD) { cartD = d; cart = m.transform; }
		}
		if (cart != null) return cart;

		// 2. keep the current target while attacking it
		if (currentState == EnemyState.Attacking && IsTargetStillValid(_currentTarget)) return _currentTarget;

		// 3. nearest
		return FindNearestTarget();
	}

	private void HandleChasingState()
	{
		if (!_navAgent.isOnNavMesh) return;
		HandleTargetScan();
		if (!IsTargetStillValid(_currentTarget))
		{
			TransitionToState(EnemyState.Idle);
			return;
		}

		PlayerAttachment pa = GetPlayerAttach(_currentTarget);
		if (pa != null)
		{
			if (_pointIndex < 0) pa.TryReserve(this, out _pointIndex);
			if (_pointIndex >= 0 && pa.GetPointPosition(_pointIndex, out Vector3 p))
			{
				Vector3 flat = p - transform.position;
				flat.y = 0f;
				if (flat.magnitude <= 0.7f)
				{
					pa.Confirm(this);
					TransitionToState(EnemyState.Attacking);
					return;
				}
				_navAgent.isStopped = false;
				_navAgent.SetDestination(p);
				return;
			}
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
		if (!_navAgent.isOnNavMesh) return;
		HandleTargetScan();
		if (currentState != EnemyState.Attacking) return;
		if (!IsTargetStillValid(_currentTarget))
		{
			TransitionToState(EnemyState.Idle);
			return;
		}

		PlayerAttachment pa = GetPlayerAttach(_currentTarget);
		if (pa != null && _pointIndex < 0 && pa.TryReserve(this, out _pointIndex))
		{
			TransitionToState(EnemyState.Chasing);
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
			ReleasePoint();
			ReleaseTroop();
			_currentTarget = null;
		}
	}

	public bool IsStunned => Time.time < _stunnedUntil;
	public bool IsInterrupted => Time.time < _interruptedUntil;

	public void Interrupt(float seconds)
	{
		if (currentState == EnemyState.Dead || IsStunned) return;
		_interruptedUntil = Mathf.Max(_interruptedUntil, Time.time + seconds);
		_wasInterrupted = true;
		_attackTimer = 0f;
		_telegraphTimer = -1f;
		_attachedTimer = 0f;
		EndGlow();
		if (_navAgent.isOnNavMesh) _navAgent.isStopped = true;
		if (CombatDebug.Enabled) Debug.Log($"[Interrupt] {name} staggered for {seconds}s");
	}

	public float StunRemaining => Mathf.Max(0f, _stunnedUntil - Time.time);
	public int AttachedCount => _myAttachableSelf != null ? _myAttachableSelf.GetAllAttachedUnits().Count : 0;
	public int FrontCount => _myAttachableSelf != null ? _myAttachableSelf.FrontCount : 0;
	public int Capacity => _myAttachableSelf != null ? _myAttachableSelf.MaxSlots : 0;
	public bool CanStartOverthrow => currentState != EnemyState.Dead && !IsStunned;

	public string OverthrowTimerState
	{
		get
		{
			if (AttachedCount == 0) return "idle (no attackers)";
			if (IsStunned) return "PAUSED (stunned)";
			return "running " + _attachedTimer.ToString("F1") + "/" + shakeOffDelay.ToString("F0");
		}
	}

	public bool IsUnaware => currentState == EnemyState.Idle;

	public EnemyState State => currentState;
	public EnemyData Data => enemyData;
	public bool IsAware => currentState == EnemyState.Chasing || currentState == EnemyState.Attacking;

	public AttackDirection GetAttackDirection(Vector3 attackerPos)
	{
		Vector3 to = attackerPos - transform.position;
		to.y = 0f;
		if (to.sqrMagnitude < 0.01f) return AttackDirection.Front;
		Vector3 fwd = transform.forward;
		fwd.y = 0f;
		float dot = Vector3.Dot(fwd.normalized, to.normalized);
		if (dot > frontDotThreshold) return AttackDirection.Front;
		if (dot < backDotThreshold) return AttackDirection.Back;
		return AttackDirection.Side;
	}

	// Returns true if the attach attempt was countered (troop must not attach)
	public bool TryCounter(UnitAgent troop, int engagedCount)
	{
		AttackDirection dir = GetAttackDirection(troop.transform.position);
		string result;
		bool countered = false;

		if (!canCounter) result = "NO COUNTER (disabled)";
		else if (IsStunned) result = "BYPASS: STUNNED";
		else if (!IsAware) result = "BYPASS: UNAWARE";
		else if (dir != AttackDirection.Front) result = "BYPASS: " + dir.ToString().ToUpper();
		else if (engagedCount >= overwhelmCount) result = "OVERWHELMED (" + engagedCount + " troops)";
		else if (Time.time < _nextCounterTime) result = "BYPASS: RECOVERING";
		else
		{
			countered = true;
			result = "COUNTERED";
			_nextCounterTime = Time.time + counterCooldown;
			Vector3 d = troop.transform.position - transform.position;
			d.y = 0f;
			if (d.sqrMagnitude < 0.01f) d = transform.forward;
			troop.GetThrown(d.normalized, counterDamage, counterKnockback, counterStun, transform);
		}

		LastDefense = result;
		LastDefenseTime = Time.time;
		if (CombatDebug.Enabled) Debug.Log($"[Defense] {name} vs {troop.name}: {result} (dir={dir}, engaged={engagedCount})");
		return countered;
	}

	public void Stun(float seconds)
	{
		if (currentState == EnemyState.Dead) return;
		bool already = IsStunned;
		_stunnedUntil = Mathf.Max(_stunnedUntil, Time.time + seconds);
		_wasStunned = true;
		_telegraphTimer = -1f;
		EndGlow();
		if (_navAgent.isOnNavMesh) _navAgent.isStopped = true;
		if (!already && CombatDebug.Enabled) Debug.Log($"[Stun] {name} stunned for {seconds}s (attached={AttachedCount})");
	}

	public float GetDamageMultiplierFrom(Vector3 attackerPos)
	{
		float m = 1f;
		if (IsUnaware && GetAttackDirection(attackerPos) == AttackDirection.Back)
			m *= backstabMultiplier;
		return m;
	}

	private void HandleShakeOff()
	{
		if (IsStunned) return;
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
		if (!_navAgent.isOnNavMesh) return;
		if (!_navAgent.pathPending && _navAgent.remainingDistance < 0.5f)
		{
			SetRandomPatrolPoint();
		}
	}

	private Transform FindNearestTarget()
	{
		if (alertAfterHit <= 0f && _myAttachableSelf.AttachedUnitCount > 0)
		{
			UnitAgent priorityAttachedUnit = _myAttachableSelf.GetPriorityAttachedUnit();
			if (priorityAttachedUnit != null && priorityAttachedUnit.CanBeAttackedBy(this))
			{
				return priorityAttachedUnit.transform;
			}
		}
		float num = float.MaxValue;
		Collider collider = null;
		Collider[] array = Physics.OverlapSphere(base.transform.position, enemyData.aggroRange, unitLayer);
		foreach (Collider collider2 in array)
		{
			float num2 = Vector3.Distance(base.transform.position, collider2.transform.position);
			if (num2 < num - 0.5f || (Mathf.Abs(num2 - num) <= 0.5f && collider2.transform == _lastAttacker))
			{
				UnitAgent component = collider2.GetComponent<UnitAgent>();
				if (!(component == null) && component.CurrentStateEnum != UnitState.Dead && component.CanBeAttackedBy(this))
				{
					num = num2;
					collider = collider2;
				}
			}
		}
		array = ignorePlayer ? new Collider[0] : Physics.OverlapSphere(base.transform.position, enemyData.aggroRange, playerLayer);
		foreach (Collider collider3 in array)
		{
			float num3 = Vector3.Distance(base.transform.position, collider3.transform.position);
			if (num3 < num - 0.5f || (Mathf.Abs(num3 - num) <= 0.5f && collider3.transform == _lastAttacker))
			{
				Health component2 = collider3.GetComponent<Health>();
				if (!(component2 == null) && !component2.IsDead)
				{
					num = num3;
					collider = collider3;
				}
			}
		}
		return collider != null ? collider.transform : null;
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
			ReleaseTroop();
			_currentTarget = null;
			return;
		}
		Debug.Log("Enemy hits " + _currentTarget.name + "!");
		UnitAgent component3 = _currentTarget.GetComponent<UnitAgent>();
		if (component3 != null)
		{
			component3.ReceiveAggro(_myAttachableSelf);
		}
		PlayerAttachment pa = GetPlayerAttach(_currentTarget);
		bool onPlayer = pa != null;
		bool attached = onPlayer && _pointIndex >= 0;
		DamageInfo info = new DamageInfo
		{
			amount = enemyData.attackDamage * ((onPlayer && !attached) ? unattachedDamageMultiplier : 1f),
			attackerReference = base.transform,
			type = DamageType.Normal,
			attackType = (onPlayer && !attached) ? AttackType.Light : (onPlayer ? AttackType.Attachment : AttackType.Light)
		};
		if (enemyData.heavyAttack && component3 != null)
		{
			Vector3 hd = component3.transform.position - transform.position;
			hd.y = 0f;
			if (hd.sqrMagnitude < 0.01f) hd = transform.forward;
			component3.GetThrown(hd.normalized, info.amount, enemyData.heavyKnockback, enemyData.heavyInterrupt, transform);
			return;
		}
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
		ReleaseTroop();
		ReleasePoint();
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
		ReleaseTroop();
		ReleasePoint();
		_health.OnDeath.RemoveListener(Die);
		_health.OnDamaged -= HandleDamaged;
	}

	private void OnDrawGizmosSelected()
	{
		if (enemyData != null)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(base.transform.position, enemyData.aggroRange);
		}
		Vector3 f = transform.forward;
		f.y = 0f;
		f.Normalize();
		Vector3 o = transform.position + Vector3.up * 0.3f;
		float frontAng = Mathf.Acos(Mathf.Clamp(frontDotThreshold, -1f, 1f)) * Mathf.Rad2Deg;
		float backAng = Mathf.Acos(Mathf.Clamp(backDotThreshold, -1f, 1f)) * Mathf.Rad2Deg;
		Gizmos.color = Color.green;
		Gizmos.DrawRay(o, f * 5f);
		Gizmos.DrawRay(o, Quaternion.Euler(0f, frontAng, 0f) * f * 4f);
		Gizmos.DrawRay(o, Quaternion.Euler(0f, -frontAng, 0f) * f * 4f);
		Gizmos.color = Color.red;
		Gizmos.DrawRay(o, Quaternion.Euler(0f, backAng, 0f) * f * 4f);
		Gizmos.DrawRay(o, Quaternion.Euler(0f, -backAng, 0f) * f * 4f);
	}
}
