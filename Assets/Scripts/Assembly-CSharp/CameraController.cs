using UnityEngine;

public class CameraController : MonoBehaviour
{
	[Header("Targeting")]
	public Transform target;

	[Tooltip("Offset from the target's pivot (e.g., look at head instead of feet).")]
	public Vector3 pivotOffset = new Vector3(0f, 1.5f, 0f);

	[Header("Spherical Settings")]
	public float distanceFromTarget = 10f;

	public float minVerticalAngle = -10f;

	public float maxVerticalAngle = 85f;

	[Header("Input")]
	public float mouseSensitivity = 3f;

	[Tooltip("Time to smooth rotation. Lower = snappier, Higher = heavier.")]
	public float rotationSmoothTime = 0.12f;

	[SerializeField]
	private bool dragToMove = true;

	[Header("Sweep Settings")]
	[SerializeField]
	private float sweepDistanceMultiplier = 1.8f;

	[SerializeField]
	private float zoomSpeed = 5f;

	[SerializeField]
	private bool freezeRotationOnSweep = true;

	[Tooltip("The lowest the camera can tilt during a sweep. Forces an isometric view.")]
	[SerializeField]
	private float minSweepVerticalAngle = 45f;

	[Tooltip("The highest the camera can tilt during a sweep.")]
	[SerializeField]
	private float maxSweepVerticalAngle = 85f;

	[Header("Auto-Align")]
	public float autoAlignSpeed = 2f;

	private PlayerTargetingManager _targetingManager;

	private Vector3 _currentRotationVelocity;

	private Vector3 _targetRotation;

	private Vector3 _currentRotation;

	private float _currentDistance;

	private Vector2 _mouseInput;

	private Vector2 _moveInput;

	private void Start()
	{
		if (!(target == null))
		{
			_targetingManager = Object.FindObjectOfType<PlayerTargetingManager>();
			_targetRotation = new Vector3(20f, target.eulerAngles.y, 0f);
			_currentRotation = _targetRotation;
			_currentDistance = distanceFromTarget;
		}
	}

	private void OnEnable()
	{
		if (PlayerInputManager.Instance != null)
		{
			PlayerInputManager.Instance.OnCameraInput += UpdateCameraInput;
			PlayerInputManager.Instance.OnMoveInput += UpdateMoveInput;
		}
	}

	private void OnDisable()
	{
		if (PlayerInputManager.Instance != null)
		{
			PlayerInputManager.Instance.OnCameraInput -= UpdateCameraInput;
			PlayerInputManager.Instance.OnMoveInput -= UpdateMoveInput;
		}
	}

	private void UpdateCameraInput(Vector2 input)
	{
		_mouseInput = input;
	}

	private void UpdateMoveInput(Vector2 input)
	{
		_moveInput = input;
	}

	private void LateUpdate()
	{
		if (!(target == null))
		{
			ITargetable lockedTarget;
			if (ShouldFreezeRotation())
			{
				_mouseInput = Vector2.zero;
			}
			else if (ShouldLockOn(out lockedTarget))
			{
				HandleLockOn(lockedTarget);
			}
			else
			{
				HandleOrbitInput();
			}
			ApplyTransform();
		}
	}

	private bool ShouldFreezeRotation()
	{
		if (PlayerInputManager.Instance != null && (PlayerInputManager.Instance.IsSweeping || PlayerInputManager.Instance.IsDelayKeyHeld))
		{
			return freezeRotationOnSweep;
		}
		return false;
	}

	private bool ShouldLockOn(out ITargetable lockedTarget)
	{
		lockedTarget = null;
		if (PlayerInputManager.Instance != null && PlayerInputManager.Instance.IsSweeping)
		{
			return false;
		}
		if (_targetingManager != null && _targetingManager.CurrentTarget != null)
		{
			if (_targetingManager.CurrentTarget as Object == null)
			{
				return false;
			}
			if (!_targetingManager.CurrentTarget.IsTargetable)
			{
				return false;
			}
			lockedTarget = _targetingManager.CurrentTarget;
			return true;
		}
		return false;
	}

	private void HandleLockOn(ITargetable lockedTarget)
	{
		if (lockedTarget != null)
		{
			Vector3 vector = target.position + pivotOffset;
			Vector3 normalized = (lockedTarget.GetTargetPosition() - vector).normalized;
			if (normalized != Vector3.zero)
			{
				Vector3 eulerAngles = Quaternion.LookRotation(normalized).eulerAngles;
				_targetRotation.y = eulerAngles.y;
				_targetRotation.x = eulerAngles.x;
			}
			_mouseInput = Vector2.zero;
		}
	}

	private void HandleOrbitInput()
	{
		bool flag = PlayerInputManager.Instance != null && PlayerInputManager.Instance.IsRightClickHeld;
		if (!dragToMove || flag)
		{
			_targetRotation.y += _mouseInput.x * mouseSensitivity;
			_targetRotation.x -= _mouseInput.y * mouseSensitivity;
		}
		else if (_moveInput.sqrMagnitude > 0.01f && autoAlignSpeed > 0f)
		{
			float y = target.eulerAngles.y;
			_targetRotation.y = Mathf.LerpAngle(_targetRotation.y, y, autoAlignSpeed * Time.deltaTime);
		}
		_mouseInput = Vector2.zero;
	}

	private void ApplyTransform()
	{
		int num;
		if (PlayerInputManager.Instance != null)
		{
			num = (PlayerInputManager.Instance.IsSweeping ? 1 : 0);
			if (num != 0)
			{
				_targetRotation.x = Mathf.Clamp(_targetRotation.x, minSweepVerticalAngle, maxSweepVerticalAngle);
				goto IL_006d;
			}
		}
		else
		{
			num = 0;
		}
		_targetRotation.x = Mathf.Clamp(_targetRotation.x, minVerticalAngle, maxVerticalAngle);
		goto IL_006d;
		IL_006d:
		_currentRotation.x = Mathf.SmoothDampAngle(_currentRotation.x, _targetRotation.x, ref _currentRotationVelocity.x, rotationSmoothTime);
		_currentRotation.y = Mathf.SmoothDampAngle(_currentRotation.y, _targetRotation.y, ref _currentRotationVelocity.y, rotationSmoothTime);
		Quaternion quaternion = Quaternion.Euler(_currentRotation.x, _currentRotation.y, 0f);
		float b = ((num != 0) ? (distanceFromTarget * sweepDistanceMultiplier) : distanceFromTarget);
		_currentDistance = Mathf.Lerp(_currentDistance, b, Time.deltaTime * zoomSpeed);
		Vector3 position = target.position + pivotOffset + quaternion * Vector3.back * _currentDistance;
		base.transform.position = position;
		base.transform.LookAt(target.position + pivotOffset);
	}
}
