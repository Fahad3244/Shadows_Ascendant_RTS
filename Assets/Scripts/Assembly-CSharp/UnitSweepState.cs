using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class UnitSweepState : UnitStateBase
{
	private Queue<Vector3> _path;

	private ITargetable _finalTarget;

	private Vector3 _currentDestination;

	private const float NODE_TRANSITION_DISTANCE = 1.25f;

	private const float ROUTE_SAMPLE_STEP = 1f;

	private const float ROUTE_NAV_SAMPLE_RADIUS = 1.5f;

	private const float ROUTE_REACHABLE_MAX_DISTANCE = 100f;

	private bool _isPathActive;

	private bool _needsDestinationUpdate;

	public UnitSweepState(UnitAgent agent)
		: base(agent)
	{
	}

	public void SetSweepCommand(List<Vector3> nodes, ITargetable target)
	{
		_finalTarget = target;
		_path = new Queue<Vector3>(BuildEntryPath(nodes));
		TryGetNextNode();
	}

	private List<Vector3> BuildEntryPath(List<Vector3> nodes)
	{
		if (nodes == null || nodes.Count == 0) return new List<Vector3>();
		if (nodes.Count == 1) return new List<Vector3>(nodes);

		Vector3 agentPos = _agent.transform.position;
		if (NavMesh.SamplePosition(agentPos, out NavMeshHit startHit, 2f, NavMesh.AllAreas))
			agentPos = startHit.position;

		NavMeshPath navPath = new NavMeshPath();
		float bestLength = float.MaxValue;
		Vector3 bestPoint = Vector3.zero;
		int bestNextIndex = -1;

		for (int i = 0; i < nodes.Count - 1; i++)
		{
			Vector3 a = nodes[i];
			Vector3 b = nodes[i + 1];
			int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / ROUTE_SAMPLE_STEP));

			for (int s = 0; s <= steps; s++)
			{
				Vector3 candidate = Vector3.Lerp(a, b, (float)s / steps);
				if (Vector3.Distance(agentPos, candidate) > ROUTE_REACHABLE_MAX_DISTANCE) continue;
				if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, ROUTE_NAV_SAMPLE_RADIUS, NavMesh.AllAreas)) continue;

				// Path length can never be shorter than straight distance, so skip hopeless candidates cheaply
				if (Vector3.Distance(agentPos, hit.position) >= bestLength) continue;

				if (!NavMesh.CalculatePath(agentPos, hit.position, NavMesh.AllAreas, navPath)) continue;
				if (navPath.status != NavMeshPathStatus.PathComplete) continue;

				float len = GetPathLength(navPath);
				if (len < bestLength)
				{
					bestLength = len;
					bestPoint = hit.position;
					bestNextIndex = i + 1;
				}
			}
		}

		// Nothing reachable: final fallback is the full route from the start marker
		if (bestNextIndex == -1) return new List<Vector3>(nodes);

		List<Vector3> joined = new List<Vector3> { bestPoint };
		for (int j = bestNextIndex; j < nodes.Count; j++)
		{
			if (j == bestNextIndex && Vector3.Distance(bestPoint, nodes[j]) < 0.1f) continue;
			joined.Add(nodes[j]);
		}
		return joined;
	}

	private static float GetPathLength(NavMeshPath path)
	{
		Vector3[] c = path.corners;
		float len = 0f;
		for (int i = 1; i < c.Length; i++) len += Vector3.Distance(c[i - 1], c[i]);
		return len;
	}

	public override void Enter()
	{
		_agent.NavAgent.isStopped = false;
		_agent.NavAgent.stoppingDistance = 0.2f;
		_agent.ApplyMovementDynamics(_agent.BaseMoveSpeed * 1.25f, _agent.BaseAcceleration * 1.5f);
		_isPathActive = true;
	}

	public override void Update()
	{
		if (!_isPathActive)
		{
			return;
		}
		if (!_agent.NavAgent.isOnNavMesh)
		{
			_needsDestinationUpdate = true;
			return;
		}

		float num = Vector3.Distance(GetFlatPosition(_agent.transform.position), GetFlatPosition(_currentDestination));
		if (num > _agent.LeashDistance * 0.4f)
		{
			_agent.NavAgent.speed = _agent.BaseMoveSpeed * 1.8f;
		}
		else
		{
			_agent.NavAgent.speed = _agent.BaseMoveSpeed * 1.25f;
		}
		if (num <= 1.25f)
		{
			TryGetNextNode();
		}
		else if (_needsDestinationUpdate || (!_agent.NavAgent.hasPath && !_agent.NavAgent.pathPending))
		{
			if (Vector3.Distance(_agent.NavAgent.destination, _currentDestination) > 0.1f)
			{
				_agent.NavAgent.SetDestination(_currentDestination);
			}
			_needsDestinationUpdate = false;
		}
	}

	private void TryGetNextNode()
	{
		if (_path != null && _path.Count > 0)
		{
			_currentDestination = _path.Dequeue();
			if (_agent.NavAgent.isOnNavMesh)
			{
				_agent.NavAgent.SetDestination(_currentDestination);
				_needsDestinationUpdate = false;
			}
			else
			{
				_needsDestinationUpdate = true;
			}
			return;
		}
		_isPathActive = false;
		if (_finalTarget != null && _finalTarget as Object != null)
		{
			AttachableTarget component = _finalTarget.GetTransform().GetComponent<AttachableTarget>();
			if (component != null && component.TryReserveSlot(_agent))
			{
				_agent.SetReservedTarget(component);
				_agent.MoveToTarget(component);
				return;
			}
		}
		_agent.MoveTo(_agent.transform.position);
	}

	public override void Exit()
	{
		_agent.ApplyMovementDynamics();
		_path?.Clear();
		_isPathActive = false;
	}

	private Vector3 GetFlatPosition(Vector3 pos)
	{
		return new Vector3(pos.x, 0f, pos.z);
	}
}
