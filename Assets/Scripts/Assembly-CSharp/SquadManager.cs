using System;
using System.Collections.Generic;
using UnityEngine;

public class SquadManager : MonoBehaviour
{
	public class SquadPayload
	{
		public GuardFlag LinkedFlag;

		public HashSet<UnitAgent> PhysicalUnits = new HashSet<UnitAgent>();

		public Dictionary<UnitType, int> Composition = new Dictionary<UnitType, int>();
	}

	[Header("Dependencies")]
	[SerializeField]
	private PlayerTargetingManager targetingManager;

	[SerializeField]
	private SwarmController swarmController;

	private SquadPayload _customSquad;

	public event Action<int, SquadPayload> OnSquadSelected;

	public event Action<int, SquadPayload> OnSquadSaved;

	private void Awake()
	{
		if (targetingManager == null)
		{
			targetingManager = UnityEngine.Object.FindObjectOfType<PlayerTargetingManager>();
		}
		if (swarmController == null)
		{
			swarmController = UnityEngine.Object.FindObjectOfType<SwarmController>();
		}
	}

	private void OnEnable()
	{
		if (PlayerInputManager.Instance != null)
		{
			PlayerInputManager.Instance.OnGroupSelectionInput += HandleGroupSelection;
		}
	}

	private void OnDisable()
	{
		if (PlayerInputManager.Instance != null)
		{
			PlayerInputManager.Instance.OnGroupSelectionInput -= HandleGroupSelection;
		}
	}

	private void HandleGroupSelection(GroupSelection selection)
	{
		if (selection == GroupSelection.Custom)
		{
			if (targetingManager != null && targetingManager.CurrentTarget is GuardFlag selectedFlag)
			{
				AssignCustomGroup(selectedFlag);
			}
			else
			{
				SelectCustomGroup();
			}
		}
	}

	private void AssignCustomGroup(GuardFlag selectedFlag)
	{
		HashSet<UnitAgent> allUnitsAtFlag = selectedFlag.GetAllUnitsAtFlag();
		if (allUnitsAtFlag.Count == 0)
		{
			Debug.Log("[SquadManager] Cannot create custom group: No units at flag.");
			return;
		}
		if (_customSquad != null && _customSquad.LinkedFlag != null && _customSquad.LinkedFlag != selectedFlag)
		{
			_customSquad.LinkedFlag.SetSquadIndex(-1);
		}
		Dictionary<UnitType, int> dictionary = new Dictionary<UnitType, int>();
		foreach (UnitAgent item in allUnitsAtFlag)
		{
			if (!dictionary.ContainsKey(item.Type))
			{
				dictionary[item.Type] = 0;
			}
			dictionary[item.Type]++;
		}
		_customSquad = new SquadPayload
		{
			LinkedFlag = selectedFlag,
			PhysicalUnits = new HashSet<UnitAgent>(allUnitsAtFlag),
			Composition = dictionary
		};
		selectedFlag.SetSquadIndex(5);
		SyncSwarmController();
		Debug.Log($"[SquadManager] Custom Group Saved with {allUnitsAtFlag.Count} units.");
		this.OnSquadSaved?.Invoke(5, _customSquad);
		this.OnSquadSelected?.Invoke(5, _customSquad);
	}

	private void SelectCustomGroup()
	{
		if (_customSquad == null)
		{
			Debug.Log("[SquadManager] No custom group exists yet.");
			this.OnSquadSelected?.Invoke(-1, null);
			return;
		}
		_customSquad.PhysicalUnits.RemoveWhere((UnitAgent unit) => unit == null || unit.CurrentStateEnum == UnitState.Dead);
		SyncSwarmController();
		Debug.Log($"[SquadManager] Selected Custom Group. Physical: {_customSquad.PhysicalUnits.Count}");
		this.OnSquadSelected?.Invoke(5, _customSquad);
	}

	private void SyncSwarmController()
	{
		if (!(swarmController != null) || _customSquad == null)
		{
			return;
		}
		swarmController.CustomGroupUnits.Clear();
		foreach (UnitAgent physicalUnit in _customSquad.PhysicalUnits)
		{
			swarmController.CustomGroupUnits.Add(physicalUnit);
		}
		HUDManager hUDManager = UnityEngine.Object.FindObjectOfType<HUDManager>();
		if (hUDManager != null)
		{
			hUDManager.UpdateUI();
		}
	}
}
