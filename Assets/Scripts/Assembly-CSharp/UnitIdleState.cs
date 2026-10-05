using UnityEngine;

public class UnitIdleState : UnitStateBase
{
	public UnitIdleState(UnitAgent agent)
		: base(agent)
	{
	}

	public override void Enter()
	{
		_agent.NavAgent.ResetPath();
	}

	public override void Update()
	{
		if (Vector3.Distance(_agent.transform.position, _agent.PlayerTransform.position) > _agent.LeashDistance)
		{
			_agent.ReturnToPlayer();
		}
	}

	public override void Exit()
	{
	}
}
