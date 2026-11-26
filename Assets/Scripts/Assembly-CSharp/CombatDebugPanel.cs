using UnityEngine;

public class CombatDebugPanel : MonoBehaviour
{
	private static CombatDebugPanel _instance;
	private Transform _player;
	private EnemyBase[] _enemies = new EnemyBase[0];
	private PlayerUnitManager _um;
	private float _timer;
	private GUIStyle _text;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Boot()
	{
		if (_instance != null) return;
		GameObject go = new GameObject("CombatDebugPanel");
		_instance = go.AddComponent<CombatDebugPanel>();
		DontDestroyOnLoad(go);
	}

	private void Update()
	{
		_timer -= Time.deltaTime;
		if (_timer > 0f) return;
		_timer = 0.3f;
		if (_player == null)
		{
			PlayerController pc = FindObjectOfType<PlayerController>();
			if (pc != null) _player = pc.transform;
		}
		if (_um == null) _um = FindObjectOfType<PlayerUnitManager>();
		_enemies = FindObjectsOfType<EnemyBase>();
	}

	private static string Hex(Color c) { return ColorUtility.ToHtmlStringRGB(c); }

	private void OnGUI()
	{
		if (!CombatDebug.Enabled) return;
		if (_text == null)
		{
			_text = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true, wordWrap = false };
			_text.normal.textColor = Color.white;
		}

		string t = "<b><color=#7fd1ff>COMBAT DEBUG</color></b>  <color=#888888>(G hides)</color>\n";

		// Enemy
		EnemyBase n = null;
		float best = float.MaxValue;
		if (_player != null)
		{
			foreach (EnemyBase e in _enemies)
			{
				if (e == null || e.State == EnemyBase.EnemyState.Dead) continue;
				float d = Vector3.Distance(e.transform.position, _player.position);
				if (d < best) { best = d; n = e; }
			}
		}
		t += "\n<b>NEAREST ENEMY</b>\n";
		if (n != null && best < 40f)
		{
			AttackDirection dir = n.GetAttackDirection(_player.position);
			string dc = dir == AttackDirection.Front ? "ff6666" : (dir == AttackDirection.Back ? "66ff88" : "ffdd55");
			Health eh = n.GetComponent<Health>();
			t += n.name + "  <color=#888888>" + best.ToString("F0") + "m</color>\n";
			t += (n.IsAware ? "<color=#ff6666>AWARE</color>" : "<color=#aaaaaa>UNAWARE</color>");
			if (n.IsStunned) t += "  <color=#ffdd55>STUNNED " + n.StunRemaining.ToString("F1") + "s</color>";
			t += "\nYou are at its <color=#" + dc + ">" + dir.ToString().ToUpper() + "</color>\n";
			if (n.LastDefense != "" && Time.time - n.LastDefenseTime < 4f) t += "Last attach: <color=#7fd1ff>" + n.LastDefense + "</color>\n";
			t += "Attached " + n.AttachedCount + "   Shake-off: " + n.OverthrowTimerState + "\n";
			if (eh != null) t += "HP " + eh.CurrentHP.ToString("F0") + " / " + eh.MaxHP.ToString("F0") + "\n";
		}
		else t += "<color=#888888>none nearby</color>\n";

		// Player
		t += "\n<b>PLAYER</b>\n";
		if (_player != null)
		{
			Health ph = _player.GetComponent<Health>();
			if (ph != null) t += "HP " + ph.CurrentHP.ToString("F0") + " / " + ph.MaxHP.ToString("F0") + "\n";
			PlayerAttachment pa = _player.GetComponent<PlayerAttachment>();
			if (pa != null) t += "Attach " + pa.Summary() + "  " + pa.OccupiedCount() + "/" + pa.Capacity + "   <color=#888888>Y = test enemies</color>\n";
		}
		else t += "<color=#ff6666>not found</color>\n";

		// Troop health
		if (_um != null)
		{
			t += "\n<b>TROOP HP</b>\n";
			t += "Melee " + _um.HealthFor(UnitType.Melee) + "  Ranged " + _um.HealthFor(UnitType.Ranged) + "\n";
			t += "Medic " + _um.HealthFor(UnitType.Medic) + "  Builder " + _um.HealthFor(UnitType.Builder);
		}

		GUIContent c = new GUIContent(t);
		float w = 330f;
		float h = _text.CalcHeight(c, w - 20f) + 16f;
		Rect r = new Rect(Screen.width - w - 10f, Screen.height - h - 10f, w, h);
		GUI.DrawTexture(r, UiEase.Solid(new Color(0.05f, 0.07f, 0.10f, 0.85f)));
		GUI.Label(new Rect(r.x + 10f, r.y + 8f, w - 20f, h - 16f), c, _text);
	}
}
