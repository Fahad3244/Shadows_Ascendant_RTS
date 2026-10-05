using System;
using UnityEngine;

public class PlayerInputManager : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField]
	private float tapThreshold = 0.25f;

	[SerializeField]
	private float longHoldThreshold = 1f;

	[SerializeField]
	private float dragThreshold = 0.5f;

	[Header("Sweep Settings")]
	[SerializeField]
	private float sweepBufferTime = 0.1f;

	[SerializeField]
	private float sweepExitCooldown = 0.2f;

	[Header("Delay Command Settings")]
	[SerializeField]
	private KeyCode delayModifierKey = KeyCode.LeftControl;

	private float _leftClickTimer;

	private bool _leftHoldTriggered;

	private float _rightClickTimer;

	private bool _rightMediumTriggered;

	private bool _rightLongTriggered;

	private float _rightMouseDragAccumulator;

	private float _sweepEntryTimer;

	private float _sweepCooldownTimer;

	private float _delayKeyTimer;

	private bool _delayKeyWasHeld;

	public static PlayerInputManager Instance { get; private set; }

	public bool IsRightClickHeld { get; private set; }

	public bool IsSweeping { get; private set; }

	public bool IsInputLocked
	{
		get
		{
			if (!IsSweeping)
			{
				return _sweepCooldownTimer > 0f;
			}
			return true;
		}
	}

	public bool IsDelayKeyHeld { get; private set; }

	public event Action<Vector2> OnMoveInput;

	public event Action<Vector2> OnCameraInput;

	public event Action OnJumpPressed;

	public event Action OnLeftTap;

	public event Action OnLeftHold;

	public event Action OnRightTap;

	public event Action OnRightMediumHold;

	public event Action OnRightLongHold;

	public event Action<GroupSelection> OnGroupSelectionInput;

	public event Action OnDelayRecordStarted;

	public event Action OnDelayRecordCommitted;

	public event Action OnDelayExecuteTapped;

	public event Action OnGuardPressed;

	public event Action OnTargetTogglePressed;

	public event Action OnTargetCyclePressed;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		else
		{
			Instance = this;
		}
	}

	private void Update()
	{
		HandleSweepLogic();
		HandleLocomotion();
		if (!IsInputLocked)
		{
			HandleLeftClick();
			HandleRightClick();
		}
		HandleKeyActions();
		HandleGroupSelectionInput();
		HandleDelayKeyLogic();
	}

	private void HandleLeftClick()
	{
		if (Input.GetMouseButtonDown(0))
		{
			_leftClickTimer = 0f;
			_leftHoldTriggered = false;
		}
		if (Input.GetMouseButton(0))
		{
			_leftClickTimer += Time.deltaTime;
			if (_leftClickTimer >= tapThreshold && !_leftHoldTriggered)
			{
				_leftHoldTriggered = true;
				this.OnLeftHold?.Invoke();
			}
		}
		if (Input.GetMouseButtonUp(0) && !_leftHoldTriggered)
		{
			this.OnLeftTap?.Invoke();
		}
	}

	private void HandleRightClick()
	{
		IsRightClickHeld = Input.GetMouseButton(1);
		if (Input.GetMouseButtonDown(1))
		{
			_rightClickTimer = 0f;
			_rightMediumTriggered = false;
			_rightLongTriggered = false;
			_rightMouseDragAccumulator = 0f;
		}
		if (Input.GetMouseButton(1))
		{
			_rightClickTimer += Time.deltaTime;
			float num = Mathf.Abs(Input.GetAxis("Mouse X")) + Mathf.Abs(Input.GetAxis("Mouse Y"));
			_rightMouseDragAccumulator += num;
			if (_rightMouseDragAccumulator < dragThreshold)
			{
				if (_rightClickTimer >= tapThreshold && !_rightMediumTriggered)
				{
					_rightMediumTriggered = true;
					this.OnRightMediumHold?.Invoke();
				}
				if (_rightClickTimer >= longHoldThreshold && !_rightLongTriggered)
				{
					_rightLongTriggered = true;
					this.OnRightLongHold?.Invoke();
				}
			}
		}
		if (Input.GetMouseButtonUp(1) && !_rightMediumTriggered && !_rightLongTriggered && _rightMouseDragAccumulator < dragThreshold)
		{
			this.OnRightTap?.Invoke();
		}
	}

	private void HandleDelayKeyLogic()
	{
		IsDelayKeyHeld = Input.GetKey(delayModifierKey);
		if (Input.GetKeyDown(delayModifierKey))
		{
			_delayKeyTimer = 0f;
			_delayKeyWasHeld = false;
		}
		if (IsDelayKeyHeld)
		{
			_delayKeyTimer += Time.deltaTime;
			if (_delayKeyTimer > tapThreshold && !_delayKeyWasHeld)
			{
				_delayKeyWasHeld = true;
				this.OnDelayRecordStarted?.Invoke();
			}
		}
		if (Input.GetKeyUp(delayModifierKey))
		{
			if (!_delayKeyWasHeld)
			{
				this.OnDelayExecuteTapped?.Invoke();
			}
			else
			{
				this.OnDelayRecordCommitted?.Invoke();
			}
			_delayKeyWasHeld = false;
			_delayKeyTimer = 0f;
		}
	}

	private void HandleSweepLogic()
	{
		if (Input.GetMouseButton(0) && Input.GetMouseButton(1))
		{
			_sweepEntryTimer += Time.deltaTime;
			if (_sweepEntryTimer >= sweepBufferTime && !IsSweeping)
			{
				IsSweeping = true;
			}
		}
		else
		{
			_sweepEntryTimer = 0f;
			if (IsSweeping)
			{
				IsSweeping = false;
				_sweepCooldownTimer = sweepExitCooldown;
			}
		}
		if (_sweepCooldownTimer > 0f)
		{
			_sweepCooldownTimer -= Time.deltaTime;
		}
	}

	private void HandleLocomotion()
	{
		float axisRaw = Input.GetAxisRaw("Horizontal");
		float axisRaw2 = Input.GetAxisRaw("Vertical");
		this.OnMoveInput?.Invoke(new Vector2(axisRaw, axisRaw2));
		float axis = Input.GetAxis("Mouse X");
		float axis2 = Input.GetAxis("Mouse Y");
		if (Mathf.Abs(axis) > 0.01f || Mathf.Abs(axis2) > 0.01f)
		{
			this.OnCameraInput?.Invoke(new Vector2(axis, axis2));
		}
		if (Input.GetButtonDown("Jump"))
		{
			this.OnJumpPressed?.Invoke();
		}
	}

	private void HandleKeyActions()
	{
		if (Input.GetKeyDown(KeyCode.Q))
		{
			this.OnGuardPressed?.Invoke();
		}
		if (Input.GetKeyDown(KeyCode.X))
		{
			this.OnTargetTogglePressed?.Invoke();
		}
		if (Input.GetKeyDown(KeyCode.Z))
		{
			this.OnTargetCyclePressed?.Invoke();
		}
	}

	private void HandleGroupSelectionInput()
	{
		if (Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.Tilde))
		{
			this.OnGroupSelectionInput?.Invoke(GroupSelection.All);
		}
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			this.OnGroupSelectionInput?.Invoke(GroupSelection.Builder);
		}
		if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			this.OnGroupSelectionInput?.Invoke(GroupSelection.Ranged);
		}
		if (Input.GetKeyDown(KeyCode.Alpha3))
		{
			this.OnGroupSelectionInput?.Invoke(GroupSelection.Melee);
		}
		if (Input.GetKeyDown(KeyCode.Alpha4))
		{
			this.OnGroupSelectionInput?.Invoke(GroupSelection.Medic);
		}
		if (Input.GetKeyDown(KeyCode.Alpha5))
		{
			this.OnGroupSelectionInput?.Invoke(GroupSelection.Custom);
		}
	}
}
