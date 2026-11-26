using UnityEngine;

public class LevelIntro : MonoBehaviour
{
	private bool _show = true;
	private bool _closing;
	private float _vis;
	private float _hideAt;
	private GUIStyle _title;
	private GUIStyle _body;
	private GUIStyle _center;

	private void Start()
	{
		_hideAt = Time.time + 60f;
		MissionTracker.Set("main", "GOAL | Explore the jungle, break the 3 seals and defeat the boss at the north end.");
	}

	private void Update()
	{
		if (!_show) return;
		if (!_closing && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Time.time > _hideAt))
			_closing = true;
		_vis = Mathf.MoveTowards(_vis, _closing ? 0f : 1f, Time.deltaTime / (_closing ? 0.3f : 0.45f));
		if (_closing && _vis <= 0f) _show = false;
	}

	private void OnGUI()
	{
		if (!_show || _vis <= 0f) return;
		if (_title == null)
		{
			_title = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
			_title.normal.textColor = new Color(0.5f, 0.82f, 1f);
		}
		if (_body == null)
		{
			_body = new GUIStyle(GUI.skin.label) { fontSize = 19, richText = true, wordWrap = true };
			_body.normal.textColor = new Color(0.92f, 0.94f, 0.97f);
		}
		if (_center == null)
		{
			_center = new GUIStyle(GUI.skin.label) { fontSize = 19, richText = true, alignment = TextAnchor.MiddleCenter };
			_center.normal.textColor = new Color(0.92f, 0.94f, 0.97f);
		}
		Texture2D bg = UiEase.Solid(new Color(0.03f, 0.05f, 0.08f, 0.92f));

		float w = Mathf.Min(Screen.width - 60f, 820f);
		float h = 470f;
		Rect r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);

		Color old = GUI.color;
		Matrix4x4 m = GUI.matrix;
		GUI.color = new Color(1f, 1f, 1f, UiEase.Smooth(_vis));
		float s = _closing ? Mathf.Lerp(0.92f, 1f, _vis) : Mathf.Lerp(0.85f, 1f, UiEase.OutBack(_vis));
		GUIUtility.ScaleAroundPivot(Vector2.one * s, r.center);

		GUI.DrawTexture(r, bg);
		GUI.Label(new Rect(r.x, r.y + 14f, w, 60f), "THE JUNGLE EXPEDITION", _title);
		string t =
			"A boss waits at the north end of the jungle, behind a door sealed three times.\n\n" +
			"<b><color=#ffd966>YOUR GOAL</color></b>\nBreak the 3 seals, then defeat the boss. The seals are hidden in the Carry Yard, the Outpost and the Ruins.\n\n" +
			"<b><color=#ffd966>HOW TO PLAY</color></b>\n" +
			"Follow the dirt path north. Side trails lead to areas in the jungle: take them, they hold loot, puzzles and the seals.\n" +
			"Stand on the <color=#66ff88>green pads</color> to read what to do. The <b>MISSIONS</b> list on the left shows your current objective.\n" +
			"Troops are lost for good. Walk onto a <color=#6fa8ff>blue rally pad</color> to recruit replacements.\n\n" +
			"<b><color=#ffd966>KEYS</color></b>\n" +
			"1-4 troop type    Left click send    Right click recall    Z target    X clear\n" +
			"H controls    Tab station list    Shift+P test arena";
		GUI.Label(new Rect(r.x + 30f, r.y + 84f, w - 60f, h - 130f), t, _body);
		float pulse = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.time * 2.5f));
		GUI.color = new Color(1f, 1f, 1f, UiEase.Smooth(_vis) * pulse);
		GUI.Label(new Rect(r.x, r.y + h - 44f, w, 30f), "<color=#aaaaaa>Press Enter to begin</color>", _center);

		GUI.matrix = m;
		GUI.color = old;
	}
}
