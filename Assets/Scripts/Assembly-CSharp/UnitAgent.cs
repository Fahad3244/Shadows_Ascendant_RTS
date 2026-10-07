using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class UnitAgent : MonoBehaviour
{
	[Header("Target Detection")]
	public float searchRadius = 15f;

	public LayerMask targetLayer;

	[Tooltip("Seconds between auto-target scans while a unit is moving.")]
	public float scanInterval = 0.2f;

	[Header("Movement Stats")]
	public float BaseMoveSpeed = 8f;

	public float BaseAcceleration = 16f;

	public float LeashDistance = 20f;

	[Header("Interruption Rules")]
	[Tooltip("Pause before reacquiring after the target disappears.")]
	public float targetLostPause = 0.5f;

	[Tooltip("Look for another target nearby when the current one is lost.")]
	public bool reacquireAfterTargetLost = true;

	[Tooltip("Pause after arriving in an empty area before returning.")]
	public float arrivePauseTime = 1.5f;

	[Tooltip("How far from the destination a partial path may end before it counts as blocked.")]
	public float blockedTolerance = 1.5f;

	private Health _health;

	private NavMeshPath _reachPath;

	private UnitStateBase _currentState;

	private Dictionary<UnitState, UnitStateBase> _states = new Dictionary<UnitState, UnitStateBase>();

	private static readonly HashSet<UnitState> BusyStates = new HashSet<UnitState>
	{
		UnitState.AtFlag,
		UnitState.Carrying,
		UnitState.Interacting
	};

	public UnitState CurrentStateEnum { get; private set; }

	public NavMeshAgent NavAgent { get; private set; }

	public PlayerUnitManager Manager { get; private set; }

	public UnitPool Pool { get; private set; }

	public Transform PlayerTransform { get; private set; }

	public Unit InternalData { get; private set; }

	public UnitType Type { get; private set; }

	public AttachableTarget ReservedTarget { get; private set; }

	public GuardFlag CurrentGuardFlag { get; private set; }

	public PickupItem CarriedPickup { get; set; }

	public bool IsReturning => CurrentStateEnum == UnitState.Returning;

	public void Initialize(Unit data, UnitType type, PlayerUnitManager manager, UnitPool pool, Transform player)
	{
		InternalData = data;
		Type = type;
		Manager = manager;
		Pool = pool;
		PlayerTransform = player;
		ApplyMovementDynamics();
		_health.Initialize(data.health);
		_health.OnDeath.RemoveListener(HandleDeath);
		_health.OnDeath.AddListener(HandleDeath);
		ChangeState(UnitState.Idle);
	}

	public void ResetHealth()
	{
		if (InternalData != null)
		{
			_health.Initialize(InternalData.health);
		}
	}

	private void Awake()
	{
		NavAgent = GetComponent<NavMeshAgent>();
		_health = GetComponent<Health>();
		_states.Add(UnitState.Idle, new UnitIdleState(this));
		_states.Add(UnitState.Moving, new UnitMoveState(this));
		_states.Add(UnitState.Returning, new UnitReturnState(this));
		_states.Add(UnitState.Interacting, new UnitInteractState(this));
		_states.Add(UnitState.AtFlag, new UnitGuardState(this));
		_states.Add(UnitState.Carrying, new UnitCarryState(this));
		_states.Add(UnitState.Dead, new UnitDeadState(this));
		_states.Add(UnitState.Sweeping, new UnitSweepState(this));
	}

	private void Update()
	{
		if (_currentState != null)
		{
			_currentState.Update();
		}
	}

	public void ApplyMovementDynamics(float? speedOverride = null, float? accelOverride = null)
	{
		NavAgent.speed = speedOverride ?? BaseMoveSpeed;
		NavAgent.acceleration = accelOverride ?? BaseAcceleration;
	}

	public void ChangeState(UnitState newStateEnum)
	{
		if (_states.ContainsKey(newStateEnum) && CurrentStateEnum != UnitState.Dead && (CurrentStateEnum != newStateEnum || _currentState == null))
		{
			CurrentStateEnum = newStateEnum;
			_currentState?.Exit();
			_currentState = _states[newStateEnum];
			_currentState.Enter();
		}
	}

	public void MoveTo(Vector3 destination, float? speedOverride = null, float? accelOverride = null)
	{
		ClearReservation();
		if (CurrentStateEnum != UnitState.Moving)
		{
			ChangeState(UnitState.Moving);
		}
		UnitMoveState obj = (UnitMoveState)_states[UnitState.Moving];
		obj.SetMovementOverrides(speedOverride, accelOverride);
		obj.SetDestination(destination);
	}

	public void MoveToTarget(AttachableTarget target)
	{
		if (ReservedTarget != target)
		{
			ClearReservation();
		}
		ChangeState(UnitState.Moving);
		((UnitMoveState)_states[UnitState.Moving]).SetTarget(target);
	}

	public void ExecuteSweep(List<Vector3> pathNodes, ITargetable finalTarget)
	{
		ClearReservation();
		ChangeState(UnitState.Sweeping);
		((UnitSweepState)_states[UnitState.Sweeping]).SetSweepCommand(pathNodes, finalTarget);
	}

	public void ReturnToPlayer()
	{
		ClearReservation();
		ChangeState(UnitState.Returning);
	}

	public void Despawn()
	{
		if (CarriedPickup != null)
		{
			CarriedPickup.Deliver();
			CarriedPickup = null;
		}
		Manager.ReturnUnit(InternalData);
		Pool.ReturnAgent(this);
	}

	public void ClearReservation()
	{
		if (ReservedTarget != null)
		{
			ReservedTarget.CancelReservation(this);
			ReservedTarget = null;
		}
	}

	public void SetReservedTarget(AttachableTarget target)
	{
		ReservedTarget = target;
	}

	public void OnAttachedToTarget(AttachableTarget target)
	{
		PickupItem pickup = target.GetComponent<PickupItem>();
		if (pickup != null)
		{
			target.CancelReservation(this);
			if (pickup.TryCollect(this))
			{
				CarriedPickup = pickup;
			}
			ReturnToPlayer();
			return;
		}
		if (target.GetComponent<ResourceSource>() != null)
		{
			target.CancelReservation(this);
			ChangeState(UnitState.Carrying);
		}
		else if (CurrentStateEnum != UnitState.Interacting)
		{
			ChangeState(UnitState.Interacting);
			((UnitInteractState)_states[UnitState.Interacting]).SetTarget(target);
		}
	}

	public void OnDetachedFromTarget()
	{
		if (CurrentStateEnum == UnitState.Interacting)
		{
			ChangeState(UnitState.Moving);
		}
	}

	private void AssignGuardFlag(GuardFlag flag)
	{
		CurrentGuardFlag = flag;
	}

	public void EnterGuardMode(GuardFlag flag)
	{
		AssignGuardFlag(flag);
		flag.AddUnit(this);
		ChangeState(UnitState.AtFlag);
		((UnitGuardState)_states[UnitState.AtFlag]).SetFlag(flag);
	}

	public void ReacquireOrReturn()
	{
		ClearReservation();
		if (reacquireAfterTargetLost)
		{
			AttachableTarget next = FindClosestTarget();
			if (next != null && next.TryReserveSlot(this))
			{
				SetReservedTarget(next);
				MoveToTarget(next);
				return;
			}
		}
		ReturnToPlayer();
	}

	public void ReceiveAggro(AttachableTarget enemyTarget)
	{
		if (IsBusy() || CurrentStateEnum == UnitState.Sweeping || CurrentStateEnum == UnitState.Returning || CurrentStateEnum == UnitState.Dead) return;
		if (CurrentStateEnum == UnitState.Moving && ReservedTarget != null) return;
		if (enemyTarget.TryReserveSlot(this))
		{
			SetReservedTarget(enemyTarget);
			MoveToTarget(enemyTarget);
		}
	}

	public AttachableTarget FindClosestTarget()
	{
		Collider[] array = Physics.OverlapSphere(base.transform.position, searchRadius, targetLayer);
		AttachableTarget closestEnemy = null;
		float closestEnemyDist = float.MaxValue;
		AttachableTarget closestObject = null;
		float closestObjectDist = float.MaxValue;
		for (int i = 0; i < array.Length; i++)
		{
			AttachableTarget componentInParent = array[i].GetComponentInParent<AttachableTarget>();
			if (componentInParent == null || !componentInParent.CanBeEngaged)
			{
				continue;
			}
			Health targetHealth = componentInParent.GetComponent<Health>();
			if (targetHealth != null && targetHealth.IsDead) continue;
			float dist = Vector3.Distance(base.transform.position, componentInParent.transform.position);
			if (componentInParent.Team == TargetTeam.Enemy)
			{
				if (dist < closestEnemyDist && CanReach(componentInParent.transform.position)) { closestEnemyDist = dist; closestEnemy = componentInParent; }
			}
			else if (componentInParent.Team == TargetTeam.Object)
			{
				if (dist < closestObjectDist && CanReach(componentInParent.transform.position)) { closestObjectDist = dist; closestObject = componentInParent; }
			}
		}
		return closestEnemy != null ? closestEnemy : closestObject;
	}

	public bool CanReach(Vector3 worldPos)
	{
		if (NavAgent == null || !NavAgent.isOnNavMesh) return true;
		if (!NavMesh.SamplePosition(worldPos, out NavMeshHit hit, 3f, NavMesh.AllAreas)) return false;
		if (_reachPath == null) _reachPath = new NavMeshPath();
		return NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, _reachPath)
			&& _reachPath.status == NavMeshPathStatus.PathComplete;
	}

	public bool IsBusy()
	{
		return BusyStates.Contains(CurrentStateEnum);
	}

	private void HandleDeath()
	{
		ClearReservation();
		if (CarriedPickup != null)
		{
			CarriedPickup.Drop(transform.position);
			CarriedPickup = null;
		}
		if (Pool != null)
		{
			Pool.ActiveAgents.Remove(this);
		}
		Manager.ProcessUnitDeath(this);
		ChangeState(UnitState.Dead);
		PlayDeathEffect();
	}

	private void PlayDeathEffect()
	{
		if (UnitLossNotificationUI.Instance != null)
		{
			UnitLossNotificationUI.Instance.ShowMessage($"{Type} Lost!");
		}
		UnitDeathVisual deathVisual = GetComponent<UnitDeathVisual>();
		if (deathVisual != null)
		{
			deathVisual.PlayDeath(() => Object.Destroy(gameObject));
			return;
		}
		Sequence sequence = DOTween.Sequence();
		sequence.Join(transform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InBack));
		sequence.OnComplete(delegate
		{
			Object.Destroy(gameObject);
		});
	}

	public void PlayDespawnEffect(System.Action onComplete)
	{
		transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InQuad).OnComplete(delegate
		{
			transform.localScale = Vector3.one;
			onComplete?.Invoke();
		});
	}

	private void OnDestroy()
	{
		if (_health != null)
		{
			_health.OnDeath.RemoveListener(HandleDeath);
		}
	}
}
