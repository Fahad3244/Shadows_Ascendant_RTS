using UnityEngine;

public static class GlobalEvents
{
	public static readonly EventChannel<bool> OnBuildModeChanged = new EventChannel<bool>();

	public static readonly EventChannel<GameObject> OnBuildingPlaced = new EventChannel<GameObject>();

	public static readonly EventChannel<GameObject> OnReadyToMove = new EventChannel<GameObject>();
}
