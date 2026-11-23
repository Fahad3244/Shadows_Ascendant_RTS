using UnityEngine;
using UnityEngine.AI;

public class PlayerAttachment : MonoBehaviour
{
	public static PlayerAttachment Instance { get; private set; }

	[Tooltip("Optional. Use Attach_Player (4).")]
	[SerializeField] private AttachmentSettings settings;
	[SerializeField] private int maxPoints = 4;
	[SerializeField] private float radius = 1.6f;

	[Header("Test (Y = spawn 6 weak enemies around you, Y again = remove)")]
	[SerializeField] private bool testInvulnerable = true;

	private EnemyBase[] _slot;
	private bool[] _occupied;
	private GameObject[] _spawned = new GameObject[0];
	private Health _health;

	public int Capacity => _slot.Length;

	private void Awake()
	{
		Instance = this;
		if (settings != null) maxPoints = settings.maxAttachmentPoints;
		_slot = new EnemyBase[maxPoints];
		_occupied = new bool[maxPoints];
		_health = GetComponent<Health>();
	}

	private Vector3 PointPos(int i)
	{
		float a = (i * 360f / maxPoints + 45f) * Mathf.Deg2Rad;
		return transform.position + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * radius;
	}

	private void Prune()
	{
		for (int i = 0; i < _slot.Length; i++)
		{
			if (_slot[i] == null || _slot[i].State == EnemyBase.EnemyState.Dead)
			{
				_slot[i] = null;
				_occupied[i] = false;
			}
		}
	}

	public bool TryReserve(EnemyBase e, out int index)
	{
		Prune();
		for (int i = 0; i < _slot.Length; i++)
		{
			if (_slot[i] == e) { index = i; return true; }
		}
		int best = -1;
		float bestD = float.MaxValue;
		for (int i = 0; i < _slot.Length; i++)
		{
			if (_slot[i] != null) continue;
			float d = (PointPos(i) - e.transform.position).sqrMagnitude;
			if (d < bestD) { bestD = d; best = i; }
		}
		index = best;
		if (best < 0) return false;
		_slot[best] = e;
		_occupied[best] = false;
		if (CombatDebug.Enabled) Debug.Log($"[PlayerAttach] RESERVED point {best} for {e.name} {Summary()}");
		return true;
	}

	public void Confirm(EnemyBase e)
	{
		for (int i = 0; i < _slot.Length; i++)
		{
			if (_slot[i] == e && !_occupied[i])
			{
				_occupied[i] = true;
				if (CombatDebug.Enabled) Debug.Log($"[PlayerAttach] ATTACHED {e.name} point {i} {Summary()}");
			}
		}
	}

	public void Release(EnemyBase e)
	{
		for (int i = 0; i < _slot.Length; i++)
		{
			if (_slot[i] == e)
			{
				_slot[i] = null;
				_occupied[i] = false;
				if (CombatDebug.Enabled) Debug.Log($"[PlayerAttach] RELEASED {e.name} point {i} {Summary()}");
			}
		}
	}

	public bool GetPointPosition(int i, out Vector3 pos)
	{
		pos = default;
		if (i < 0 || i >= _slot.Length) return false;
		pos = PointPos(i);
		return true;
	}

	public string Summary()
	{
		System.Text.StringBuilder sb = new System.Text.StringBuilder("[");
		for (int i = 0; i < _slot.Length; i++)
			sb.Append(_slot[i] == null ? 'A' : (_occupied[i] ? 'O' : 'R'));
		return sb.Append(']').ToString();
	}

	public int OccupiedCount()
	{
		int n = 0;
		for (int i = 0; i < _slot.Length; i++) if (_occupied[i]) n++;
		return n;
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Y)) ToggleSpawn();
		if (testInvulnerable && _spawned.Length > 0 && _health != null && !_health.IsDead)
			_health.Heal(500f);
	}

	private void ToggleSpawn()
	{
		if (_spawned.Length > 0)
		{
			foreach (GameObject g in _spawned) if (g != null) Destroy(g);
			_spawned = new GameObject[0];
			return;
		}
		EnemyBase tpl = null;
		foreach (EnemyBase e in FindObjectsOfType<EnemyBase>())
		{
			if (e.Data == null || e.Data.passive) continue;
			if (tpl == null || e.Data.attackDamage < tpl.Data.attackDamage) tpl = e;
		}
		if (tpl == null) { Debug.LogWarning("[PlayerAttach] No non-passive enemy to clone."); return; }
		_spawned = new GameObject[6];
		for (int i = 0; i < 6; i++)
		{
			float a = i * 60f * Mathf.Deg2Rad;
			Vector3 p = transform.position + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 7f;
			if (NavMesh.SamplePosition(p, out NavMeshHit hit, 4f, NavMesh.AllAreas)) p = hit.position;
			GameObject g = Instantiate(tpl.gameObject, p, Quaternion.LookRotation(transform.position - p));
			g.name = "TestSpawn_" + i;
			g.SetActive(true);
			EnemyBase spawned = g.GetComponent<EnemyBase>();
			if (spawned != null) spawned.SetIgnorePlayer(false);
			_spawned[i] = g;
		}
	}

	private void OnDrawGizmosSelected()
	{
		for (int i = 0; i < maxPoints; i++)
		{
			Gizmos.color = Application.isPlaying && _slot != null && i < _slot.Length && _slot[i] != null ? (_occupied[i] ? Color.red : Color.yellow) : Color.cyan;
			Gizmos.DrawSphere(PointPos(i), 0.25f);
		}
	}
}
