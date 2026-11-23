using UnityEngine;

public class EnemyDebugLabel : MonoBehaviour
{
	private static EnemyDebugLabel _instance;
	private bool _show = true;
	private Transform _player;
	private EnemyBase[] _enemies = new EnemyBase[0];
	private float _timer;
	private GUIStyle _style;
	private GUIStyle _box;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Boot()
	{
		if (_instance != null) return;
		GameObject go = new GameObject("EnemyDebugLabel");
		_instance = go.AddComponent<EnemyDebugLabel>();
		DontDestroyOnLoad(go);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.G)) _show = !_show;
		_timer -= Time.deltaTime;
		if (_timer > 0f) return;
		_timer = 0.3f;
		if (_player == null)
		{
			PlayerController pc = FindObjectOfType<PlayerController>();
			if (pc != null) _player = pc.transform;
		}
		_enemies = FindObjectsOfType<EnemyBase>();
	}

	private void OnGUI()
	{
		if (!_show) return;
		if (_style == null)
		{
			_style = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
			_box = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.UpperLeft };
		}

		EnemyBase nearest = null;
		float best = float.MaxValue;
		if (_player != null)
		{
			foreach (EnemyBase e in _enemies)
			{
				if (e == null || e.State == EnemyBase.EnemyState.Dead) continue;
				float d = Vector3.Distance(e.transform.position, _player.position);
				if (d < best) { best = d; nearest = e; }
			}
		}

		string status = "ENEMY DEBUG (G = hide)\n";
		status += "Player: " + (_player != null ? "found" : "NOT FOUND") + "   Enemies: " + _enemies.Length + "\n";
		if (nearest != null)
		{
			AttackDirection nd = nearest.GetAttackDirection(_player.position);
			status += "Nearest: " + nearest.name + " (" + best.ToString("F0") + "m)\n";
			status += (nearest.IsAware ? "AWARE" : "UNAWARE") + (nearest.IsStunned ? " | STUNNED" : "") + "\n";
			status += "You are at its: " + nd.ToString().ToUpper();
			if (nearest.LastDefense != "" && Time.time - nearest.LastDefenseTime < 4f)
				status += "\nLast attach: " + nearest.LastDefense;
		}
		GUI.Box(new Rect(10, 10, 360, 130), status, _box);

		Camera cam = Camera.main;
		if (_player == null || cam == null) return;
		foreach (EnemyBase e in _enemies)
		{
			if (e == null || e.State == EnemyBase.EnemyState.Dead) continue;
			if (Vector3.Distance(e.transform.position, _player.position) > 25f) continue;
			Vector3 sp = cam.WorldToScreenPoint(e.transform.position + Vector3.up * 3.5f);
			if (sp.z < 0f) continue;
			AttackDirection dir = e.GetAttackDirection(_player.position);
			Color c = dir == AttackDirection.Front ? Color.red : (dir == AttackDirection.Back ? Color.green : Color.yellow);
			string text = (e.IsAware ? "AWARE" : "UNAWARE") + (e.IsStunned ? " | STUNNED" : "") + "\nYou: " + dir.ToString().ToUpper();
			if (e.LastDefense != "" && Time.time - e.LastDefenseTime < 4f) text += "\n" + e.LastDefense;
			Rect r = new Rect(sp.x - 100f, Screen.height - sp.y - 20f, 200f, 64f);
			_style.normal.textColor = Color.black;
			GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), text, _style);
			_style.normal.textColor = c;
			GUI.Label(r, text, _style);
		}
	}
}
