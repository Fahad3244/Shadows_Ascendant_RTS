using UnityEngine;

public class UnitReturnState : UnitStateBase
{
	private const float ABSORB_DISTANCE = 1.5f;

	private bool _despawnTriggered;

	public UnitReturnState(UnitAgent agent)
		: base(agent)
	{
	}

	public override void Enter()
	{
		_despawnTriggered = false;
		_agent.NavAgent.isStopped = false;
		_agent.NavAgent.stoppingDistance = 0.5f;
		_agent.ClearReservation();
		_agent.ApplyMovementDynamics(_agent.BaseMoveSpeed * 1.2f);
	}

	public override void Update()
	{
		_agent.NavAgent.SetDestination(_agent.PlayerTransform.position);
		if (!_despawnTriggered && Vector3.Distance(_agent.transform.position, _agent.PlayerTransform.position) <= 1.5f)
		{
			_despawnTriggered = true;
			_agent.NavAgent.isStopped = true;
			_agent.PlayDespawnEffect(_agent.Despawn);
		}
	}

	public override void Exit()
	{
		_agent.ApplyMovementDynamics();
	}
}
