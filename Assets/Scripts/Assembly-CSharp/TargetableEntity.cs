using UnityEngine;

public class TargetableEntity : MonoBehaviour, ITargetable
{
	[SerializeField]
	private Vector3 targetOffset = Vector3.zero;

	[SerializeField]
	private TargetTeam team;

	[SerializeField]
	private HealthBarUI healthUI;

	public bool IsTargetable
	{
		get
		{
			if (this != null)
			{
				return base.gameObject.activeInHierarchy;
			}
			return false;
		}
	}

	public TargetTeam Team => team;

	public Vector3 GetTargetPosition()
	{
		return base.transform.position + targetOffset;
	}

	public Transform GetTransform()
	{
		return base.transform;
	}

	public void OnTargetSelected()
	{
		if (healthUI != null)
		{
			healthUI.SetTargetedState(isTargeted: true);
		}
		if (team == TargetTeam.Object)
		{
			AttachableTarget component = GetComponent<AttachableTarget>();
			if (component != null && component.CanBeMoved)
			{
				GlobalEvents.OnReadyToMove.Invoke(base.gameObject);
			}
		}
	}

	public void OnTargetDeselected()
	{
		if (healthUI != null)
		{
			healthUI.SetTargetedState(isTargeted: false);
		}
		GlobalEvents.OnReadyToMove.Invoke(null);
	}
}
