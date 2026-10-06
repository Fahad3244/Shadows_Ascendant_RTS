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

	[Header("Interaction")]
	[SerializeField]
	private InteractionCategory category = InteractionCategory.Breakable;

	[Tooltip("Speed with the minimum troops attached.")]
	[SerializeField]
	private float minMoveSpeed = 2f;

	[Tooltip("Speed with every slot filled.")]
	[SerializeField]
	private float maxMoveSpeed = 5f;

	[Tooltip("Damage multiplier for troops attacking from the front once all slots are full.")]
	[SerializeField]
	private float frontAttackMultiplier = 0.5f;

	[Header("Front Attack (Hostiles only)")]
	[SerializeField]
	private int maxFrontAttackers = 4;

	[SerializeField]
	private float frontRadius = 2.5f;

	[Tooltip("Total spread angle of the front arc, in degrees.")]
	[SerializeField]
	private float frontArcAngle = 90f;

	[Header("UI")]
	[SerializeField]
	private AttachPointUI uiPrefab;

	[SerializeField]
	private Vector3 uiOffset = new Vector3(0f, 2f, 0f);

	private List<UnitAgent> _attachedUnits = new List<UnitAgent>();

	private List<UnitAgent> _frontUnits = new List<UnitAgent>();

	private Dictionary<UnitAgent, int> _frontSlotIndex = new Dictionary<UnitAgent, int>();

	private Vector3 _frontAnchorDir;

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

	// Enemies are always Hostile, whatever the Inspector says
	public InteractionCategory Category => _baseTarget != null && _baseTarget.Team == TargetTeam.Enemy ? InteractionCategory.Hostile : category;

	public float FrontAttackMultiplier => frontAttackMultiplier;

	public int RequiredUnits => requiredUnitsToMove;

	public bool MeetsRequirement => AttachedUnitCount >= requiredUnitsToMove;

	public float FrontRadius => frontRadius;

	public bool CanJoinFront => Category == InteractionCategory.Hostile && _frontUnits.Count < maxFrontAttackers;

	public bool CanBeEngaged => HasFreeSlots || CanJoinFront;

	public bool IsFrontAttacker(UnitAgent agent) => _frontUnits.Contains(agent);

	public bool TryPromoteFront(UnitAgent agent)
	{
		if (!_frontUnits.Contains(agent) || !HasFreeSlots)
		{
			return false;
		}
		_frontUnits.Remove(agent);
		_frontSlotIndex.Remove(agent);
		_attachedUnits.Add(agent);
		UpdateUI();
		return true;
	}

	public float GetMoveSpeed()
	{
		int min = Mathf.Max(1, requiredUnitsToMove);
		int n = Mathf.Clamp(AttachedUnitCount, min, maxAttachSlots);
		float t = maxAttachSlots > min ? (float)(n - min) / (maxAttachSlots - min) : 1f;
		return Mathf.Lerp(minMoveSpeed, maxMoveSpeed, t);
	}

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
		if (CanJoinFront)
		{
			if (_frontUnits.Count == 0)
			{
				// Lock the arc direction toward where the first front troop arrived from
				Vector3 d = agent.transform.position - transform.position;
				d.y = 0f;
				_frontAnchorDir = d.sqrMagnitude > 0.01f ? d.normalized : transform.forward;
			}
			int idx = 0;
			while (_frontSlotIndex.ContainsValue(idx))
			{
				idx++;
			}
			_frontSlotIndex[agent] = idx;
			_frontUnits.Add(agent);
			return true;
		}
		return false;
	}

	public void ConfirmAttach(UnitAgent agent)
	{
		if (_frontUnits.Contains(agent))
		{
			agent.OnAttachedToTarget(this);
			return;
		}
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
		else if (_frontUnits.Remove(agent))
		{
			_frontSlotIndex.Remove(agent);
			flag = true;
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
		if (num == -1 && _frontSlotIndex.TryGetValue(agent, out int frontSlot))
		{
			float t = maxFrontAttackers > 1 ? (float)frontSlot / (maxFrontAttackers - 1) : 0.5f;
			float angle = Mathf.Lerp(-frontArcAngle * 0.5f, frontArcAngle * 0.5f, t);
			Vector3 dir = Quaternion.Euler(0f, angle, 0f) * _frontAnchorDir;
			return transform.position + dir * frontRadius + Vector3.up * attachPointHeight;
		}
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
		foreach (UnitAgent agent3 in _frontUnits.ToArray())
		{
			CancelReservation(agent3);
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
