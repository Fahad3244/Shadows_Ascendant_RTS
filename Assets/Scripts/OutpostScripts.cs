using UnityEngine;

// Shows a banner when a waypoint changes owner
public class WaypointAnnouncer : MonoBehaviour
{
	public Waypoint target;
	public string capturedMessage;
	public string lostMessage;

	private void OnEnable() { Waypoint.OnOwnerChanged += Handle; }
	private void OnDisable() { Waypoint.OnOwnerChanged -= Handle; }

	private void Handle(Waypoint w, WaypointOwner oldOwner, WaypointOwner newOwner)
	{
		if (w != target || LevelMessenger.Instance == null) return;
		if (newOwner == WaypointOwner.Player && !string.IsNullOrEmpty(capturedMessage))
			LevelMessenger.Instance.Say(capturedMessage);
		else if (oldOwner == WaypointOwner.Player && newOwner != WaypointOwner.Player && !string.IsNullOrEmpty(lostMessage))
			LevelMessenger.Instance.Say(lostMessage);
	}
}

// Wakes hidden objects the first time 'watched' is picked up or destroyed
public class AmbushTrigger : MonoBehaviour
{
	public GameObject watched;
	public GameObject[] activate;
	public string message;
	private bool _hadTarget;
	private bool _fired;

	private void Start() { _hadTarget = watched != null; }

	private void Update()
	{
		if (_fired || !_hadTarget) return;
		if (watched != null && watched.activeSelf) return;
		_fired = true;
		foreach (GameObject g in activate)
			if (g != null) g.SetActive(true);
		if (LevelMessenger.Instance != null && !string.IsNullOrEmpty(message))
			LevelMessenger.Instance.Say(message);
	}
}
