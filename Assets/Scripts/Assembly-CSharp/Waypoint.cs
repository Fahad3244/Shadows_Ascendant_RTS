using UnityEngine;
using UnityEngine.AI;

public class Waypoint : MonoBehaviour
{
	public WaypointOwner owner = WaypointOwner.Player;

	public static Waypoint Nearest(Vector3 position)
	{
		Waypoint[] array = Object.FindObjectsOfType<Waypoint>();
		Waypoint result = null;
		float num = float.MaxValue;
		Waypoint[] array2 = array;
		foreach (Waypoint waypoint in array2)
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
		foreach (Waypoint w in Object.FindObjectsOfType<Waypoint>())
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
		foreach (Waypoint w in Object.FindObjectsOfType<Waypoint>())
			if (w.owner == WaypointOwner.Player) return true;
		return false;
	}

	public static Waypoint NearestReachablePlayerOwned(Vector3 from)
	{
		NavMeshPath path = new NavMeshPath();
		Waypoint best = null;
		float bestLen = float.MaxValue;
		foreach (Waypoint w in Object.FindObjectsOfType<Waypoint>())
		{
			if (w.owner != WaypointOwner.Player) continue;
			if (!NavMesh.SamplePosition(w.transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas)) continue;
			if (!NavMesh.CalculatePath(from, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) continue;
			float len = 0f;
			for (int i = 1; i < path.corners.Length; i++) len += Vector3.Distance(path.corners[i - 1], path.corners[i]);
			if (len < bestLen) { bestLen = len; best = w; }
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