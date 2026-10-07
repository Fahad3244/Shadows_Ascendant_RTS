using UnityEngine;
using UnityEngine.AI;

public class UnitMoveState : UnitStateBase
{
	private Vector3 _targetPos;

	private AttachableTarget _targetEntity;

	private bool _hasRequestedAttach;

	private float? _speedOverride;

	private float? _accelOverride;

	private float _scanTimer;

	private float _commitmentTimer;

	private float _arrivePauseTimer = -1f;

	private float _blockedTimer = -1f;

	private const float COMMITMENT_DURATION = 2f;

	public UnitMoveState(UnitAgent agent)
		: base(agent)
	{
	}

	public void SetMovementOverrides(float? speed, float? accel)
	{
		_speedOverride = speed;
		_accelOverride = accel;
		if (_agent.CurrentStateEnum == UnitState.Moving)
		{
			_agent.ApplyMovementDynamics(_speedOverride, _accelOverride);
		}
	}

	public void SetDestination(Vector3 pos)
	{
		_targetPos = pos;
		_targetEntity = null;
		_hasRequestedAttach = false;
		_arrivePauseTimer = -1f;
		_blockedTimer = -1f;
		_agent.NavAgent.stoppingDistance = 0.5f;
		if (_agent.NavAgent.isOnNavMesh)
		{
			_agent.NavAgent.SetDestination(_targetPos);
		}
	}

	public void SetTarget(AttachableTarget target)
	{
		_targetEntity = target;
		_hasRequestedAttach = false;
		if (target != null)
		{
			SetDestination(target.transform.position);
			_targetEntity = target;
			float stoppingDistance = target.AttachRadius - 0.2f;
			if (target.IsFrontAttacker(_agent))
			{
				stoppingDistance = target.FrontRadius + 0.3f;
			}
			if (_agent.Type == UnitType.Ranged && target.Team == TargetTeam.Enemy)
			{
				stoppingDistance = 6f;
			}
			_agent.NavAgent.stoppingDistance = stoppingDistance;
		}
	}

	public override void Enter()
	{
		_agent.NavAgent.isStopped = false;
		_agent.ApplyMovementDynamics(_speedOverride, _accelOverride);
		_hasRequestedAttach = false;
		_scanTimer = 0f;
		_commitmentTimer = 2f;
		_arrivePauseTimer = -1f;
		_blockedTimer = -1f;
	}

	public override void Update()
	{
		if (_commitmentTimer > 0f)
		{
			_commitmentTimer -= Time.deltaTime;
		}
		if ((object)_targetEntity != null && (_targetEntity == null || !_targetEntity.IsInteractable))
		{
			_targetEntity = null;
			_agent.ReacquireOrReturn();
			return;
		}
		if (_targetEntity == null)
		{
			_scanTimer += Time.deltaTime;
			if (_scanTimer >= _agent.scanInterval)
			{
				_scanTimer = 0f;
				AttachableTarget attachableTarget = _agent.FindClosestTarget();
				if (attachableTarget != null && attachableTarget.TryReserveSlot(_agent))
				{
					_agent.SetReservedTarget(attachableTarget);
					_agent.MoveToTarget(attachableTarget);
					return;
				}
			}
		}
		if (!_agent.NavAgent.pathPending && _agent.NavAgent.remainingDistance <= _agent.NavAgent.stoppingDistance)
		{
			if (_targetEntity != null ? !CanAttachFromHere() : IsPathBlocked())
			{
				_blockedTimer = _blockedTimer < 0f ? _agent.arrivePauseTime : _blockedTimer - Time.deltaTime;
				if (_blockedTimer <= 0f)
				{
					Debug.Log($"[Blocked] {_agent.name} cannot reach destination, returning");
					_agent.ReturnToPlayer();
				}
				return;
			}
			if (_targetEntity != null)
			{
				if (!_hasRequestedAttach)
				{
					_hasRequestedAttach = true;
					_targetEntity.ConfirmAttach(_agent);
				}
				Vector3 normalized = (_targetEntity.transform.position - _agent.transform.position).normalized;
				if (normalized != Vector3.zero)
				{
					_agent.transform.rotation = Quaternion.LookRotation(normalized);
				}
			}
			else
			{
				_arrivePauseTimer = _arrivePauseTimer < 0f ? _agent.arrivePauseTime : _arrivePauseTimer - Time.deltaTime;
				if (_arrivePauseTimer <= 0f) _agent.ReturnToPlayer();
			}
		}
		else
		{
			float num = ((_targetEntity != null) ? 100f : _agent.LeashDistance);
			if (Vector3.Distance(_agent.transform.position, _agent.PlayerTransform.position) > num && _commitmentTimer <= 0f)
			{
				_agent.ReturnToPlayer();
			}
		}
	}

	public override void Exit()
	{
		_agent.NavAgent.isStopped = true;
		_speedOverride = null;
		_accelOverride = null;
		_agent.ApplyMovementDynamics();
	}

	private bool IsPathBlocked()
	{
		NavMeshAgent nav = _agent.NavAgent;
		if (nav.pathStatus != NavMeshPathStatus.PathPartial) return false;
		Vector3 d = nav.destination - _agent.transform.position;
		d.y = 0f;
		return d.magnitude > nav.stoppingDistance + _agent.blockedTolerance;
	}

	private bool CanAttachFromHere()
	{
		Vector3 from = _agent.transform.position;
		Vector3 to = _targetEntity.transform.position;
		Vector3 flat = to - from;
		flat.y = 0f;
		float maxDist = Mathf.Max(_agent.NavAgent.stoppingDistance, _targetEntity.AttachRadius) + _agent.blockedTolerance;
		if (flat.magnitude > maxDist) return false;

		if (NavMesh.SamplePosition(from, out NavMeshHit a, 1.5f, NavMesh.AllAreas) &&
			NavMesh.SamplePosition(to, out NavMeshHit b, 2f, NavMesh.AllAreas))
		{
			return !NavMesh.Raycast(a.position, b.position, out _, NavMesh.AllAreas);
		}
		return true;
	}
}
