using UnityEngine;

[RequireComponent(typeof(AttachableTarget))]
public class ResourceSource : MonoBehaviour
{
	[SerializeField]
	private int resourceValue = 10;

	public int TakeChunk()
	{
		return resourceValue;
	}
}
