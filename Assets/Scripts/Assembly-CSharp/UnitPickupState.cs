using UnityEngine;

public class UnitPickupState : UnitStateBase
{
	private float _timer;
	private GameObject _marker;

	public UnitPickupState(UnitAgent agent) : base(agent) { }

	public override void Enter()
	{
		_timer = _agent.pickupPauseTime;
		if (_agent.NavAgent.isOnNavMesh)
		{
			_agent.NavAgent.isStopped = true;
			_agent.NavAgent.ResetPath();
		}
		_marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		Object.Destroy(_marker.GetComponent<Collider>());
		_marker.transform.SetParent(_agent.transform, false);
		_marker.transform.localPosition = new Vector3(0f, 3f, 0f);
		_marker.transform.localScale = Vector3.one * 0.5f;
		_marker.GetComponent<Renderer>().material = new Material(Shader.Find("Sprites/Default")) { color = Color.yellow };
	}

	public override void Update()
	{
		_timer -= Time.deltaTime;
		if (_timer <= 0f) _agent.ReturnToPlayer();
	}

	public override void Exit()
	{
		if (_marker != null) Object.Destroy(_marker);
		if (_agent.NavAgent.isOnNavMesh) _agent.NavAgent.isStopped = false;
	}
}
