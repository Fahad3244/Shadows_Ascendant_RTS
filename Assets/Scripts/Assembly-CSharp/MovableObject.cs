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

	private AttachableTarget _attach;

	private NavMeshAgent _agent;

	private Waypoint _targetWaypoint;

	private float _repathTimer;

	private float _manualUntil;

	private bool _delivered;

	private bool _routeOpen;

	private NavMeshPath _path;

	[Header("Debug")]
	[SerializeField]
	private CarryState state;

	public CarryState State => state;

	private void Awake()
	{
		_attach = GetComponent<AttachableTarget>();
		_agent = GetComponent<NavMeshAgent>();
		_agent.acceleration = acceleration;
		_agent.angularSpeed = angularSpeed;
		_agent.stoppingDistance = 0f;
		_agent.autoBraking = true;
		_path = new NavMeshPath();
	}

	private void Update()
	{
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
				_targetWaypoint = Waypoint.NearestReachablePlayerOwned(transform.position);
				_routeOpen = _targetWaypoint != null;
			}
		}

		if (fixedDestination == null && _targetWaypoint == null) _routeOpen = false;

		if (!_routeOpen)
		{
			Halt();
			state = (fixedDestination == null && !Waypoint.AnyPlayerOwned()) ? CarryState.NoWaypoint : CarryState.Blocked;
			return;
		}

		Vector3 dest = fixedDestination != null ? fixedDestination.position : _targetWaypoint.transform.position;
		Vector3 flat = dest - transform.position;
		flat.y = 0f;
		if (flat.magnitude <= arriveDistance)
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