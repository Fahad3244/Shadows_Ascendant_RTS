using System;

[Serializable]
public class Unit
{
	public UnitType type;

	public int health = 100;

	public Unit(UnitType unitType)
	{
		type = unitType;
	}
}
