using UnityEngine;

public abstract class MovementStrategy
{
	protected PlayerController _controller;

	protected Transform _cameraTransform;

	public MovementStrategy(PlayerController controller)
	{
		_controller = controller;
		if (Camera.main != null)
		{
			_cameraTransform = Camera.main.transform;
		}
	}

	public abstract Vector3 CalculateVelocity(Vector2 input, float currentSpeed);

	public abstract float CalculateRotation(Vector2 input, float currentRotation);

	protected Vector3 GetCameraRelativeDirection(Vector2 input)
	{
		if (_cameraTransform == null)
		{
			return new Vector3(input.x, 0f, input.y);
		}
		Vector3 forward = _cameraTransform.forward;
		Vector3 right = _cameraTransform.right;
		forward.y = 0f;
		right.y = 0f;
		forward.Normalize();
		right.Normalize();
		return (forward * input.y + right * input.x).normalized;
	}
}
