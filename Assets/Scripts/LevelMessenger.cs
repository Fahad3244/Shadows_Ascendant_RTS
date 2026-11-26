using UnityEngine;

public class LevelMessenger : MonoBehaviour
{
	public static LevelMessenger Instance { get; private set; }

	private const float Duration = 5f;
	private string _text = "";
	private float _start = -99f;
	private GUIStyle _style;

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
		float inT = Mathf.Clamp01(t / 0.4f);
		float outT = Mathf.Clamp01((Duration - t) / 0.5f);
		float a = Mathf.Min(UiEase.Smooth(inT), outT);
		float s = Mathf.Lerp(0.8f, 1f, UiEase.OutBack(inT)) * Mathf.Lerp(0.95f, 1f, outT);
		float slide = (1f - UiEase.OutCubic(inT)) * 45f + (1f - outT) * 20f;

		float w = Mathf.Min(Screen.width - 40f, 900f);
		float h = _style.CalcHeight(new GUIContent(_text), w - 40f) + 28f;
		Rect r = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.66f + slide, w, h);
		Color old = GUI.color;
		Matrix4x4 m = GUI.matrix;
		GUI.color = new Color(1f, 1f, 1f, a);
		GUIUtility.ScaleAroundPivot(Vector2.one * s, r.center);
		GUI.DrawTexture(r, UiEase.Solid(new Color(0.05f, 0.07f, 0.10f, 0.9f)));
		GUI.Label(new Rect(r.x + 20f, r.y + 14f, w - 40f, h - 28f), _text, _style);
		GUI.matrix = m;
		GUI.color = old;
	}
}
