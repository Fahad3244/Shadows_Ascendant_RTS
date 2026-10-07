using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SwarmController : MonoBehaviour
{
	private struct SweepNode
	{
		public Vector3 Position;

		public float TimeAdded;
	}

	[Header("Dependencies")]
	[SerializeField]
	private UnitSpawner spawner;

	[SerializeField]
	private UnitPool unitPool;

	[SerializeField]
	private PlayerUnitManager unitManager;

	[SerializeField]
	private Transform playerModel;

	[SerializeField]
	private PlayerTargetingManager targetingManager;

	[SerializeField]
	private DelayCommandManager delayManager;

	private VirtualCursor _cursor;

	[Header("Swarm Settings")]
	[SerializeField]
	private float sendRange = 25f;

	[SerializeField]
	private float pathResolution = 1.5f;

	[SerializeField]
	private float targetReachedRadius = 4f;

	[SerializeField]
	private float sweepStoppingDistance = 1.5f;

	[SerializeField]
	private float pathDecayTime = 1.5f;

	[SerializeField]
	private float objectSweepSpeed = 5f;

	[SerializeField]
	private float maxSweepLeashDistance = 12f;

	[SerializeField]
	private float stragglerCatchUpDistance = 6f;

	[SerializeField]
	private float stragglerSpeedMultiplier = 1.6f;

	[SerializeField]
	private float delayCommandStartOffset = 2f;

	[SerializeField]
	private float spreadSpacing = 1.4f;

	[SerializeField]
	private LayerMask groundLayer;

	[Header("Debug Visualization")]
	[SerializeField]
	private bool showSweepPath = true;

	[SerializeField]
	private Color pathColor = Color.cyan;

	[SerializeField]
	private Color currentTargetColor = Color.blue;

	private bool _isSystemBusy;

	private Queue<SweepNode> _sweepPath = new Queue<SweepNode>();

	private Vector3 _currentSweepTarget;

	private Vector3 _lastRecordedCursorPos;

	private bool _wasSweepingPreviously;

	private GameObject _objectBeingMoved;

	private NavMeshAgent _objectNavAgent;

	private MovableObject _objectMovable;

	private bool _isRecordingDelayCommand;

	private Camera _mainCamera;

	public GroupSelection CurrentSelectedGroup { get; private set; }

	public HashSet<UnitAgent> CustomGroupUnits { get; private set; } = new HashSet<UnitAgent>();

	public event Action OnSelectionChanged;

	private void Start()
	{
		_mainCamera = Camera.main;
		if (spawner == null)
		{
			spawner = GetComponentInChildren<UnitSpawner>();
		}
		if (unitPool == null)
		{
			unitPool = UnityEngine.Object.FindObjectOfType<UnitPool>();
		}
		if (unitManager == null)
		{
			unitManager = UnityEngine.Object.FindObjectOfType<PlayerUnitManager>();
		}
		if (playerModel == null)
		{
			playerModel = base.transform;
		}
		if (targetingManager == null)
		{
			targetingManager = UnityEngine.Object.FindObjectOfType<PlayerTargetingManager>();
		}
		if (delayManager == null)
		{
			delayManager = UnityEngine.Object.FindObjectOfType<DelayCommandManager>();
		}
		_cursor = VirtualCursor.Instance;
		if (_cursor != null)
		{
			_cursor.ToggleVisuals(show: false);
		}
	}

	private void OnEnable()
	{
		if (PlayerInputManager.Instance != null)
		{
			PlayerInputManager.Instance.OnLeftTap += HandleSendTap;
			PlayerInputManager.Instance.OnLeftHold += HandleSendHold;
			PlayerInputManager.Instance.OnRightTap += HandleRecallTap;
			PlayerInputManager.Instance.OnRightMediumHold += HandleRecallMediumHold;
			PlayerInputManager.Instance.OnRightLongHold += HandleRecallLongHold;
			PlayerInputManager.Instance.OnGroupSelectionInput += HandleGroupSelectionInput;
			PlayerInputManager.Instance.OnDelayRecordStarted += HandleDelayRecordStarted;
			PlayerInputManager.Instance.OnDelayRecordCommitted += HandleDelayRecordCommitted;
		}
		GlobalEvents.OnBuildingPlaced.Subscribe(HandleBuildingPlaced);
		GlobalEvents.OnBuildModeChanged.Subscribe(HandleSpawnLock);
		GlobalEvents.OnReadyToMove.Subscribe(HandleObjectReadyToMove);
		if (delayManager != null)
		{
			delayManager.OnDelayCommandExecuted += HandleDelayCommandExecuted;
		}
	}

	private void OnDisable()
	{
		if (PlayerInputManager.Instance != null)
		{
			PlayerInputManager.Instance.OnLeftTap -= HandleSendTap;
			PlayerInputManager.Instance.OnLeftHold -= HandleSendHold;
			PlayerInputManager.Instance.OnRightTap -= HandleRecallTap;
			PlayerInputManager.Instance.OnRightMediumHold -= HandleRecallMediumHold;
			PlayerInputManager.Instance.OnRightLongHold -= HandleRecallLongHold;
			PlayerInputManager.Instance.OnGroupSelectionInput -= HandleGroupSelectionInput;
			PlayerInputManager.Instance.OnDelayRecordStarted -= HandleDelayRecordStarted;
			PlayerInputManager.Instance.OnDelayRecordCommitted -= HandleDelayRecordCommitted;
		}
		GlobalEvents.OnBuildingPlaced.Unsubscribe(HandleBuildingPlaced);
		GlobalEvents.OnBuildModeChanged.Unsubscribe(HandleSpawnLock);
		GlobalEvents.OnReadyToMove.Unsubscribe(HandleObjectReadyToMove);
		if (delayManager != null)
		{
			delayManager.OnDelayCommandExecuted -= HandleDelayCommandExecuted;
		}
	}

	private void Update()
	{
		if (_isRecordingDelayCommand)
		{
			if (_cursor != null && delayManager != null && _cursor.GetWorldPosition(groundLayer, out var worldPos))
			{
				delayManager.UpdatePreview(worldPos);
			}
		}
		else
		{
			HandleSweepLifecycle();
		}
	}

	public void HandleGroupSelectionInput(GroupSelection selection)
	{
		CurrentSelectedGroup = selection;
		this.OnSelectionChanged?.Invoke();
	}

	public List<UnitAgent> GetUnitsForCurrentSelection()
	{
		return GetActiveUnitsByGroup(CurrentSelectedGroup);
	}

	public List<UnitAgent> GetActiveUnitsByGroup(GroupSelection group)
	{
		List<UnitAgent> list = new List<UnitAgent>();
		foreach (UnitAgent item in (IEnumerable<UnitAgent>)((group == GroupSelection.Custom) ? CustomGroupUnits : unitPool.ActiveAgents))
		{
			if (item == null || !item.gameObject.activeInHierarchy || item.CurrentStateEnum == UnitState.Dead || item.CurrentStateEnum == UnitState.AtFlag)
			{
				continue;
			}
			if (group == GroupSelection.All || group == GroupSelection.Custom)
			{
				list.Add(item);
				continue;
			}
			UnitType unitType = (UnitType)(group - 1);
			if (item.Type == unitType)
			{
				list.Add(item);
			}
		}
		return list;
	}

	public List<UnitAgent> SpawnInactiveUnitsByGroup(GroupSelection group, int maxToSpawn = 999)
	{
		List<UnitAgent> list = new List<UnitAgent>();
		if (group == GroupSelection.Custom)
		{
			foreach (UnitAgent customGroupUnit in CustomGroupUnits)
			{
				if (list.Count >= maxToSpawn)
				{
					break;
				}

				if (customGroupUnit == null
					|| customGroupUnit.CurrentStateEnum == UnitState.Dead
					|| customGroupUnit.gameObject.activeInHierarchy)
				{
					continue;
				}

				if (!unitManager.TryClaimSpecificUnit(customGroupUnit.InternalData))
				{
					continue;
				}

				if (!unitPool.ReactivateAgent(customGroupUnit, spawner.transform.position, spawner.transform.rotation))
				{
					unitManager.ReturnUnit(customGroupUnit.InternalData);
					continue;
				}

				if (customGroupUnit.NavAgent)
				{
					customGroupUnit.NavAgent.Warp(spawner.transform.position);
				}
				customGroupUnit.ChangeState(UnitState.Idle);
				customGroupUnit.ResetHealth();
				list.Add(customGroupUnit);
			}
		}
		else
		{
			UnitType[] array = ((group == GroupSelection.All) ? ((UnitType[])Enum.GetValues(typeof(UnitType))) : new UnitType[1] { (UnitType)(group - 1) });
			foreach (UnitType type in array)
			{
				while (list.Count < maxToSpawn)
				{
					UnitAgent unitAgent = spawner.TrySpawnUnit(type);
					if (!(unitAgent != null))
					{
						break;
					}
					list.Add(unitAgent);
				}
			}
		}
		return list;
	}

	public bool HasAvailableUnitsForGroup(GroupSelection group)
	{
		if (GetActiveUnitsByGroup(group).Count > 0)
		{
			return true;
		}
		if (group == GroupSelection.Custom)
		{
			foreach (UnitAgent unit in CustomGroupUnits)
			{
				if (unit != null && unit.CurrentStateEnum != UnitState.Dead && !unit.gameObject.activeInHierarchy)
				{
					return true;
				}
			}
			return false;
		}
		UnitType[] types = (group == GroupSelection.All) ? (UnitType[])Enum.GetValues(typeof(UnitType)) : new UnitType[] { (UnitType)(group - 1) };
		foreach (UnitType type in types)
		{
			if (unitManager.GetFreeUnitsByType(type).Count > 0)
			{
				return true;
			}
		}
		return false;
	}

	private void HandleDelayRecordStarted()
	{
		if (!(_cursor == null) && !(delayManager == null))
		{
			_isRecordingDelayCommand = true;
			_cursor.ToggleVisuals(true);
			Vector3 cameraForward = (_mainCamera != null) ? _mainCamera.transform.forward : playerModel.forward;
			cameraForward.y = 0f;
			if (cameraForward.sqrMagnitude < 0.01f)
			{
				cameraForward = playerModel.forward;
			}
			cameraForward.Normalize();
			Vector3 rawStartPos = playerModel.position + cameraForward * delayCommandStartOffset;
			Vector3 startPos = rawStartPos;
			if (Physics.Raycast(rawStartPos + Vector3.up * 5f, Vector3.down, out var hitInfo, 20f, groundLayer))
			{
				startPos = hitInfo.point;
			}
			_cursor.WarpToWorldPosition(startPos);
			delayManager.StartPreview(CurrentSelectedGroup, startPos);
		}
	}

	private void HandleDelayRecordCommitted()
	{
		if (!(_cursor == null) && !(delayManager == null))
		{
			_isRecordingDelayCommand = false;
			_cursor.ToggleVisuals(false);
			if (_cursor.GetWorldPosition(groundLayer, out var worldPos))
			{
				ITargetable entityTarget = FindTargetAtCursor(worldPos);
				delayManager.CommitCommand(entityTarget);
			}
		}
	}

	private void HandleDelayCommandExecuted(GroupSelection storedGroup, List<Vector3> path, ITargetable finalTarget)
	{
		List<UnitAgent> activeUnitsByGroup = GetActiveUnitsByGroup(storedGroup);
		activeUnitsByGroup.AddRange(SpawnInactiveUnitsByGroup(storedGroup));
		foreach (UnitAgent item in activeUnitsByGroup)
		{
			if (item.CurrentStateEnum != UnitState.Dead && item.CurrentStateEnum != UnitState.AtFlag)
			{
				item.ExecuteSweep(new List<Vector3>(path), finalTarget);
			}
		}
		if (delayManager != null)
		{
			delayManager.ClearCommand();
		}
	}

	private void HandleSendTap()
	{
		if (_isSystemBusy || PlayerInputManager.Instance.IsDelayKeyHeld)
		{
			return;
		}
		List<UnitAgent> activeUnitsByGroup = GetActiveUnitsByGroup(CurrentSelectedGroup);
		UnitAgent unitAgent = activeUnitsByGroup.Find((UnitAgent a) => a.CurrentStateEnum == UnitState.Idle || a.CurrentStateEnum == UnitState.Returning);
		if (unitAgent == null)
		{
			List<UnitAgent> list = SpawnInactiveUnitsByGroup(CurrentSelectedGroup, 1);
			if (list.Count > 0)
			{
				unitAgent = list[0];
			}
		}
		if (unitAgent == null && activeUnitsByGroup.Count > 0)
		{
			unitAgent = activeUnitsByGroup[0];
		}
		if (unitAgent != null)
		{
			IssueCommandToAgent(unitAgent);
		}
	}

	private void HandleSendHold()
	{
		if (!_isSystemBusy && !PlayerInputManager.Instance.IsDelayKeyHeld)
		{
			List<UnitAgent> activeUnitsByGroup = GetActiveUnitsByGroup(CurrentSelectedGroup);
			activeUnitsByGroup.AddRange(SpawnInactiveUnitsByGroup(CurrentSelectedGroup));
			for (int i = 0; i < activeUnitsByGroup.Count; i++)
			{
				IssueCommandToAgent(activeUnitsByGroup[i], i, activeUnitsByGroup.Count);
			}
		}
	}

	private void HandleRecallTap()
	{
		if (targetingManager.CurrentTarget is GuardFlag guardFlag)
		{
			UnitAgent unitToRecall = guardFlag.GetUnitToRecall();
			if (unitToRecall != null)
			{
				unitToRecall.ReturnToPlayer();
			}
			return;
		}
		foreach (UnitAgent item in GetUnitsForCurrentSelection())
		{
			if (!item.IsReturning && item.CurrentStateEnum != UnitState.AtFlag)
			{
				item.ReturnToPlayer();
				break;
			}
		}
	}

	private void HandleRecallMediumHold()
	{
		if (_isSystemBusy)
		{
			return;
		}
		if (targetingManager.CurrentTarget is GuardFlag guardFlag)
		{
			{
				foreach (UnitAgent item in guardFlag.GetAllUnitsAtFlag())
				{
					item.ReturnToPlayer();
				}
				return;
			}
		}
		foreach (UnitAgent item2 in GetUnitsForCurrentSelection())
		{
			if (!item2.IsReturning && item2.CurrentStateEnum != UnitState.AtFlag)
			{
				item2.ReturnToPlayer();
			}
		}
	}

	private void HandleRecallLongHold()
	{
		if (_isSystemBusy)
		{
			return;
		}
		foreach (UnitAgent activeAgent in unitPool.ActiveAgents)
		{
			if (activeAgent != null && activeAgent.gameObject.activeInHierarchy && !activeAgent.IsReturning && activeAgent.CurrentStateEnum != UnitState.AtFlag)
			{
				activeAgent.ReturnToPlayer();
			}
		}
		HandleGroupSelectionInput(GroupSelection.All);
	}

	private void IssueCommandToAgent(UnitAgent agent, int index = 0, int totalInGroup = 1)
	{
		if (targetingManager != null && targetingManager.CurrentTarget != null)
		{
			SendUnitToTarget(agent, targetingManager.CurrentTarget);
		}
		else
		{
			SendUnitToGround(agent, index, totalInGroup);
		}
	}

	private void SendUnitToTarget(UnitAgent agent, ITargetable target)
	{
		agent.LeashDistance = 100f;
		if (target is GuardFlag flag)
		{
			agent.EnterGuardMode(flag);
			return;
		}
		AttachableTarget component = target.GetTransform().GetComponent<AttachableTarget>();
		if (component != null && component.TryReserveSlot(agent))
		{
			agent.SetReservedTarget(component);
			agent.MoveToTarget(component);
		}
		else
		{
			SendUnitToGround(agent);
		}
	}

	private void SendUnitToGround(UnitAgent agent, int index = 0, int totalInGroup = 1)
	{
		Vector3 forwardDestination = GetForwardDestination();
		if (totalInGroup > 1)
		{
			float r = spreadSpacing * Mathf.Sqrt(index + 0.5f);
			float a = index * 137.5f * Mathf.Deg2Rad;
			forwardDestination += new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
		}
		agent.LeashDistance = sendRange;
		agent.MoveTo(forwardDestination);
	}

	private Vector3 GetForwardDestination()
	{
		Vector3 forward = playerModel.forward;
		forward.y = 0f;
		forward.Normalize();
		return playerModel.position + forward * sendRange;
	}

	private void HandleObjectReadyToMove(GameObject objToMove)
	{
		_objectBeingMoved = objToMove;
		_objectMovable = objToMove != null ? objToMove.GetComponent<MovableObject>() : null;
		if (_objectBeingMoved != null)
		{
			_objectNavAgent = _objectBeingMoved.GetComponent<NavMeshAgent>();
			if (_objectNavAgent == null)
			{
				_objectNavAgent = _objectBeingMoved.AddComponent<NavMeshAgent>();
			}
			if (_objectMovable == null)
			{
				_objectNavAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
				_objectNavAgent.speed = objectSweepSpeed;
				_objectNavAgent.acceleration = 4f;
				_objectNavAgent.angularSpeed = 100f;
				_objectNavAgent.autoBraking = true;
			}
		}
		else
		{
			_objectNavAgent = null;
		}
	}

	private ITargetable FindTargetAtCursor(Vector3 endPosition)
	{
		Collider[] array = Physics.OverlapSphere(endPosition, 2f);
		for (int i = 0; i < array.Length; i++)
		{
			ITargetable componentInParent = array[i].GetComponentInParent<ITargetable>();
			if (componentInParent != null && componentInParent as UnityEngine.Object != null)
			{
				return componentInParent;
			}
		}
		return null;
	}

	private void HandleSweepLifecycle()
	{
		if (PlayerInputManager.Instance != null && PlayerInputManager.Instance.IsSweeping && !_isSystemBusy)
		{
			if (!_wasSweepingPreviously)
			{
				BeginSweep();
			}
			ProcessSweep();
		}
		else if (_wasSweepingPreviously)
		{
			EndSweep();
		}
	}

	private Vector3 ClampToLeash(Vector3 rawPosition)
	{
		Vector3 origin = playerModel.position;
		Vector3 offset = rawPosition - origin;
		float y = offset.y;
		offset.y = 0f;
		if (offset.magnitude > maxSweepLeashDistance)
		{
			offset = offset.normalized * maxSweepLeashDistance;
		}
		offset.y = y;
		return origin + offset;
	}

	private void BeginSweep()
	{
		_wasSweepingPreviously = true;
		_sweepPath.Clear();
		if (_cursor != null && _cursor.GetWorldPosition(groundLayer, out var worldPos))
		{
			if (_objectBeingMoved != null)
			{
				Vector3 position = _objectBeingMoved.transform.position;
				_cursor.WarpToWorldPosition(position);
				_currentSweepTarget = position;
				_lastRecordedCursorPos = position;
			}
			else
			{
				_currentSweepTarget = ClampToLeash(worldPos);
				_lastRecordedCursorPos = ClampToLeash(worldPos);
			}
		}
	}

	private void ProcessSweep()
	{
		if (_cursor != null && _cursor.GetWorldPosition(groundLayer, out var worldPos))
		{
			worldPos = ClampToLeash(worldPos);
			_cursor.ToggleVisuals(show: true);
			if (Vector3.Distance(worldPos, _lastRecordedCursorPos) > pathResolution)
			{
				_sweepPath.Enqueue(new SweepNode
				{
					Position = worldPos,
					TimeAdded = Time.time
				});
				_lastRecordedCursorPos = worldPos;
			}
			UpdateSweepTargetAndDirectUnits();
		}
	}

	private void EndSweep()
	{
		_wasSweepingPreviously = false;
		_sweepPath.Clear();
		if (_cursor != null)
		{
			_cursor.ToggleVisuals(show: false);
		}
		if (!(_objectBeingMoved != null))
		{
			return;
		}
		if (_objectNavAgent != null && _objectMovable == null)
		{
			if (_objectNavAgent.isOnNavMesh)
			{
				_objectNavAgent.isStopped = true;
				_objectNavAgent.ResetPath();
			}
			UnityEngine.Object.Destroy(_objectNavAgent);
		}
		GlobalEvents.OnBuildingPlaced.Invoke(_objectBeingMoved);
		_objectBeingMoved = null;
		_objectNavAgent = null;
		_objectMovable = null;
	}

	private void UpdateSweepTargetAndDirectUnits()
	{
		while (_sweepPath.Count > 0 && Time.time - _sweepPath.Peek().TimeAdded > pathDecayTime)
		{
			_sweepPath.Dequeue();
		}
		if (_objectBeingMoved != null && _objectNavAgent != null)
		{
			if (_cursor.GetWorldPosition(groundLayer, out var worldPos))
			{
				_currentSweepTarget = ClampToLeash(worldPos);
			}
			DirectUnitsToPosition(_currentSweepTarget);
			return;
		}
		Vector3 zero = Vector3.zero;
		int num = 0;
		foreach (UnitAgent item in GetUnitsForCurrentSelection())
		{
			if (item.CurrentStateEnum != UnitState.AtFlag && item.CurrentStateEnum != UnitState.Carrying && item.CurrentStateEnum != UnitState.Interacting)
			{
				zero += item.transform.position;
				num++;
			}
		}
		if (num > 0)
		{
			zero /= (float)num;
			if (Vector3.Distance(zero, _currentSweepTarget) <= targetReachedRadius)
			{
				Vector3 worldPos2;
				if (_sweepPath.Count > 0)
				{
					_currentSweepTarget = _sweepPath.Dequeue().Position;
				}
				else if (_cursor.GetWorldPosition(groundLayer, out worldPos2))
				{
					_currentSweepTarget = ClampToLeash(worldPos2);
				}
			}
		}
		DirectUnitsToPosition(_currentSweepTarget);
	}

	private void DirectUnitsToPosition(Vector3 targetPos)
	{
		if (_objectBeingMoved != null && _objectNavAgent != null)
		{
			if (_objectMovable != null)
			{
				_objectMovable.ManualMove(targetPos);
				return;
			}
			if (_objectNavAgent.isActiveAndEnabled && _objectNavAgent.isOnNavMesh)
			{
				_objectNavAgent.MoveTo(targetPos, objectSweepSpeed);
			}
			return;
		}
		foreach (UnitAgent item in GetUnitsForCurrentSelection())
		{
			if (item.CurrentStateEnum != UnitState.AtFlag && item.CurrentStateEnum != UnitState.Carrying && item.CurrentStateEnum != UnitState.Interacting)
			{
				item.NavAgent.stoppingDistance = sweepStoppingDistance;
				float distFromTarget = Vector3.Distance(item.transform.position, targetPos);
				float speed = (distFromTarget > stragglerCatchUpDistance) ? item.BaseMoveSpeed * stragglerSpeedMultiplier : item.BaseMoveSpeed;
				item.MoveTo(targetPos, speed);
			}
		}
	}

	private void HandleSpawnLock(bool isLocked)
	{
		_isSystemBusy = isLocked;
	}

	private void HandleBuildingPlaced(GameObject building)
	{
		GuardFlag component = building.GetComponent<GuardFlag>();
		if (component == null)
		{
			return;
		}
		List<UnitAgent> activeUnitsByGroup = GetActiveUnitsByGroup(CurrentSelectedGroup);
		activeUnitsByGroup.AddRange(SpawnInactiveUnitsByGroup(CurrentSelectedGroup));
		foreach (UnitAgent item in activeUnitsByGroup)
		{
			item.transform.position = component.transform.position + UnityEngine.Random.insideUnitSphere * 2f;
			if ((bool)item.NavAgent)
			{
				item.NavAgent.Warp(item.transform.position);
			}
			item.EnterGuardMode(component);
		}
	}
}
