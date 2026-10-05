using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
	[Header("Dependencies")]
	[SerializeField]
	private PlayerUnitManager unitManager;

	[SerializeField]
	private PlayerTargetingManager targetingManager;

	[Header("Configuration")]
	public float baseMoveSpeed = 6f;

	public float rotationSpeed = 10f;

	public float smoothTime = 0.1f;

	public float gravity = -20f;

	[Header("Visuals")]
	[Tooltip("The root GameObject for the standard walking visual.")]
	[SerializeField]
	private GameObject standardModelRoot;

	[Tooltip("The root GameObject for the sled visual.")]
	[SerializeField]
	private GameObject sledModelRoot;

	[Tooltip("The root GameObject for the turret visual.")]
	[SerializeField]
	private GameObject turretModelRoot;

	private MovementStrategy _currentStrategy;

	private TurretStrategy _turretMode;

	private SledStrategy _sledMode;

	private StandardStrategy _standardMode;

	private CharacterController _controller;

	private Vector2 _currentInput;

	private Vector3 _currentVelocity;

	private Vector3 _smoothDampVelocity;

	private float _verticalVelocity;

	private bool _isControlLocked;

	private void Awake()
	{
		_controller = GetComponent<CharacterController>();
		if (unitManager == null)
		{
			unitManager = Object.FindObjectOfType<PlayerUnitManager>();
		}
		_turretMode = new TurretStrategy(this);
		_sledMode = new SledStrategy(this);
		_standardMode = new StandardStrategy(this);
		_currentStrategy = _standardMode;
		SetControlLock(isLocked: false);
	}

	private void Start()
	{
		UpdateVisuals(_currentStrategy);
	}

	private void OnEnable()
	{
		if (PlayerInputManager.Instance != null)
		{
			PlayerInputManager.Instance.OnMoveInput += UpdateMovementInput;
		}
		if (unitManager != null)
		{
			unitManager.OnUnitsChanged += CheckMobilityState;
		}
	}

	private void OnDisable()
	{
		if (PlayerInputManager.Instance != null)
		{
			PlayerInputManager.Instance.OnMoveInput -= UpdateMovementInput;
		}
		if (unitManager != null)
		{
			unitManager.OnUnitsChanged -= CheckMobilityState;
		}
	}

	private void UpdateMovementInput(Vector2 input)
	{
		if (_isControlLocked)
		{
			_currentInput = Vector2.zero;
		}
		else
		{
			_currentInput = input;
		}
	}

	private void CheckMobilityState()
	{
		int freeCount = unitManager.FreeCount;
		MovementStrategy standardMode = _standardMode;
		standardMode = ((freeCount <= 4) ? _turretMode : ((freeCount > 8) ? ((MovementStrategy)_standardMode) : ((MovementStrategy)_sledMode)));
		if (_currentStrategy != standardMode)
		{
			_currentStrategy = standardMode;
			UpdateVisuals(_currentStrategy);
		}
	}

	private void UpdateVisuals(MovementStrategy activeStrategy)
	{
		if ((bool)standardModelRoot)
		{
			standardModelRoot.SetActive(value: false);
		}
		if ((bool)sledModelRoot)
		{
			sledModelRoot.SetActive(value: false);
		}
		if ((bool)turretModelRoot)
		{
			turretModelRoot.SetActive(value: false);
		}
		if (activeStrategy == _standardMode && (bool)standardModelRoot)
		{
			standardModelRoot.SetActive(value: true);
		}
		else if (activeStrategy == _sledMode && (bool)sledModelRoot)
		{
			sledModelRoot.SetActive(value: true);
		}
		else if (activeStrategy == _turretMode && (bool)turretModelRoot)
		{
			turretModelRoot.SetActive(value: true);
		}
	}

	private void Update()
	{
		HandleGravity();
		HandleStrategyMovement();
	}

	private void HandleGravity()
	{
		if (_controller.isGrounded && _verticalVelocity < 0f)
		{
			_verticalVelocity = -2f;
		}
		_verticalVelocity -= gravity * Time.deltaTime;
	}

	private void HandleStrategyMovement()
	{
		float b;
		if (targetingManager != null && targetingManager.CurrentTarget != null && targetingManager.CurrentTarget as Object != null)
		{
			Vector3 vector = targetingManager.CurrentTarget.GetTransform().position - base.transform.position;
			vector.y = 0f;
			b = ((!(vector != Vector3.zero)) ? base.transform.eulerAngles.y : Quaternion.LookRotation(vector).eulerAngles.y);
		}
		else
		{
			b = _currentStrategy.CalculateRotation(_currentInput, base.transform.eulerAngles.y);
		}
		float y = Mathf.LerpAngle(base.transform.eulerAngles.y, b, rotationSpeed * Time.deltaTime);
		base.transform.rotation = Quaternion.Euler(0f, y, 0f);
		Vector3 target = _currentStrategy.CalculateVelocity(_currentInput, baseMoveSpeed);
		_currentVelocity = Vector3.SmoothDamp(_currentVelocity, target, ref _smoothDampVelocity, smoothTime);
		Vector3 currentVelocity = _currentVelocity;
		currentVelocity.y = _verticalVelocity;
		_controller.Move(currentVelocity * Time.deltaTime);
	}

	public void SetControlLock(bool isLocked)
	{
		_isControlLocked = isLocked;
		if (_isControlLocked)
		{
			_currentVelocity = Vector3.zero;
			_currentInput = Vector2.zero;
		}
	}
}
