using System.Collections.Generic;
using UnityEngine;

public static class UiEase
{
	private static readonly Dictionary<Color, Texture2D> _solids = new Dictionary<Color, Texture2D>();

	public static Texture2D Solid(Color c)
	{
		Texture2D t;
		if (_solids.TryGetValue(c, out t) && t != null) return t;
		t = new Texture2D(1, 1);
		t.SetPixel(0, 0, c);
		t.Apply();
		t.hideFlags = HideFlags.HideAndDontSave;
		_solids[c] = t;
		return t;
	}

	public static float OutBack(float t)
	{
		t = Mathf.Clamp01(t);
		const float c1 = 1.70158f, c3 = c1 + 1f;
		float u = t - 1f;
		return 1f + c3 * u * u * u + c1 * u * u;
	}

	public static float OutCubic(float t)
	{
		t = 1f - Mathf.Clamp01(t);
		return 1f - t * t * t;
	}

	public static float Smooth(float t)
	{
		t = Mathf.Clamp01(t);
		return t * t * (3f - 2f * t);
	}
}
