using UnityEngine;

public class Waypoint : MonoBehaviour
{
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
}
