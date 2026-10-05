using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerUnitManager : MonoBehaviour
{
	[Header("Configuration")]
	[SerializeField]
	private int initialTotalUnits = 20;

	private Dictionary<UnitType, Stack<Unit>> _freeUnits = new Dictionary<UnitType, Stack<Unit>>();

	private List<Unit> _allUnits = new List<Unit>();

	private HashSet<Unit> _activeUnits = new HashSet<Unit>();

	public int TotalCount => _allUnits.Count;

	public int FreeCount => _allUnits.Count - _activeUnits.Count;

	public event Action OnUnitsChanged;

	private void Awake()
	{
		InitializeStorage();
		CreateInitialUnits();
	}

	private void InitializeStorage()
	{
		foreach (UnitType value in Enum.GetValues(typeof(UnitType)))
		{
			_freeUnits[value] = new Stack<Unit>();
		}
	}

	private void CreateInitialUnits()
	{
		int length = Enum.GetValues(typeof(UnitType)).Length;
		int num = initialTotalUnits / length;
		foreach (UnitType value in Enum.GetValues(typeof(UnitType)))
		{
			for (int i = 0; i < num; i++)
			{
				Unit item = new Unit(value);
				_allUnits.Add(item);
				_freeUnits[value].Push(item);
			}
		}
		this.OnUnitsChanged?.Invoke();
	}

	public bool TryGetFreeUnit(UnitType type, out Unit unit)
	{
		if (_freeUnits[type].Count > 0)
		{
			unit = _freeUnits[type].Pop();
			_activeUnits.Add(unit);
			this.OnUnitsChanged?.Invoke();
			return true;
		}
		unit = null;
		return false;
	}

	public bool TryClaimSpecificUnit(Unit unit)
	{
		if (unit == null || _activeUnits.Contains(unit)) return false;
		Stack<Unit> stack = _freeUnits[unit.type];
		if (!stack.Contains(unit)) return false;

		List<Unit> remaining = stack.Where(u => u != unit).Reverse().ToList();
		stack.Clear();
		foreach (Unit u in remaining) stack.Push(u);

		_activeUnits.Add(unit);
		OnUnitsChanged?.Invoke();
		return true;
	}

	public void ReturnUnit(Unit unit)
	{
		if (_activeUnits.Contains(unit))
		{
			_activeUnits.Remove(unit);
			unit.health = 100;
			_freeUnits[unit.type].Push(unit);
			this.OnUnitsChanged?.Invoke();
		}
	}

	public List<Unit> GetFreeUnitsByType(UnitType type)
	{
		if (_freeUnits.TryGetValue(type, out var value))
		{
			return value.ToList();
		}
		return new List<Unit>();
	}

	public List<Unit> GetActiveUnitsByType(UnitType type)
	{
		return _activeUnits.Where((Unit u) => u.type == type).ToList();
	}

	public void ProcessUnitDeath(UnitAgent agent)
	{
		if (_activeUnits.Contains(agent.InternalData))
		{
			_activeUnits.Remove(agent.InternalData);
			_allUnits.Remove(agent.InternalData);
			this.OnUnitsChanged?.Invoke();
		}
	}
}
