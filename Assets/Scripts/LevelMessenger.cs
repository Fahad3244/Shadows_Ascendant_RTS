using UnityEngine;

public class LevelMessenger : MonoBehaviour
{
	public static LevelMessenger Instance { get; private set; }

	private const float Duration = 5f;
	private string _text = "";
	private float _start = -99f;
	private GUIStyle _style;
	private Texture2D _bg;

	private void Awake() { Instance = this; }

	public void Say(string text)
	{
		_text = text;
		_start = Time.time;
	}

	private void OnGUI()
	{
		float t = Time.time - _start;
		if (t > Duration || string.IsNullOrEmpty(_text)) return;
		if (_style == null)
		{
			_bg = new Texture2D(1, 1);
			_bg.SetPixel(0, 0, new Color(0.05f, 0.07f, 0.10f, 0.9f));
			_bg.Apply();
			_style = new GUIStyle(GUI.skin.label)
			{
				fontSize = 26,
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleCenter,
				wordWrap = true,
				richText = true
			};
			_style.normal.textColor = new Color(0.45f, 1f, 0.55f);
		}
		float a = Mathf.Clamp01(Mathf.Min(t / 0.25f, (Duration - t) / 0.8f));
		float w = Mathf.Min(Screen.width - 40f, 900f);
		float h = _style.CalcHeight(new GUIContent(_text), w - 40f) + 28f;
		Rect r = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.66f, w, h);
		Color old = GUI.color;
		GUI.color = new Color(1f, 1f, 1f, a);
		GUI.DrawTexture(r, _bg);
		GUI.Label(new Rect(r.x + 20f, r.y + 14f, w - 40f, h - 28f), _text, _style);
		GUI.color = old;
	}
}
