using System;
using UnityEngine;

[Serializable]
public class Unit
{
	public UnitType type;

	public int health = 100;

	public bool hasGear;

	public string gearName;

	public float damageBonus;

	public GameObject gearVisualPrefab;

	public Unit(UnitType unitType)
	{
		type = unitType;
	}
}
