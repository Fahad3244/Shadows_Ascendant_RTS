using UnityEngine;

public class UnitCarryState : UnitStateBase
{
	private Waypoint _targetWaypoint;

	public UnitCarryState(UnitAgent agent)
		: base(agent)
	{
	}

	public override void Enter()
	{
		_agent.NavAgent.isStopped = false;
		_targetWaypoint = Waypoint.Nearest(_agent.transform.position);
		_agent.ApplyMovementDynamics(_agent.BaseMoveSpeed * 0.5f, _agent.BaseAcceleration * 0.4f);
	}

	public override void Update()
	{
		if (_targetWaypoint == null)
		{
			_agent.ReturnToPlayer();
			return;
		}
		_agent.NavAgent.SetDestination(_targetWaypoint.transform.position);
		if (!_agent.NavAgent.pathPending && _agent.NavAgent.remainingDistance < 4f)
		{
			Deposit();
		}
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
