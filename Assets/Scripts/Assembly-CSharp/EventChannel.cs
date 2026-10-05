using System;

public class EventChannel<T>
{
	private Action<T> _action;

	public void Subscribe(Action<T> listener)
	{
		_action = (Action<T>)Delegate.Combine(_action, listener);
	}

	public void Unsubscribe(Action<T> listener)
	{
		_action = (Action<T>)Delegate.Remove(_action, listener);
	}

	public void Invoke(T payload)
	{
		_action?.Invoke(payload);
	}
}
public class EventChannel
{
	private Action _action;

	public void Subscribe(Action listener)
	{
		_action = (Action)Delegate.Combine(_action, listener);
	}

	public void Unsubscribe(Action listener)
	{
		_action = (Action)Delegate.Remove(_action, listener);
	}

	public void Invoke()
	{
		_action?.Invoke();
	}
}
