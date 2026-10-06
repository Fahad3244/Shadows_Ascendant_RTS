using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AttachableTarget))]
public class InteractableObject : MonoBehaviour
{
	[Header("Operation")]
	[Tooltip("Seconds the required troops must keep operating it.")]
	[SerializeField]
	private float operateDuration = 3f;

	[SerializeField]
	private bool singleUse = true;

	[Tooltip("Progress resets if troops drop below the requirement.")]
	[SerializeField]
	private bool resetProgressWhenTroopsLeave;

	[Header("Events")]
	[SerializeField]
	private UnityEvent onOperateStart;

	[SerializeField]
	private UnityEvent onOperateStop;

	[SerializeField]
	private UnityEvent onCompleted;

	[Header("Debug")]
	[SerializeField]
	private float progress;

	private AttachableTarget _attach;

	private bool _operating;

	private bool _done;

	public float Progress01 => operateDuration > 0f ? Mathf.Clamp01(progress / operateDuration) : 1f;

	private void Awake()
	{
		_attach = GetComponent<AttachableTarget>();
	}

	private void Update()
	{
		if (_done)
		{
			return;
		}
		bool enough = _attach.AttachedUnitCount > 0 && _attach.MeetsRequirement;
		if (enough != _operating)
		{
			_operating = enough;
			if (enough)
			{
				onOperateStart.Invoke();
			}
			else
			{
				onOperateStop.Invoke();
			}
		}
		if (_operating)
		{
			progress += Time.deltaTime;
			if (progress >= operateDuration)
			{
				Complete();
			}
		}
		else if (resetProgressWhenTroopsLeave)
		{
			progress = 0f;
		}
	}

	private void Complete()
	{
		progress = 0f;
		_operating = false;
		if (singleUse)
		{
			_done = true;
		}
		onCompleted.Invoke();
		_attach.ReleaseAllUnits();
		if (singleUse)
		{
			_attach.SetLocked(true);
		}
	}
}