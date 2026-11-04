using UnityEngine;

public class UnitStunnedState : UnitStateBase
{
	private Vector3 _dir;
	private float _knockRemaining;
	private float _timer;
	private GameObject _marker;
	private const float KNOCK_SPEED = 12f;

	public UnitStunnedState(UnitAgent agent) : base(agent) { }

	public void Setup(Vector3 dir, float knockback, float stunSeconds)
	{
		_dir = dir;
		_knockRemaining = knockback;
		_timer = stunSeconds;
	}

	public override void Enter()
	{
		if (_agent.NavAgent.isOnNavMesh) _agent.NavAgent.ResetPath();
		_marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		Object.Destroy(_marker.GetComponent<Collider>());
		_marker.transform.SetParent(_agent.transform, false);
		_marker.transform.localPosition = new Vector3(0f, 3f, 0f);
		_marker.transform.localScale = Vector3.one * 0.5f;
		_marker.GetComponent<Renderer>().material = new Material(Shader.Find("Sprites/Default")) { color = Color.gray };
	}

	public override void Update()
	{
		if (_knockRemaining > 0f && _agent.NavAgent.isOnNavMesh)
		{
			float step = Mathf.Min(KNOCK_SPEED * Time.deltaTime, _knockRemaining);
			_agent.NavAgent.Move(_dir * step);
			_knockRemaining -= step;
		}
		_timer -= Time.deltaTime;
		if (_timer <= 0f) _agent.ReturnToPlayer();
	}

	public override void Exit()
	{
		if (_marker != null) Object.Destroy(_marker);
	}
}
