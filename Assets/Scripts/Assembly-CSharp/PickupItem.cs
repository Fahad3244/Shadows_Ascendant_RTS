using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AttachableTarget))]
public class PickupItem : MonoBehaviour
{
	[SerializeField]
	private LootType lootType = LootType.Gold;

	[SerializeField]
	private int amount = 10;

	[SerializeField]
	private UnityEvent onDelivered;

	public static event Action<LootType, int> OnPickupDelivered;

	private bool _collected;

	public bool TryCollect(UnitAgent agent)
	{
		if (_collected)
		{
			return false;
		}
		_collected = true;
		gameObject.SetActive(false);
		return true;
	}

	public void Deliver()
	{
		Debug.Log($"[Pickup] Delivered {amount} {lootType}");
		OnPickupDelivered?.Invoke(lootType, amount);
		onDelivered.Invoke();
		Destroy(gameObject);
	}

	public void Drop(Vector3 position)
	{
		_collected = false;
		transform.position = position;
		gameObject.SetActive(true);
	}
}