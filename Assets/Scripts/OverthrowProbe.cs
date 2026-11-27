using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AttachmentLog
{
	private static readonly Queue<string> Lines = new Queue<string>();

	public static void Add(string s)
	{
		Lines.Enqueue(Mathf.FloorToInt(Time.timeSinceLevelLoad) + "s  " + s);
		while (Lines.Count > 7) Lines.Dequeue();
	}

	public static void Clear() { Lines.Clear(); }
	public static string Text() { return string.Join("\n", Lines.ToArray()); }
}

// Logs attachment events so the Phase 1 hooks can be seen working. Phase 2 subscribes to the same events.
public class OverthrowProbe : MonoBehaviour
{
	private static OverthrowProbe _instance;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Boot()
	{
		if (_instance != null) return;
		GameObject go = new GameObject("OverthrowProbe");
		_instance = go.AddComponent<OverthrowProbe>();
		DontDestroyOnLoad(go);
		SceneManager.sceneLoaded += (s, m) => AttachmentLog.Clear();
	}

	private void OnEnable()
	{
		AttachableTarget.Attached += OnAttached;
		AttachableTarget.Detached += OnDetached;
		AttachableTarget.FirstAttached += OnFirst;
		AttachableTarget.AllDetached += OnAll;
		EnemyBase.StunStarted += OnStunStarted;
		EnemyBase.StunRecovered += OnStunRecovered;
	}

	private void OnDisable()
	{
		AttachableTarget.Attached -= OnAttached;
		AttachableTarget.Detached -= OnDetached;
		AttachableTarget.FirstAttached -= OnFirst;
		AttachableTarget.AllDetached -= OnAll;
		EnemyBase.StunStarted -= OnStunStarted;
		EnemyBase.StunRecovered -= OnStunRecovered;
	}

	private void OnAttached(AttachableTarget t, UnitAgent a)
	{
		if (t == null || !t.SupportsOverthrow) return;
		AttachmentLog.Add("ATTACHED  " + a.Type + " -> " + t.name + "  (" + t.AttachedUnitCount + "/" + t.MaxSlots + ")");
	}

	private void OnFirst(AttachableTarget t)
	{
		if (t == null || !t.SupportsOverthrow) return;
		AttachmentLog.Add("FIRST ATTACKER on " + t.name + (t.IsTargetStunned ? "  [stunned: Overthrow waits]" : "  -> Overthrow may start"));
	}

	private void OnDetached(AttachableTarget t, UnitAgent a, DetachCause c)
	{
		if (t == null || !t.SupportsOverthrow) return;
		AttachmentLog.Add("DETACHED  " + (a != null ? a.Type.ToString() : "?") + " from " + t.name + "  (" + c + ")");
	}

	private void OnAll(AttachableTarget t)
	{
		if (t == null || !t.SupportsOverthrow) return;
		AttachmentLog.Add("NO ATTACKERS left on " + t.name + "  -> Overthrow resets");
	}

	private void OnStunStarted(EnemyBase e)
	{
		if (e == null) return;
		AttachmentLog.Add("STUNNED  " + e.name + "  attached=" + e.AttachedCount + (e.AttachedCount > 0 ? "  -> Overthrow paused" : ""));
	}

	private void OnStunRecovered(EnemyBase e)
	{
		if (e == null) return;
		AttachmentLog.Add("STUN ENDED  " + e.name + "  attached=" + e.AttachedCount + (e.AttachedCount > 0 ? "  -> Overthrow may start now" : ""));
	}
}
