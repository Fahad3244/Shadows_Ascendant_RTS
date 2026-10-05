using UnityEngine;

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
}

public enum WaypointOwner
{
	Player,
	Enemy,
	Neutral
}