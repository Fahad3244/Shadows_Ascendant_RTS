using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerTargetingManager : MonoBehaviour
{
	public enum TargetingMode
	{
		Mouse = 0,
		Cycle = 1
	}

	[Header("Dependencies")]
	[SerializeField]
	private LayerMask targetableLayers;

	[SerializeField]
	private float targetingRange = 50f;

	[Header("Configuration")]
	[SerializeField]
	private TargetingMode currentMode;

	[Tooltip("How far the targeting system will search for a NEW target around the CURRENT target when cycling.")]
	[SerializeField]
	private float cycleRadius = 15f;

	[Header("Outline Settings - Colors")]
	[SerializeField]
	private Color enemyColor = Color.red;

	[SerializeField]
	private Color objectColor = Color.yellow;

	[SerializeField]
	private Color friendlyColor = Color.cyan;

	[Header("Outline Settings - Widths")]
	[SerializeField]
	[Range(0f, 10f)]
	private float hoverWidth = 4f;

	[SerializeField]
	[Range(0f, 10f)]
	private float selectedWidth = 6f;

	private ITargetable _currentHover;

	private bool _canTarget;

	private Camera _mainCamera;

	public ITargetable CurrentTarget { get; private set; }

	private void Awake()
	{
		_mainCamera = Camera.main;
	}

	private void Start()
	{
		HandleTargetLock(locked: false);
	}

	private void OnEnable()
	{
		PlayerInputManager.Instance.OnTargetTogglePressed += HandleToggleInput;
		PlayerInputManager.Instance.OnTargetCyclePressed += HandleCycleInput;
		GlobalEvents.OnBuildModeChanged.Subscribe(HandleTargetLock);
		GlobalEvents.OnBuildingPlaced.Subscribe(OnBuildingPlaced);
	}

	private void OnDisable()
	{
		PlayerInputManager.Instance.OnTargetTogglePressed -= HandleToggleInput;
		PlayerInputManager.Instance.OnTargetCyclePressed -= HandleCycleInput;
		GlobalEvents.OnBuildModeChanged.Unsubscribe(HandleTargetLock);
		GlobalEvents.OnBuildingPlaced.Unsubscribe(OnBuildingPlaced);
	}

	private void HandleTargetLock(bool locked)
	{
		_canTarget = !locked;
		if (locked)
		{
			ClearHover();
		}
	}

	private void Update()
	{
		if (_canTarget)
		{
			if (CurrentTarget as Object == null)
			{
				CurrentTarget = null;
			}
			else if (!CurrentTarget.IsTargetable)
			{
				ClearTarget();
			}
			else if (currentMode == TargetingMode.Mouse)
			{
				HandleHoverLogic();
			}
			else if (_currentHover != null)
			{
				ClearHover();
			}
		}
	}

	private void HandleToggleInput()
	{
		if (!_canTarget)
		{
			return;
		}
		if (currentMode == TargetingMode.Mouse)
		{
			if (_currentHover != null)
			{
				if (CurrentTarget == _currentHover)
				{
					ClearTarget();
				}
				else
				{
					SetTarget(_currentHover);
				}
			}
			else
			{
				ClearTarget();
			}
		}
		else if (CurrentTarget != null)
		{
			ClearTarget();
		}
		else
		{
			CycleToNextTarget();
		}
	}

	private void HandleCycleInput()
	{
		if (_canTarget)
		{
			CycleToNextTarget();
		}
	}

	private float GetTargetScore(ITargetable target)
	{
		Vector3 toTarget = target.GetTargetPosition() - base.transform.position;
		float distance = toTarget.magnitude;
		Vector3 flatForward = _mainCamera.transform.forward;
		flatForward.y = 0f;
		Vector3 flatToTarget = toTarget;
		flatToTarget.y = 0f;
		float dot = Vector3.Dot(flatForward.normalized, flatToTarget.normalized);
		bool isBehind = dot < 0f;
		return distance + (isBehind ? 1000f : 0f);
	}

	private void CycleToNextTarget()
	{
		Vector3 searchOrigin = ((CurrentTarget != null) ? CurrentTarget.GetTransform().position : base.transform.position);
		float radius = ((CurrentTarget != null) ? cycleRadius : targetingRange);
		Collider[] array = Physics.OverlapSphere(searchOrigin, radius, targetableLayers);
		List<ITargetable> list = new List<ITargetable>();
		Collider[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			ITargetable componentInParent = array2[i].GetComponentInParent<ITargetable>();
			if (componentInParent != null && componentInParent.IsTargetable && componentInParent as Object != null)
			{
				list.Add(componentInParent);
			}
		}
		list = list.Distinct().ToList();
		if (list.Count == 0)
		{
			if (CurrentTarget == null)
			{
				ClearTarget();
			}
		}
		else
		{
			if (list.Count == 1 && list[0] == CurrentTarget)
			{
				return;
			}
			list.Sort((ITargetable a, ITargetable b) =>
			{
				int scoreCompare = GetTargetScore(a).CompareTo(GetTargetScore(b));
				if (scoreCompare != 0)
				{
					return scoreCompare;
				}
				return ((Object)a).GetInstanceID().CompareTo(((Object)b).GetInstanceID());
			});
			ITargetable target = list[0];
			if (CurrentTarget != null)
			{
				for (int num = 0; num < list.Count; num++)
				{
					if (list[num] == CurrentTarget)
					{
						int index = (num + 1) % list.Count;
						target = list[index];
						break;
					}
				}
			}
			SetTarget(target);
		}
	}

	private void HandleHoverLogic()
	{
		if (Physics.Raycast(_mainCamera.ScreenPointToRay(Input.mousePosition), out var hitInfo, targetingRange, targetableLayers))
		{
			ITargetable componentInParent = hitInfo.collider.GetComponentInParent<ITargetable>();
			if (componentInParent != null && componentInParent.IsTargetable)
			{
				if (componentInParent == CurrentTarget)
				{
					ClearHover();
				}
				else if (_currentHover != componentInParent)
				{
					ClearHover();
					_currentHover = componentInParent;
					AddOutline(_currentHover, GetColorForTarget(_currentHover), hoverWidth);
				}
				return;
			}
		}
		ClearHover();
	}

	private void OnBuildingPlaced(GameObject placedObject)
	{
		if (CurrentTarget != null && CurrentTarget.GetTransform().gameObject == placedObject)
		{
			ClearTarget();
		}
	}

	public void SetTarget(ITargetable newTarget)
	{
		if (CurrentTarget != newTarget)
		{
			if (CurrentTarget != null)
			{
				RemoveOutline(CurrentTarget);
				CurrentTarget.OnTargetDeselected();
			}
			CurrentTarget = newTarget;
			AddOutline(CurrentTarget, GetColorForTarget(CurrentTarget), selectedWidth);
			CurrentTarget.OnTargetSelected();
			_currentHover = null;
			Debug.Log("[Targeting] Locked on: " + CurrentTarget.GetTransform().name);
		}
	}

	private void ClearTarget()
	{
		if (CurrentTarget != null && CurrentTarget as Object != null)
		{
			RemoveOutline(CurrentTarget);
			CurrentTarget.OnTargetDeselected();
		}
		CurrentTarget = null;
	}

	private void ClearHover()
	{
		if (_currentHover != null)
		{
			RemoveOutline(_currentHover);
			_currentHover = null;
		}
	}

	private Color GetColorForTarget(ITargetable target)
	{
		return target.Team switch
		{
			TargetTeam.Enemy => enemyColor, 
			TargetTeam.Friendly => friendlyColor, 
			_ => objectColor, 
		};
	}

	private void AddOutline(ITargetable target, Color color, float width)
	{
		if (target != null)
		{
			GameObject gameObject = target.GetTransform().gameObject;
			Outline outline = gameObject.GetComponent<Outline>();
			if (outline == null)
			{
				outline = gameObject.AddComponent<Outline>();
			}
			outline.enabled = true;
			outline.OutlineColor = color;
			outline.OutlineWidth = width;
			outline.OutlineMode = Outline.Mode.OutlineAll;
		}
	}

	private void RemoveOutline(ITargetable target)
	{
		if (target != null && !(target as Object == null))
		{
			Outline component = target.GetTransform().GetComponent<Outline>();
			if (component != null)
			{
				component.enabled = false;
			}
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawWireSphere(base.transform.position, targetingRange);
	}
}
