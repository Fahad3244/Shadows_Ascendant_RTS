public abstract class UnitStateBase
{
	protected UnitAgent _agent;

	public UnitStateBase(UnitAgent agent)
	{
		_agent = agent;
	}

	public abstract void Enter();

	public abstract void Update();

	public abstract void Exit();

	public virtual void FixedUpdate()
	{
	}
}
