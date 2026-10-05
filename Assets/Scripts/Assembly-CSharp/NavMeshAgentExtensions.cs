using UnityEngine;
using UnityEngine.AI;

public static class NavMeshAgentExtensions
{
	public static void MoveTo(this NavMeshAgent agent, Vector3 destination, float speed = 10f)
	{
		if (agent.isOnNavMesh)
		{
			agent.isStopped = false;
			agent.speed = speed;
			agent.acceleration = 100f;
			if (Vector3.Distance(agent.destination, destination) > 0.1f)
			{
				agent.SetDestination(destination);
			}
		}
	}
}
