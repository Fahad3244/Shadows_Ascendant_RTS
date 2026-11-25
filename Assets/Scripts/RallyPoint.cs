using UnityEngine;

public class RallyPoint : MonoBehaviour
{
	public int reserve = 6;
	public float radius = 3.5f;

	private Transform _player;
	private PlayerUnitManager _um;
	private bool _inside;
	private float _timer;

	private void Update()
	{
		_timer -= Time.deltaTime;
		if (_timer > 0f) return;
		_timer = 0.25f;
		if (_player == null)
		{
			PlayerController pc = FindObjectOfType<PlayerController>();
			if (pc != null) _player = pc.transform;
		}
		if (_um == null) _um = FindObjectOfType<PlayerUnitManager>();
		if (_player == null || _um == null) return;

		Vector3 d = _player.position - transform.position;
		d.y = 0f;
		bool inside = d.magnitude <= radius;
		if (inside && !_inside) Arrive();
		_inside = inside;
	}

	private void Arrive()
	{
		LevelMessenger m = LevelMessenger.Instance;
		if (reserve <= 0)
		{
			if (m != null) m.Say("This rally point has no recruits left.");
			return;
		}
		int added = _um.ReinforceUpTo(reserve);
		if (added > 0)
		{
			reserve -= added;
			if (m != null) m.Say("RALLY POINT: " + added + " recruits joined your army! (" + reserve + " left here)");
		}
		else if (m != null)
		{
			m.Say("Your army is at full strength. Come back here when you have lost troops.");
		}
	}
}
