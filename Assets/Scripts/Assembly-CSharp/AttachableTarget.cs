using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(TargetableEntity))]
public class AttachableTarget : MonoBehaviour
{
	[Header("Configuration")]
	[SerializeField]
	private int maxAttachSlots = 5;

	[SerializeField]
	private float attachRadius = 1.5f;

	[SerializeField]
	private float attachPointHeight;

	[Header("Movement Rules")]
	[Tooltip("How many units must be attached before this object can be swept/moved? Set to 0 if immovable.")]
	[SerializeField]
	private int requiredUnitsToMove;

	[Header("UI")]
	[SerializeField]
	private AttachPointUI uiPrefab;

	[SerializeField]
	private Vector3 uiOffset = new Vector3(0f, 2f, 0f);

	private List<UnitAgent> _attachedUnits = new List<UnitAgent>();

	private HashSet<UnitAgent> _incomingUnits = new HashSet<UnitAgent>();

	private AttachPointUI _uiInstance;

	private TargetableEntity _baseTarget;

	private PlayerTargetingManager _ptm;

	private bool _hasTakenDamage;

	public int AvailableSlots => maxAttachSlots - (_attachedUnits.Count + _incomingUnits.Count);

	public float AttachRadius => attachRadius;

	public bool HasFreeSlots => AvailableSlots > 0;

	public int AttachedUnitCount => _attachedUnits.Count;

	public bool CanBeMoved
	{
		get
		{
			if (requiredUnitsToMove > 0)
			{
				return AttachedUnitCount >= requiredUnitsToMove;
			}
			return false;
		}
	}

	public TargetTeam Team => _baseTarget.Team;

	private void Awake()
	{
		_baseTarget = GetComponent<TargetableEntity>();
		_ptm = UnityEngine.Object.FindObjectOfType<PlayerTargetingManager>();
		if (uiPrefab != null)
		{
			_uiInstance = UnityEngine.Object.Instantiate(uiPrefab, base.transform.position + uiOffset, Quaternion.identity, base.transform);
			UpdateUI();
		}
	}

	private void Update()
	{
		HandleVisibilityRules();
	}

	private void HandleVisibilityRules()
	{
		if (!(_uiInstance == null))
		{
			bool flag = false;
			flag = _baseTarget.Team == TargetTeam.Object || (_ptm != null && _ptm.CurrentTarget == _baseTarget) || _hasTakenDamage;
			_uiInstance.SetVisibility(flag);
		}
	}

	public bool TryReserveSlot(UnitAgent agent)
	{
		if (AvailableSlots > 0)
		{
			_incomingUnits.Add(agent);
			UpdateUI();
			return true;
		}
		return false;
	}

	public void ConfirmAttach(UnitAgent agent)
	{
		if (_incomingUnits.Contains(agent))
		{
			_incomingUnits.Remove(agent);
			_attachedUnits.Add(agent);
			agent.OnAttachedToTarget(this);
			UpdateUI();
			CheckMoveState();
		}
	}

	public void CancelReservation(UnitAgent agent)
	{
		bool flag = false;
		if (_incomingUnits.Contains(agent))
		{
			_incomingUnits.Remove(agent);
			flag = true;
		}
		else if (_attachedUnits.Contains(agent))
		{
			_attachedUnits.Remove(agent);
			flag = true;
			CheckMoveState();
		}
		if (flag)
		{
			UpdateUI();
		}
	}

	private void CheckMoveState()
	{
		if (_baseTarget.Team == TargetTeam.Object && _ptm != null && _ptm.CurrentTarget == _baseTarget)
		{
			if (CanBeMoved)
			{
				GlobalEvents.OnReadyToMove.Invoke(base.gameObject);
			}
			else
			{
				GlobalEvents.OnReadyToMove.Invoke(null);
			}
		}
	}

	public Vector3 GetAttachmentPosition(UnitAgent agent)
	{
		int num = _attachedUnits.IndexOf(agent);
		if (num == -1)
		{
			return base.transform.position;
		}
		float num2 = 360f / (float)maxAttachSlots;
		float f = (float)num * num2 * (MathF.PI / 180f);
		Vector3 vector = new Vector3(Mathf.Sin(f), attachPointHeight, Mathf.Cos(f)) * attachRadius;
		return base.transform.position + vector;
	}

	public UnitAgent GetPriorityAttachedUnit()
	{
		if (_incomingUnits.Count > 0)
		{
			using HashSet<UnitAgent>.Enumerator enumerator = _incomingUnits.GetEnumerator();
			if (enumerator.MoveNext())
			{
				return enumerator.Current;
			}
		}
		return null;
	}

	private void UpdateUI()
	{
		if ((bool)_uiInstance)
		{
			_uiInstance.UpdateCount(AvailableSlots);
		}
	}

	public void RegisterDamage()
	{
		_hasTakenDamage = true;
	}

	public void EjectAllUnits()
	{
		UnitAgent[] array = _attachedUnits.ToArray();
		foreach (UnitAgent agent in array)
		{
			CancelReservation(agent);
		}
		array = _incomingUnits.ToArray();
		foreach (UnitAgent agent2 in array)
		{
			CancelReservation(agent2);
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Vector3 vector = base.transform.position + uiOffset;
		Gizmos.DrawLine(base.transform.position, vector);
		Gizmos.DrawWireCube(vector, new Vector3(0.5f, 0.2f, 0.1f));
		Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
		Gizmos.DrawWireSphere(base.transform.position, attachRadius);
		if (maxAttachSlots > 0)
		{
			float num = 360f / (float)maxAttachSlots;
			for (int i = 0; i < maxAttachSlots; i++)
			{
				float f = (float)i * num * (MathF.PI / 180f);
				Vector3 vector2 = new Vector3(Mathf.Sin(f), attachPointHeight, Mathf.Cos(f)) * attachRadius;
				Vector3 vector3 = base.transform.position + vector2;
				Gizmos.color = ((Application.isPlaying && i < _attachedUnits.Count) ? Color.red : Color.cyan);
				Gizmos.DrawSphere(vector3, 0.25f);
				Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.5f);
				Gizmos.DrawLine(base.transform.position, vector3);
			}
		}
	}
}
