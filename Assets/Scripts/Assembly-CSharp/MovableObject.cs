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
		NoWaypoint
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
		Vector3 dest;
		if (fixedDestination != null)
		{
			dest = fixedDestination.position;
		}
		else
		{
			_repathTimer -= Time.deltaTime;
			if (_repathTimer <= 0f || _targetWaypoint == null)
			{
				_repathTimer = repathInterval;
				_targetWaypoint = Waypoint.NearestPlayerOwned(transform.position);
			}
			if (_targetWaypoint == null)
			{
				Halt();
				state = CarryState.NoWaypoint;
				return;
			}
			dest = _targetWaypoint.transform.position;
		}
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
}