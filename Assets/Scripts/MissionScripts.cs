using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Draws the mission list on the left of the screen
public class MissionTracker : MonoBehaviour
{
	private class Line { public string text; public bool done; public float changedAt = -99f; }

	private static readonly Dictionary<string, Line> Lines = new Dictionary<string, Line>();
	private static readonly List<string> Order = new List<string>();
	private static MissionTracker _instance;
	private GUIStyle _style;
	private float _vis;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Boot()
	{
		if (_instance != null) return;
		GameObject go = new GameObject("MissionTracker");
		_instance = go.AddComponent<MissionTracker>();
		DontDestroyOnLoad(go);
		SceneManager.sceneLoaded += (s, m) =>
		{
			Clear();
			if (_instance != null) _instance._vis = 0f;
		};
	}

	public static void Set(string id, string text, bool done = false)
	{
		Line l;
		if (!Lines.TryGetValue(id, out l))
		{
			l = new Line();
			Lines[id] = l;
			Order.Add(id);
			l.changedAt = Time.time;
		}
		else if (l.text != text || l.done != done)
		{
			l.changedAt = Time.time;
		}
		l.text = text;
		l.done = done;
	}

	public static void Clear()
	{
		Lines.Clear();
		Order.Clear();
	}

	private void Update()
	{
		_vis = Mathf.MoveTowards(_vis, Order.Count > 0 ? 1f : 0f, Time.deltaTime / 0.4f);
	}

	private void OnGUI()
	{
		if (_vis <= 0f || Order.Count == 0) return;
		if (_style == null)
		{
			_style = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true, wordWrap = true };
			_style.normal.textColor = new Color(0.9f, 0.92f, 0.95f);
		}
		string t = "<b><color=#7fd1ff>MISSIONS</color></b>\n";
		foreach (string id in Order)
		{
			Line l = Lines[id];
			bool fresh = Time.time - l.changedAt < 2.5f;
			if (l.done) t += "<color=#66ff88>[x] " + l.text + "</color>\n";
			else if (fresh) t += "<b><color=#ffd966>[ ] " + l.text + "</color></b>\n";
			else t += "[ ] " + l.text + "\n";
		}
		float w = 360f;
		float h = _style.CalcHeight(new GUIContent(t), w - 24f) + 20f;
		float x = Mathf.Lerp(-w - 20f, 10f, UiEase.OutCubic(_vis));
		Rect r = new Rect(x, 190f, w, h);
		Color old = GUI.color;
		GUI.color = new Color(1f, 1f, 1f, _vis);
		GUI.DrawTexture(r, UiEase.Solid(new Color(0.05f, 0.07f, 0.10f, 0.88f)));
		GUI.Label(new Rect(r.x + 12f, r.y + 10f, w - 24f, h - 20f), t, _style);
		GUI.color = old;
	}
}

// One step of a mission. Call Complete() from any UnityEvent.
public class MissionStep : MonoBehaviour
{
	[Header("Auto-start (optional)")]
	public float beginRadius;
	public string beginText;
	public string beginMessage;

	[Header("On Complete()")]
	public string trackerId;
	public string nextText;
	public bool finishes;
	public string message;
	public GameObject[] activate;
	public GameObject[] deactivate;

	private bool _begun;
	private bool _completed;
	private Transform _player;
	private float _timer;

	private void Update()
	{
		if (beginRadius <= 0f || _begun) return;
		_timer -= Time.deltaTime;
		if (_timer > 0f) return;
		_timer = 0.3f;
		if (_player == null)
		{
			PlayerController pc = FindObjectOfType<PlayerController>();
			if (pc != null) _player = pc.transform;
		}
		if (_player == null) return;
		Vector3 d = _player.position - transform.position;
		d.y = 0f;
		if (d.magnitude > beginRadius) return;
		_begun = true;
		MissionTracker.Set(trackerId, beginText);
		if (LevelMessenger.Instance != null && !string.IsNullOrEmpty(beginMessage))
			LevelMessenger.Instance.Say(beginMessage);
	}

	public void Complete()
	{
		if (_completed) return;
		_completed = true;
		if (deactivate != null)
			foreach (GameObject g in deactivate) if (g != null) g.SetActive(false);
		if (activate != null)
			foreach (GameObject g in activate) if (g != null) g.SetActive(true);
		if (!string.IsNullOrEmpty(nextText)) MissionTracker.Set(trackerId, nextText, finishes);
		if (LevelMessenger.Instance != null && !string.IsNullOrEmpty(message))
			LevelMessenger.Instance.Say(message);
	}
}
