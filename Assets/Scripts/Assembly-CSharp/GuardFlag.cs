using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GuardFlag : MonoBehaviour, ITargetable
{
	[Header("Settings")]
	[Tooltip("The range at which units will spot enemies.")]
	public float guardRadius = 10f;

	[Header("Squad Visuals")]
	[SerializeField]
	private TextMeshProUGUI squadIndexText;

	[Header("Debug")]
	[SerializeField]
	private Color gizmoColor = Color.blue;

	private int _squadIndex = -1;

	private Dictionary<UnitType, float> _formationRadii = new Dictionary<UnitType, float>
	{
		{
			UnitType.Ranged,
			3f
		},
		{
			UnitType.Melee,
			4f
		},
		{
			UnitType.Medic,
			6f
		},
		{
			UnitType.Builder,
			8f
		}
	};

	private Dictionary<UnitType, List<UnitAgent>> _unitsByType = new Dictionary<UnitType, List<UnitAgent>>();

	public bool IsTargetable
	{
		get
		{
			if (this != null)
			{
				return base.gameObject.activeInHierarchy;
			}
			return false;
		}
	}

	public TargetTeam Team => TargetTeam.Friendly;

	private void Awake()
	{
		foreach (UnitType value in Enum.GetValues(typeof(UnitType)))
		{
			_unitsByType[value] = new List<UnitAgent>();
		}
	}

	public void AddUnit(UnitAgent agent)
	{
		if (!_unitsByType[agent.Type].Contains(agent))
		{
			_unitsByType[agent.Type].Add(agent);
		}
	}

	public void RemoveUnit(UnitAgent agent)
	{
		if (_unitsByType.ContainsKey(agent.Type) && _unitsByType[agent.Type].Contains(agent))
		{
			_unitsByType[agent.Type].Remove(agent);
		}
		if (GetTotalUnitCount() == 0)
		{
			DespawnFlag();
		}
	}

	private void DespawnFlag()
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private int GetTotalUnitCount()
	{
		int num = 0;
		foreach (List<UnitAgent> value in _unitsByType.Values)
		{
			num += value.Count;
		}
		return num;
	}

	public void SetSquadIndex(int index)
	{
		_squadIndex = index;
		if (squadIndexText != null)
		{
			if (index > 0)
			{
				squadIndexText.text = index.ToString();
				squadIndexText.gameObject.SetActive(value: true);
			}
			else
			{
				squadIndexText.gameObject.SetActive(value: false);
			}
		}
	}

	public UnitAgent GetUnitToRecall()
	{
		foreach (List<UnitAgent> value in _unitsByType.Values)
		{
			if (value.Count > 0)
			{
				return value[0];
			}
		}
		return null;
	}

	public Vector3 GetFormationPosition(UnitAgent agent)
	{
		List<UnitAgent> list = _unitsByType[agent.Type];
		int num = list.IndexOf(agent);
		int count = list.Count;
		if (num == -1 || count == 0)
		{
			return base.transform.position;
		}
		float num2 = (_formationRadii.ContainsKey(agent.Type) ? _formationRadii[agent.Type] : 3f);
		float num3 = (((int)agent.Type % 2 == 0) ? 0f : 15f);
		float num4 = 360f / (float)count;
		float f = ((float)num * num4 + num3) * (MathF.PI / 180f);
		Vector3 vector = new Vector3(Mathf.Sin(f), 0f, Mathf.Cos(f)) * num2;
		return base.transform.position + vector;
	}

	public Vector3 GetTargetPosition()
	{
		return base.transform.position;
	}

	public Transform GetTransform()
	{
		return base.transform;
	}

	public void OnTargetSelected()
	{
		GlobalEvents.OnReadyToMove.Invoke(base.gameObject);
	}

	public void OnTargetDeselected()
	{
		GlobalEvents.OnReadyToMove.Invoke(null);
	}

	public HashSet<UnitAgent> GetAllUnitsAtFlag()
	{
		HashSet<UnitAgent> hashSet = new HashSet<UnitAgent>();
		foreach (List<UnitAgent> value in _unitsByType.Values)
		{
			foreach (UnitAgent item in value)
			{
				hashSet.Add(item);
			}
		}
		return hashSet;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = gizmoColor;
		Gizmos.DrawLine(base.transform.position, base.transform.position + Vector3.up * 3f);
		Gizmos.DrawSphere(base.transform.position + Vector3.up * 3f, 0.5f);
		Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.2f);
		Gizmos.DrawSphere(base.transform.position, guardRadius);
		if (_formationRadii == null)
		{
			return;
		}
		foreach (KeyValuePair<UnitType, float> formationRadius in _formationRadii)
		{
			Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
			float value = formationRadius.Value;
			Gizmos.DrawWireSphere(base.transform.position, value);
		}
	}
}
