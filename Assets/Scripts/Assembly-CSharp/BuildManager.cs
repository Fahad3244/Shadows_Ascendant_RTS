using UnityEngine;

public class BuildManager : MonoBehaviour
{
	[Header("Defaults:Temporary")]
	[SerializeField]
	private GameObject defaultFlagPrefab;

	[Header("Instant Placement")]
	[SerializeField]
	private bool useInstantPlacement = true;

	[SerializeField]
	private Transform playerTransform;

	[SerializeField]
	private float instantPlaceDistance = 3f;

	[Header("Settings")]
	[SerializeField]
	private LayerMask placementLayer;

	[Header("Dependencies")]
	[SerializeField]
	private PlayerUnitManager unitManager;

	[SerializeField]
	private SwarmController swarmController;

	private bool _isInBuildMode;

	private GameObject _currentGhost;

	private GameObject _activeBuildingPrefab;

	private VirtualCursor _cursor;

	private void Awake()
	{
		if (playerTransform == null)
		{
			GameObject gameObject = GameObject.FindGameObjectWithTag("Player");
			if ((bool)gameObject)
			{
				playerTransform = gameObject.transform;
			}
		}
		if (unitManager == null)
		{
			unitManager = Object.FindObjectOfType<PlayerUnitManager>();
		}
		if (swarmController == null)
		{
			swarmController = Object.FindObjectOfType<SwarmController>();
		}
	}

	private void Start()
	{
		_cursor = VirtualCursor.Instance;
		if (_cursor == null)
		{
			Debug.LogError("VirtualCursor missing!");
		}
	}

	private void OnEnable()
	{
		if (PlayerInputManager.Instance != null)
		{
			PlayerInputManager.Instance.OnGuardPressed += HandleGuardShortcut;
			PlayerInputManager.Instance.OnLeftTap += OnLeftInput;
			PlayerInputManager.Instance.OnRightTap += OnRightInput;
		}
	}

	private void OnDisable()
	{
		if (PlayerInputManager.Instance != null)
		{
			PlayerInputManager.Instance.OnGuardPressed -= HandleGuardShortcut;
			PlayerInputManager.Instance.OnLeftTap -= OnLeftInput;
			PlayerInputManager.Instance.OnRightTap -= OnRightInput;
		}
	}

	private void OnLeftInput()
	{
		if (_isInBuildMode)
		{
			FinalizeAction();
		}
	}

	private void OnRightInput()
	{
		if (_isInBuildMode)
		{
			CancelAll();
		}
	}

	private void HandleGuardShortcut()
	{
		if (_isInBuildMode)
		{
			CancelAll();
		}
		else if (swarmController != null && !swarmController.HasAvailableUnitsForGroup(swarmController.CurrentSelectedGroup))
		{
			Debug.LogWarning("[BuildManager] Cannot place flag: No available units of the selected type.");
		}
		else if (useInstantPlacement)
		{
			PerformInstantPlacement();
		}
		else
		{
			SelectBuilding(defaultFlagPrefab);
		}
	}

	public void SelectBuilding(GameObject prefab)
	{
		if (_isInBuildMode)
		{
			CancelAll();
		}
		_activeBuildingPrefab = prefab;
		StartBuild();
	}

	private void PerformInstantPlacement()
	{
		if (!(playerTransform == null) && !(defaultFlagPrefab == null) && Physics.Raycast(playerTransform.position + playerTransform.forward * instantPlaceDistance + Vector3.up * 2f, Vector3.down, out var hitInfo, 10f, placementLayer))
		{
			GameObject gameObject = Object.Instantiate(defaultFlagPrefab, hitInfo.point, Quaternion.identity);
			float verticalOffset = GetVerticalOffset(gameObject);
			gameObject.transform.position = hitInfo.point + Vector3.up * verticalOffset;
			GlobalEvents.OnBuildingPlaced.Invoke(gameObject);
		}
	}

	private void StartBuild()
	{
		_isInBuildMode = true;
		_currentGhost = Object.Instantiate(_activeBuildingPrefab);
		Vector3 position = playerTransform.position + playerTransform.forward * instantPlaceDistance;
		_currentGhost.transform.position = position;
		GlobalEvents.OnBuildModeChanged.Invoke(payload: true);
	}

	private void FinalizeAction()
	{
		if (swarmController != null && !swarmController.HasAvailableUnitsForGroup(swarmController.CurrentSelectedGroup))
		{
			Debug.LogWarning("[BuildManager] Cannot place flag: No available units of the selected type.");
			CancelAll();
			return;
		}
		Vector3 position = _currentGhost.transform.position;
		GameObject activeBuildingPrefab = _activeBuildingPrefab;
		if ((bool)_currentGhost)
		{
			Object.Destroy(_currentGhost);
		}
		GameObject gameObject = Object.Instantiate(activeBuildingPrefab, position, Quaternion.identity);
		float verticalOffset = GetVerticalOffset(gameObject);
		gameObject.transform.position += Vector3.up * verticalOffset;
		GlobalEvents.OnBuildingPlaced.Invoke(gameObject);
		_isInBuildMode = false;
		GlobalEvents.OnBuildModeChanged.Invoke(payload: false);
	}

	private void CancelAll()
	{
		_activeBuildingPrefab = null;
		if ((bool)_currentGhost)
		{
			Object.Destroy(_currentGhost);
		}
		_isInBuildMode = false;
		GlobalEvents.OnBuildModeChanged.Invoke(payload: false);
	}

	private float GetVerticalOffset(GameObject obj)
	{
		Renderer component = obj.GetComponent<Renderer>();
		if (!(component != null))
		{
			return 0f;
		}
		return component.bounds.extents.y;
	}
}
