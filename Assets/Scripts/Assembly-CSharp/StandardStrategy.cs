using UnityEngine;

public class StandardStrategy : MovementStrategy
{
	public StandardStrategy(PlayerController controller)
		: base(controller)
	{
	}

	public override Vector3 CalculateVelocity(Vector2 input, float currentSpeed)
	{
		if (input.sqrMagnitude < 0.01f)
		{
			return Vector3.zero;
		}
		return GetCameraRelativeDirection(input) * currentSpeed;
	}

	public override float CalculateRotation(Vector2 input, float currentRotation)
	{
		if (input.sqrMagnitude < 0.01f)
		{
			return currentRotation;
		}
		Vector3 cameraRelativeDirection = GetCameraRelativeDirection(input);
		return Mathf.Atan2(cameraRelativeDirection.x, cameraRelativeDirection.z) * 57.29578f;
	}
}
