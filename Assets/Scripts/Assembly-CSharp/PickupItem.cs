using System;
using UnityEngine;
using UnityEngine.Events;

public enum PickupMode
{
	ReturnToPlayer,
	EquipToTroop
}

[RequireComponent(typeof(AttachableTarget))]
public class PickupItem : MonoBehaviour
{
	[SerializeField]
	private LootType lootType = LootType.Gold;

	[SerializeField]
	private int amount = 10;

	[SerializeField]
	private UnityEvent onDelivered;

	[Header("Mode")]
	[SerializeField]
	private PickupMode mode = PickupMode.ReturnToPlayer;

	[Header("Gear (Equip To Troop)")]
	[SerializeField]
	private string gearName = "Gear";

	[Tooltip("0.5 = +50% damage for the troop that equips it.")]
	[SerializeField]
	private float damageBonus = 0.5f;

	[SerializeField]
	private GameObject equippedVisualPrefab;

	public static event Action<LootType, int> OnPickupDelivered;

	public static event Action<UnitAgent, PickupItem> OnGearEquipped;

	private bool _collected;

	public string GearName => gearName;

	public bool TryCollect(UnitAgent agent)
	{
		if (_collected)
		{
			return false;
		}
		_collected = true;
		gameObject.SetActive(false);
		if (mode == PickupMode.EquipToTroop && agent.InternalData != null && !agent.InternalData.hasGear)
		{
			agent.EquipGear(gearName, damageBonus, equippedVisualPrefab);
			Debug.Log($"[Pickup] {agent.name} equipped {gearName} (+{damageBonus * 100f:F0}% damage)");
			OnGearEquipped?.Invoke(agent, this);
			Destroy(gameObject);
			return false;
		}
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
