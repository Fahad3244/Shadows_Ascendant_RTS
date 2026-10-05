using UnityEngine;

public class SledStrategy : MovementStrategy
{
	private float _sledSpeedMultiplier = 1.5f;

	public SledStrategy(PlayerController controller)
		: base(controller)
	{
	}

	public override Vector3 CalculateVelocity(Vector2 input, float currentSpeed)
	{
		float y = input.y;
		if (Mathf.Abs(y) < 0.01f)
		{
			return Vector3.zero;
		}
		return _controller.transform.forward * y * (currentSpeed * _sledSpeedMultiplier);
	}

	public override float CalculateRotation(Vector2 input, float currentRotation)
	{
		return 0f;
	}
}
