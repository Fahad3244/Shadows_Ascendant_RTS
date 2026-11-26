using System.Collections.Generic;
using UnityEngine;

// Counts seals. Opens the door when enough are broken.
public class BossGate : MonoBehaviour
{
	public int needed = 3;
	public GameObject door;
	public GameObject[] stonesOn;
	public GameObject[] stonesOff;
	public float showRadius = 55f;

	private readonly HashSet<string> _sealed = new HashSet<string>();
	private Transform _player;
	private bool _shown;
	private float _timer;

	private bool Has(string id) { return _sealed.Contains(id); }
	private string Mark(string id) { return Has(id) ? "[x]" : "[ ]"; }

	private void Update()
	{
		if (_shown) return;
		_timer -= Time.deltaTime;
		if (_timer > 0f) return;
		_timer = 0.5f;
		if (_player == null)
		{
			PlayerController pc = FindObjectOfType<PlayerController>();
			if (pc != null) _player = pc.transform;
		}
		if (_player == null) return;
		Vector3 d = _player.position - transform.position;
		d.y = 0f;
		if (d.magnitude <= showRadius)
		{
			_shown = true;
			Refresh();
			if (LevelMessenger.Instance != null)
				LevelMessenger.Instance.Say("THE BOSS ARENA: its door is sealed. Break the 3 seals to open it. Follow the MISSIONS list.");
		}
	}

	private void Refresh()
	{
		_shown = true;
		int n = _sealed.Count;
		if (n >= needed)
			MissionTracker.Set("boss", "BOSS | The arena is open. Defeat the boss!");
		else
			MissionTracker.Set("boss", "BOSS SEALS " + n + "/" + needed + " | Carry Yard key " + Mark("key") +
				" | Outpost ring " + Mark("outpost") + " | Ruins guardian " + Mark("ruins"));
	}

	// Safe to call more than once for the same id
	public void Seal(string id)
	{
		if (!_sealed.Add(id)) return;
		int n = _sealed.Count;
		int i = n - 1;
		if (stonesOn != null && i < stonesOn.Length && stonesOn[i] != null) stonesOn[i].SetActive(true);
		if (stonesOff != null && i < stonesOff.Length && stonesOff[i] != null) stonesOff[i].SetActive(false);
		Refresh();
		if (LevelMessenger.Instance == null) return;
		if (n >= needed)
		{
			if (door != null) door.SetActive(false);
			LevelMessenger.Instance.Say("ALL SEALS BROKEN! The boss arena door is open. Prepare your army.");
		}
		else
		{
			LevelMessenger.Instance.Say("SEAL " + n + "/" + needed + " BROKEN! " + (needed - n) + " more to open the boss arena.");
		}
	}
}

// Breaks a seal when a waypoint becomes player-owned
public class WaypointSeal : MonoBehaviour
{
	public Waypoint target;
	public BossGate gate;
	public string id = "outpost";

	private void OnEnable() { Waypoint.OnOwnerChanged += Handle; }
	private void OnDisable() { Waypoint.OnOwnerChanged -= Handle; }

	private void Handle(Waypoint w, WaypointOwner oldOwner, WaypointOwner newOwner)
	{
		if (w == target && newOwner == WaypointOwner.Player && gate != null) gate.Seal(id);
	}
}

// Wakes hidden enemies when the boss drops below a health fraction
public class BossPhase : MonoBehaviour
{
	public Health boss;
	public float threshold = 0.5f;
	public GameObject[] activate;
	public string message;
	private bool _fired;

	private void Update()
	{
		if (_fired || boss == null || boss.MaxHP <= 0f) return;
		if (boss.CurrentHP / boss.MaxHP > threshold) return;
		_fired = true;
		foreach (GameObject g in activate) if (g != null) g.SetActive(true);
		if (LevelMessenger.Instance != null && !string.IsNullOrEmpty(message)) LevelMessenger.Instance.Say(message);
	}
}

// Full-screen victory message
public class VictoryScreen : MonoBehaviour
{
	private bool _show;
	private bool _closing;
	private float _at;
	private float _closeAt;
	private GUIStyle _big;
	private GUIStyle _small;

	public void Show()
	{
		if (_show) return;
		_show = true;
		_closing = false;
		_at = Time.time + 2f;
		MissionTracker.Set("boss", "BOSS DEFEATED | Level complete!", true);
	}

	private void OnGUI()
	{
		if (!_show || Time.time < _at) return;
		float v = Mathf.Clamp01((Time.time - _at) / 0.7f);
		if (_closing)
		{
			v = 1f - Mathf.Clamp01((Time.time - _closeAt) / 0.4f);
			if (v <= 0f) { _show = false; return; }
		}
		if (_big == null)
		{
			_big = new GUIStyle(GUI.skin.label) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
			_big.normal.textColor = new Color(1f, 0.85f, 0.3f);
			_small = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter, richText = true, wordWrap = true };
			_small.normal.textColor = Color.white;
		}
		Color old = GUI.color;
		Matrix4x4 m = GUI.matrix;
		GUI.color = new Color(1f, 1f, 1f, UiEase.Smooth(v));
		GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), UiEase.Solid(new Color(0.02f, 0.03f, 0.06f, 0.82f)));
		float s = _closing ? 1f : Mathf.Lerp(0.6f, 1f, UiEase.OutBack(v));
		GUIUtility.ScaleAroundPivot(Vector2.one * s, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
		GUI.Label(new Rect(0, Screen.height * 0.25f, Screen.width, 90f), "VICTORY!", _big);

		PlayerUnitManager um = FindObjectOfType<PlayerUnitManager>();
		int mm = Mathf.FloorToInt(Time.timeSinceLevelLoad / 60f);
		int ss = Mathf.FloorToInt(Time.timeSinceLevelLoad % 60f);
		string t = "The boss has fallen and the level is complete.\n\n" +
			"Time  <b>" + mm + ":" + ss.ToString("00") + "</b>\n" +
			"Troops left  <b>" + (um != null ? um.TotalCount : 0) + "</b>\n\n" +
			"<color=#aaaaaa>Press F12 to play again, or keep exploring the level.</color>";
		GUI.Label(new Rect(Screen.width * 0.2f, Screen.height * 0.25f + 100f, Screen.width * 0.6f, 260f), t, _small);
		if (GUI.Button(new Rect(Screen.width * 0.5f - 90f, Screen.height * 0.25f + 380f, 180f, 44f), "Keep exploring"))
		{
			_closing = true;
			_closeAt = Time.time;
		}
		GUI.matrix = m;
		GUI.color = old;
	}
}
