using UnityEngine;

public class UnitDeadState : UnitStateBase
{
	public UnitDeadState(UnitAgent agent)
		: base(agent)
	{
	}

	public override void Enter()
	{
		_agent.NavAgent.isStopped = true;
		_agent.NavAgent.enabled = false;
		_agent.ClearReservation();
		Collider component = _agent.GetComponent<Collider>();
		if (component != null)
		{
			component.enabled = false;
		}
	}

	public override void Update()
	{
	}

	public override void Exit()
	{
	}
}
