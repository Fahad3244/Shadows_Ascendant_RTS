using System;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
	[Header("Dependencies")]
	[SerializeField]
	private PlayerUnitManager unitManager;

	[SerializeField]
	private UnitPool unitPool;

	[Header("Settings")]
	[SerializeField]
	private float spawnRadius = 2f;

	private float _currentAngle;

	private float _angleStep = 20f;

	[Header("Gizmo Visualization")]
	[SerializeField]
	private bool showSpawnCircle = true;

	[SerializeField]
	private Color spawnCircleColor = new Color(0.2f, 1f, 0.5f, 0.3f);

	[SerializeField]
	private Color nextSpawnColor = Color.yellow;

	public UnitAgent TrySpawnUnit(UnitType type)
	{
		if (unitManager.TryGetFreeUnit(type, out var unit))
		{
			Vector3 nextSpawnOffset = GetNextSpawnOffset();
			Vector3 position = base.transform.position + nextSpawnOffset;
			UnitAgent agent = unitPool.GetAgent(type, position, base.transform.rotation);
			agent.Initialize(unit, type, unitManager, unitPool, base.transform.root);
			return agent;
		}
		return null;
	}

	private Vector3 GetNextSpawnOffset()
	{
		float f = _currentAngle * (MathF.PI / 180f);
		Vector3 result = new Vector3(Mathf.Sin(f), 0f, Mathf.Cos(f)) * spawnRadius;
		_currentAngle += _angleStep;
		if (_currentAngle >= 360f)
		{
			_currentAngle -= 360f;
		}
		return result;
	}

	private void OnDrawGizmos()
	{
		if (showSpawnCircle)
		{
			Vector3 position = base.transform.position;
			Gizmos.color = spawnCircleColor;
			int num = 32;
			float num2 = 360f / (float)num;
			Vector3 vector = position + new Vector3(spawnRadius, 0f, 0f);
			for (int i = 1; i <= num + 1; i++)
			{
				float f = (float)i * num2 * (MathF.PI / 180f);
				Vector3 vector2 = position + new Vector3(Mathf.Sin(f) * spawnRadius, 0f, Mathf.Cos(f) * spawnRadius);
				Gizmos.DrawLine(vector, vector2);
				vector = vector2;
			}
			float f2 = _currentAngle * (MathF.PI / 180f);
			Vector3 vector3 = new Vector3(Mathf.Sin(f2), 0f, Mathf.Cos(f2)) * spawnRadius;
			Vector3 vector4 = position + vector3;
			Gizmos.color = nextSpawnColor;
			Gizmos.DrawSphere(vector4, 0.3f);
			Gizmos.DrawLine(position, vector4);
		}
	}
}
