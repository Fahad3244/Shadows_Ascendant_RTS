using UnityEngine;

public class ProjectileVisual : MonoBehaviour
{
	private Vector3 _targetPosition;
	private float _speed;

	public void Launch(Vector3 targetPosition, float speed)
	{
		_targetPosition = targetPosition;
		_speed = speed;
	}

	private void Update()
	{
		transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _speed * Time.deltaTime);
		if (Vector3.Distance(transform.position, _targetPosition) < 0.15f)
		{
			Destroy(gameObject);
		}
	}
}
