using UnityEngine;

public class UnitCarryState : UnitStateBase
{
	private Waypoint _targetWaypoint;
	private float _carryTimeout;

	public UnitCarryState(UnitAgent agent)
		: base(agent)
	{
	}

	public override void Enter()
	{
		_agent.NavAgent.isStopped = false;
		_targetWaypoint = Waypoint.NearestPlayerOwned(_agent.transform.position);
		_agent.ApplyMovementDynamics(_agent.BaseMoveSpeed * 0.5f, _agent.BaseAcceleration * 0.4f);
		_agent.NavAgent.stoppingDistance = 0.5f;   // don't inherit the mine's 5.6
		_carryTimeout = 20f;                       // safety net
		if (_targetWaypoint != null && _agent.NavAgent.isOnNavMesh)
			_agent.NavAgent.SetDestination(_targetWaypoint.ZoneCenter);  // set once
	}

	public override void Update()
	{
		if (_targetWaypoint == null)
		{
			_agent.ReturnToPlayer();
			return;
		}

		_carryTimeout -= Time.deltaTime;
		if (_carryTimeout <= 0f)
		{
			_agent.ReturnToPlayer();   // never stall silently
			return;
		}

		Vector3 flat = _targetWaypoint.ZoneCenter - _agent.transform.position;
		flat.y = 0f;
		if (flat.magnitude <= _targetWaypoint.ZoneRadius * 0.5f)
			Deposit();
	}

	private void Deposit()
	{
		Debug.Log("Resource Deposited!");
		_agent.ReturnToPlayer();
	}

	public override void Exit()
	{
		_agent.ApplyMovementDynamics();
	}
}
