using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Unit
{
	public UnitType type;

	public int health = 100;

	public const int MaxGear = 4;

	public int gearCount;

	public string gearName;

	public float damageBonus;

	public List<GameObject> gearVisualPrefabs = new List<GameObject>();

	public Unit(UnitType unitType)
	{
		type = unitType;
	}
}
