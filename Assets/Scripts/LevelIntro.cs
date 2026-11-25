using UnityEngine;

public class LevelIntro : MonoBehaviour
{
	private bool _show = true;
	private float _hideAt;
	private GUIStyle _title;
	private GUIStyle _body;
	private Texture2D _bg;

	private void Start()
	{
		_hideAt = Time.time + 60f;
		MissionTracker.Set("main", "GOAL | Explore the jungle, break the 3 seals and defeat the boss at the north end.");
	}

	private void Update()
	{
		if (!_show) return;
		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Time.time > _hideAt) _show = false;
	}

	private void OnGUI()
	{
		if (!_show) return;
		if (_title == null)
		{
			_bg = new Texture2D(1, 1);
			_bg.SetPixel(0, 0, new Color(0.03f, 0.05f, 0.08f, 0.92f));
			_bg.Apply();
			_title = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
			_title.normal.textColor = new Color(0.5f, 0.82f, 1f);
			_body = new GUIStyle(GUI.skin.label) { fontSize = 19, richText = true, wordWrap = true };
			_body.normal.textColor = new Color(0.92f, 0.94f, 0.97f);
		}
		float w = Mathf.Min(Screen.width - 60f, 820f);
		float h = 470f;
		Rect r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
		GUI.DrawTexture(r, _bg);
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
			"Tab station list    H controls    F10 test arena";
		GUI.Label(new Rect(r.x + 30f, r.y + 84f, w - 60f, h - 130f), t, _body);
		GUI.Label(new Rect(r.x, r.y + h - 44f, w, 30f), "<color=#aaaaaa>Press Enter to begin</color>", new GUIStyle(_body) { alignment = TextAnchor.MiddleCenter });
	}
}
