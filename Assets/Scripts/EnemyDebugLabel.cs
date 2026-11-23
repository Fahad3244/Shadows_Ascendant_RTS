using UnityEngine;

public class EnemyDebugLabel : MonoBehaviour
{
	private static EnemyDebugLabel _instance;
	private Transform _player;
	private EnemyBase[] _enemies = new EnemyBase[0];
	private float _timer;
	private GUIStyle _style;

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
		if (Input.GetKeyDown(KeyCode.G)) CombatDebug.Enabled = !CombatDebug.Enabled;
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
		if (!CombatDebug.Enabled) return;
		Camera cam = Camera.main;
		if (_player == null || cam == null) return;
		if (_style == null)
			_style = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

		foreach (EnemyBase e in _enemies)
		{
			if (e == null || e.State == EnemyBase.EnemyState.Dead) continue;
			if (Vector3.Distance(e.transform.position, _player.position) > 20f) continue;
			Vector3 sp = cam.WorldToScreenPoint(e.transform.position + Vector3.up * 3.5f);
			if (sp.z < 0f) continue;

			AttackDirection dir = e.GetAttackDirection(_player.position);
			Color c = dir == AttackDirection.Front ? Color.red : (dir == AttackDirection.Back ? Color.green : Color.yellow);
			string text = (e.IsAware ? "AWARE" : "UNAWARE") + (e.IsStunned ? " | STUNNED" : "") + " | " + dir.ToString().ToUpper();
			if (e.LastDefense != "" && Time.time - e.LastDefenseTime < 4f) text += "\n" + e.LastDefense;

			Rect r = new Rect(sp.x - 120f, Screen.height - sp.y - 20f, 240f, 40f);
			_style.normal.textColor = Color.black;
			GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), text, _style);
			_style.normal.textColor = c;
			GUI.Label(r, text, _style);
		}
	}
}
