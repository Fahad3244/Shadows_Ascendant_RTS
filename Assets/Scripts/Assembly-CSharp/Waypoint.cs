using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Waypoint : MonoBehaviour
{
	public static readonly List<Waypoint> All = new List<Waypoint>();

	public static event Action<Waypoint, WaypointOwner, WaypointOwner> OnOwnerChanged;

	public WaypointOwner owner = WaypointOwner.Player;

	[Header("Zone")]
	[SerializeField]
	private float zoneRadius = 8f;

	[SerializeField]
	private bool showZoneRing = true;

	[Header("Capture")]
	[SerializeField]
	private bool capturable = true;

	[Tooltip("Seconds for ONE unit to capture alone.")]
	[SerializeField]
	private float captureSeconds = 8f;

	[Tooltip("Extra capture speed per additional unit (0.25 = +25%).")]
	[SerializeField]
	private float extraUnitBonus = 0.25f;

	[SerializeField]
	private int maxCountedUnits = 6;

	[Tooltip("Progress lost per second when nobody is capturing.")]
	[SerializeField]
	private float decayPerSecond = 0.1f;

	[SerializeField]
	private LayerMask troopLayers;

	[SerializeField]
	private LayerMask enemyLayers;

	[SerializeField]
	private float scanInterval = 0.25f;

	[Header("Visuals")]
	[SerializeField]
	private Color playerColor = new Color(0.2f, 0.5f, 1f);

	[SerializeField]
	private Color enemyColor = new Color(1f, 0.25f, 0.2f);

	[SerializeField]
	private Color neutralColor = Color.gray;

	[Header("Debug / Testing")]
	[SerializeField]
	private int debugFakePlayerUnits;

	[SerializeField]
	private int debugFakeEnemyUnits;

	[SerializeField]
	[Range(0f, 1f)]
	private float captureProgress;

	[SerializeField]
	private WaypointOwner capturingSide = WaypointOwner.Neutral;

	[SerializeField]
	private int playerUnitsInside;

	[SerializeField]
	private int enemyUnitsInside;

	[SerializeField]
	private bool isContested;

	private readonly Collider[] _hits = new Collider[64];

	private readonly HashSet<Component> _seen = new HashSet<Component>();

	private float _scanTimer;

	private Renderer _rend;

	private MaterialPropertyBlock _mpb;

	private LineRenderer _ring;

	public float ZoneRadius => zoneRadius;

	public bool IsContested => isContested;

	public bool Contains(Vector3 pos)
	{
		Vector3 d = pos - transform.position;
		d.y = 0f;
		return d.magnitude <= zoneRadius;
	}

	private void Awake()
	{
		_rend = GetComponent<Renderer>();
		_mpb = new MaterialPropertyBlock();
		if (showZoneRing)
		{
			BuildRing();
		}
	}

	private void OnEnable()
	{
		if (!All.Contains(this))
		{
			All.Add(this);
		}
	}

	private void OnDisable()
	{
		All.Remove(this);
	}

	private void Update()
	{
		_scanTimer -= Time.deltaTime;
		if (_scanTimer <= 0f)
		{
			_scanTimer = scanInterval;
			CountUnits();
		}
		if (capturable)
		{
			TickCapture(Time.deltaTime);
		}
		UpdateVisuals();
	}

	private void CountUnits()
	{
		playerUnitsInside = 0;
		enemyUnitsInside = 0;
		_seen.Clear();
		int n = Physics.OverlapSphereNonAlloc(transform.position, zoneRadius, _hits, troopLayers | enemyLayers);
		for (int i = 0; i < n; i++)
		{
			UnitAgent troop = _hits[i].GetComponentInParent<UnitAgent>();
			if (troop != null)
			{
				if (troop.CurrentStateEnum != UnitState.Dead && _seen.Add(troop))
				{
					playerUnitsInside++;
				}
				continue;
			}
			EnemyBase enemy = _hits[i].GetComponentInParent<EnemyBase>();
			if (enemy != null)
			{
				Health h = enemy.GetComponent<Health>();
				if ((h == null || !h.IsDead) && _seen.Add(enemy))
				{
					enemyUnitsInside++;
				}
			}
		}
		playerUnitsInside += debugFakePlayerUnits;
		enemyUnitsInside += debugFakeEnemyUnits;
	}

	private float RateFor(WaypointOwner side)
	{
		int count = Mathf.Clamp(side == WaypointOwner.Player ? playerUnitsInside : enemyUnitsInside, 1, maxCountedUnits);
		return (1f + (count - 1) * extraUnitBonus) / Mathf.Max(0.01f, captureSeconds);
	}

	private void TickCapture(float dt)
	{
		bool pl = playerUnitsInside > 0;
		bool en = enemyUnitsInside > 0;
		isContested = pl && en;
		if (isContested)
		{
			return;
		}
		WaypointOwner pusher = (pl ? WaypointOwner.Player : (en ? WaypointOwner.Enemy : WaypointOwner.Neutral));
		if (pusher == WaypointOwner.Neutral || pusher == owner)
		{
			captureProgress = Mathf.MoveTowards(captureProgress, 0f, decayPerSecond * dt);
			if (captureProgress <= 0f)
			{
				capturingSide = WaypointOwner.Neutral;
			}
			return;
		}
		if (capturingSide != pusher && captureProgress > 0f)
		{
			captureProgress -= RateFor(pusher) * dt;
			if (captureProgress <= 0f)
			{
				captureProgress = 0f;
				capturingSide = pusher;
			}
			return;
		}
		capturingSide = pusher;
		captureProgress += RateFor(pusher) * dt;
		if (captureProgress >= 1f)
		{
			SetOwner(pusher);
		}
	}

	public void SetOwner(WaypointOwner newOwner)
	{
		WaypointOwner old = owner;
		owner = newOwner;
		captureProgress = 0f;
		capturingSide = WaypointOwner.Neutral;
		if (old != newOwner)
		{
			Debug.Log($"[Waypoint] {name}: {old} -> {newOwner}");
			Waypoint.OnOwnerChanged?.Invoke(this, old, newOwner);
		}
	}

	private Color ColorFor(WaypointOwner o)
	{
		return (o == WaypointOwner.Player) ? playerColor : ((o == WaypointOwner.Enemy) ? enemyColor : neutralColor);
	}

	private void UpdateVisuals()
	{
		Color c = ColorFor(owner);
		if (captureProgress > 0f && capturingSide != WaypointOwner.Neutral)
		{
			c = Color.Lerp(c, ColorFor(capturingSide), captureProgress);
		}
		if (_rend != null)
		{
			_rend.GetPropertyBlock(_mpb);
			_mpb.SetColor("_BaseColor", c);
			_mpb.SetColor("_Color", c);
			_rend.SetPropertyBlock(_mpb);
		}
		if (_ring != null)
		{
			Color rc = new Color(c.r, c.g, c.b, 0.8f);
			_ring.startColor = rc;
			_ring.endColor = rc;
		}
	}

	private void BuildRing()
	{
		GameObject go = new GameObject("ZoneRing");
		go.transform.SetParent(transform, false);
		_ring = go.AddComponent<LineRenderer>();
		_ring.useWorldSpace = true;
		_ring.loop = true;
		_ring.widthMultiplier = 0.15f;
		_ring.material = new Material(Shader.Find("Sprites/Default"));
		int seg = 48;
		_ring.positionCount = seg;
		Vector3 c = transform.position + Vector3.up * 0.1f;
		for (int i = 0; i < seg; i++)
		{
			float a = (float)i * Mathf.PI * 2f / (float)seg;
			_ring.SetPosition(i, c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * zoneRadius);
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, zoneRadius);
	}

	public static Waypoint Nearest(Vector3 position)
	{
		Waypoint result = null;
		float num = float.MaxValue;
		foreach (Waypoint waypoint in All)
		{
			float num2 = Vector3.Distance(position, waypoint.transform.position);
			if (num2 < num)
			{
				num = num2;
				result = waypoint;
			}
		}
		return result;
	}

	public static Waypoint NearestPlayerOwned(Vector3 position)
	{
		return NearestOwned(position, WaypointOwner.Player);
	}

	public static Waypoint NearestOwned(Vector3 position, WaypointOwner wantedOwner)
	{
		Waypoint result = null;
		float best = float.MaxValue;
		foreach (Waypoint w in All)
		{
			if (w.owner != wantedOwner)
			{
				continue;
			}
			float d = Vector3.Distance(position, w.transform.position);
			if (d < best)
			{
				best = d;
				result = w;
			}
		}
		return result;
	}

	public static bool AnyPlayerOwned()
	{
		foreach (Waypoint w in All)
		{
			if (w.owner == WaypointOwner.Player)
			{
				return true;
			}
		}
		return false;
	}

	public static Waypoint NearestReachablePlayerOwned(Vector3 from)
	{
		NavMeshPath path = new NavMeshPath();
		Waypoint best = null;
		float bestLen = float.MaxValue;
		foreach (Waypoint w in All)
		{
			if (w.owner != WaypointOwner.Player)
			{
				continue;
			}
			if (!NavMesh.SamplePosition(w.transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
			{
				continue;
			}
			if (!NavMesh.CalculatePath(from, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
			{
				continue;
			}
			float len = 0f;
			for (int i = 1; i < path.corners.Length; i++)
			{
				len += Vector3.Distance(path.corners[i - 1], path.corners[i]);
			}
			if (len < bestLen)
			{
				bestLen = len;
				best = w;
			}
		}
		return best;
	}
}

public enum WaypointOwner
{
	Player,
	Enemy,
	Neutral
}
