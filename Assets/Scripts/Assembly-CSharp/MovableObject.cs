using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(AttachableTarget))]
[RequireComponent(typeof(NavMeshAgent))]
public class MovableObject : MonoBehaviour
{
	public enum CarryState
	{
		Idle,
		AutoCarry,
		Manual,
		Delivered,
		NoWaypoint,
		Blocked
	}

	[Header("Auto Carry")]
	[SerializeField]
	private bool autoCarry = true;

	[Tooltip("Flat distance to the waypoint at which the object counts as delivered.")]
	[SerializeField]
	private float arriveDistance = 2f;

	[SerializeField]
	private float repathInterval = 0.25f;

	[SerializeField]
	private float acceleration = 8f;

	[SerializeField]
	private float angularSpeed = 120f;

	[Header("Manual Control")]
	[Tooltip("Seconds after the last sweep input before auto-carry resumes.")]
	[SerializeField]
	private float manualResumeDelay = 0.6f;

	[Tooltip("Speed multiplier while the player sweeps the object manually.")]
	[SerializeField]
	private float manualSpeedMultiplier = 1.5f;

	[Header("Retrievable (optional)")]
	[Tooltip("If set, the object is carried here instead of the nearest waypoint.")]
	[SerializeField]
	private Transform fixedDestination;

	[SerializeField]
	private bool destroyOnDelivery;

	[SerializeField]
	private UnityEvent onDelivered;

	[Header("Waypoint Zone")]
	[Tooltip("Delivered when within this fraction of the zone radius from the waypoint centre (1 = edge, 0.5 = halfway).")]
	[SerializeField]
	[Range(0.1f, 1f)]
	private float deliverRadiusFraction = 0.6f;

	[Tooltip("Max distance to a valid waypoint for reassess/redirect. 0 = unlimited.")]
	[SerializeField]
	private float maxWaypointRange = 60f;

	[Tooltip("Optional child shown only while Active (ring, light, glow).")]
	[SerializeField]
	private GameObject activeVisual;

	[SerializeField]
	private UnityEvent onActivated;

	[SerializeField]
	private UnityEvent onDeactivated;

	[SerializeField]
	private bool isActive;

	[Header("Retrievable Marker")]
	[Tooltip("Shown while the object is targeted or being carried. Make it a child of the destination object.")]
	[SerializeField]
	private GameObject destinationMarker;

	private AttachableTarget _attach;

	private PlayerTargetingManager _ptm;

	private TargetableEntity _targetable;

	private NavMeshAgent _agent;

	private Waypoint _targetWaypoint;

	private Waypoint _homeWaypoint;

	private float _repathTimer;

	private float _manualUntil;

	private bool _delivered;

	private bool _routeOpen;

	private NavMeshPath _path;

	[Header("Debug")]
	[SerializeField]
	private CarryState state;

	public CarryState State => state;

	public bool IsActive => isActive;

	private void Awake()
	{
		_attach = GetComponent<AttachableTarget>();
		_agent = GetComponent<NavMeshAgent>();
		_agent.acceleration = acceleration;
		_agent.angularSpeed = angularSpeed;
		_agent.stoppingDistance = 0f;
		_agent.autoBraking = true;
		_path = new NavMeshPath();
		_ptm = FindObjectOfType<PlayerTargetingManager>();
		_targetable = GetComponent<TargetableEntity>();
		if (destinationMarker)
		{
			destinationMarker.SetActive(false);
		}
		if (activeVisual)
		{
			activeVisual.SetActive(false);
		}
	}

	private void OnEnable()
	{
		Waypoint.OnPlayerLostWaypoint += HandleWaypointLost;
	}

	private void OnDisable()
	{
		Waypoint.OnPlayerLostWaypoint -= HandleWaypointLost;
	}

	private void HandleWaypointLost(Waypoint wp)
	{
		if (wp != _homeWaypoint)
		{
			return;
		}
		Debug.Log($"[Movable] {name}: lost {wp.name}, reassessing");
		_homeWaypoint = null;
		if (_delivered)
		{
			_delivered = false;
			_attach.SetLocked(false);
		}
		_repathTimer = 0f;
	}

	private void UpdateDestinationMarker()
	{
		if (destinationMarker == null)
		{
			return;
		}
		bool targeted = _ptm != null && _targetable != null && _ptm.CurrentTarget == (ITargetable)_targetable;
		bool show = fixedDestination != null && !_delivered && (targeted || _attach.AttachedUnitCount > 0);
		if (destinationMarker.activeSelf != show)
		{
			destinationMarker.SetActive(show);
		}
	}

	private void UpdateActive()
	{
		if (fixedDestination != null)
		{
			return;
		}
		bool inside = Waypoint.FindPlayerZoneContaining(transform.position) != null;
		if (inside == isActive)
		{
			return;
		}
		isActive = inside;
		if (activeVisual)
		{
			activeVisual.SetActive(inside);
		}
		if (inside)
		{
			onActivated.Invoke();
		}
		else
		{
			onDeactivated.Invoke();
		}
	}

	private void Update()
	{
		UpdateActive();
		UpdateDestinationMarker();
		if (!_attach.CanBeMoved)
		{
			Halt();
			if (!_delivered)
			{
				state = CarryState.Idle;
			}
			return;
		}
		float baseSpeed = _attach.GetMoveSpeed();
		if (Time.time < _manualUntil)
		{
			_agent.speed = baseSpeed * manualSpeedMultiplier;
			state = CarryState.Manual;
			return;
		}
		_agent.speed = baseSpeed;
		if (!autoCarry)
		{
			Halt();
			return;
		}
		AutoCarry();
	}

	// Called every frame by SwarmController while the player sweeps this object
	public void ManualMove(Vector3 position)
	{
		if (!_attach.CanBeMoved || !_agent.isOnNavMesh)
		{
			return;
		}
		_manualUntil = Time.time + manualResumeDelay;
		_delivered = false;
		_agent.isStopped = false;
		_agent.speed = _attach.GetMoveSpeed() * manualSpeedMultiplier;
		if (Vector3.Distance(_agent.destination, position) > 0.1f)
		{
			_agent.SetDestination(position);
		}
	}

	private void AutoCarry()
	{
		if (_delivered)
		{
			Halt();
			state = CarryState.Delivered;
			return;
		}
		_repathTimer -= Time.deltaTime;
		if (_repathTimer <= 0f)
		{
			_repathTimer = repathInterval;
			if (fixedDestination != null)
			{
				_targetWaypoint = null;
				_routeOpen = IsReachable(fixedDestination.position);
			}
			else
			{
				Waypoint prev = _targetWaypoint;
				_targetWaypoint = Waypoint.NearestReachablePlayerOwned(transform.position, maxWaypointRange);
				_routeOpen = _targetWaypoint != null;
				if (prev != null && _targetWaypoint != prev)
				{
					Debug.Log($"[Movable] {name} redirect: {prev.name} -> {((_targetWaypoint != null) ? _targetWaypoint.name : "none")}");
				}
			}
		}

		if (fixedDestination == null && _targetWaypoint == null) _routeOpen = false;

		if (!_routeOpen)
		{
			Halt();
			state = (fixedDestination == null && !Waypoint.AnyPlayerOwned()) ? CarryState.NoWaypoint : CarryState.Blocked;
			return;
		}

		Vector3 dest = fixedDestination != null ? fixedDestination.position : _targetWaypoint.ZoneCenter;
		Vector3 flat = dest - transform.position;
		flat.y = 0f;
		float arrive = (fixedDestination == null && _targetWaypoint != null) ? Mathf.Max(arriveDistance, _targetWaypoint.ZoneRadius * deliverRadiusFraction) : arriveDistance;
		if (flat.magnitude <= arrive)
		{
			Deliver();
			return;
		}
		state = CarryState.AutoCarry;
		if (!_agent.isOnNavMesh)
		{
			return;
		}
		_agent.isStopped = false;
		if (Vector3.Distance(_agent.destination, dest) > 0.5f || (!_agent.hasPath && !_agent.pathPending))
		{
			_agent.SetDestination(dest);
		}
	}

	private void Deliver()
	{
		_homeWaypoint = ((fixedDestination == null) ? _targetWaypoint : null);
		_delivered = true;
		state = CarryState.Delivered;
		Halt();
		string where = fixedDestination != null ? fixedDestination.name : _targetWaypoint?.name ?? "destination";
		Debug.Log($"[Movable] {name} delivered to {where}");
		_attach.ReleaseAllUnits();
		_attach.SetLocked(true);
		onDelivered.Invoke();
		if (destroyOnDelivery)
		{
			Destroy(gameObject);
		}
	}

	private void Halt()
	{
		if (_agent.isOnNavMesh)
		{
			_agent.isStopped = true;
		}
	}

	private bool IsReachable(Vector3 dest)
	{
		if (!_agent.isOnNavMesh) return true;
		if (!NavMesh.SamplePosition(dest, out NavMeshHit hit, 3f, NavMesh.AllAreas)) return false;
		return NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, _path)
			&& _path.status == NavMeshPathStatus.PathComplete;
	}
}