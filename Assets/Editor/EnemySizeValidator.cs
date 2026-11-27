using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class EnemySizeValidator
{
	private static float HeightOf(GameObject go)
	{
		Renderer[] rs = go.GetComponentsInChildren<Renderer>();
		if (rs.Length == 0) return 0f;
		Bounds b = rs[0].bounds;
		foreach (Renderer r in rs) b.Encapsulate(r.bounds);
		return b.size.y;
	}

	[MenuItem("Tools/Test Arena/Validate Enemy Sizes")]
	public static void Validate()
	{
		PlayerController pc = Object.FindObjectOfType<PlayerController>();
		float playerH = pc != null ? HeightOf(pc.gameObject) : 0f;
		int warnings = 0, count = 0;
		float maxStandardHp = 0f;
		List<string> bigLines = new List<string>();
		string report = "[Sizes] player height " + playerH.ToString("F1") + "\n";

		foreach (EnemyBase e in Resources.FindObjectsOfTypeAll<EnemyBase>())
		{
			if (EditorUtility.IsPersistent(e) || !e.gameObject.scene.IsValid()) continue;
			bool archived = false;
			for (Transform t = e.transform; t != null; t = t.parent)
				if (t.name == "OldPrototype") { archived = true; break; }
			if (archived) continue;
			AttachableTarget at = e.GetComponent<AttachableTarget>();
			EnemyData d = new SerializedObject(e).FindProperty("enemyData").objectReferenceValue as EnemyData;
			if (at == null || d == null) continue;

			SerializedObject so = new SerializedObject(at);
			AttachmentSettings s = so.FindProperty("settings").objectReferenceValue as AttachmentSettings;
			int cap = s != null ? s.maxAttachmentPoints : so.FindProperty("maxAttachSlots").intValue;
			float ring = so.FindProperty("attachRadius").floatValue;
			float spacing = cap > 0 ? 2f * Mathf.PI * ring / cap : 0f;
			float h = HeightOf(e.gameObject);
			count++;

			string line = e.name + "  " + d.category + "  hp " + d.maxHP + "  points " + cap + "  height " + h.ToString("F1") + "  spacing " + spacing.ToString("F1");
			List<string> problems = new List<string>();
			if (spacing < 1.6f) problems.Add("attach points too close together (" + spacing.ToString("F1") + " < 1.6)");
			if (d.category == EnemyCategory.Standard && cap > 4) problems.Add("standard enemy with more than 4 points");
			if (d.category == EnemyCategory.Miniboss)
			{
				if (cap != 5) problems.Add("miniboss should have 5 points");
				if (playerH > 0f && h < playerH * 1.3f) problems.Add("miniboss is not clearly larger than the player");
			}
			if (d.category == EnemyCategory.Boss)
			{
				if (cap != 6) problems.Add("boss should have 6 points");
				if (playerH > 0f && h < playerH * 1.8f) problems.Add("boss is not clearly larger than the player");
			}
			if (d.category == EnemyCategory.Critter)
			{
				if (cap != 1) problems.Add("critter should have 1 point");
				// the sturdy test critter (name ends with 'Test') is allowed to exceed this
				if (d.maxHP > 60f && !e.name.EndsWith("Test")) problems.Add("critter health should be lower than a Melee troop (60)");
			}
			if (d.category == EnemyCategory.Standard && !d.passive && d.maxHP > maxStandardHp) maxStandardHp = d.maxHP;
			if (d.category != EnemyCategory.Standard && !d.passive) bigLines.Add(e.name + ":" + d.maxHP + ":" + d.category);

			if (problems.Count > 0)
			{
				warnings += problems.Count;
				Debug.LogWarning("[Sizes] " + line + "\n  -> " + string.Join("; ", problems.ToArray()));
			}
			report += line + "\n";
		}

		foreach (string b in bigLines)
		{
			string[] p = b.Split(':');
			if (float.Parse(p[1]) <= maxStandardHp)
			{
				warnings++;
				Debug.LogWarning("[Sizes] " + p[0] + " (" + p[2] + ") has no more health than the strongest standard enemy (" + maxStandardHp + ")");
			}
		}

		Debug.Log(report + "[Sizes] " + count + " enemies checked, " + warnings + " warnings");
	}
}
