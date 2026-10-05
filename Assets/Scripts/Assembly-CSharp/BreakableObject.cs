using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(AttachableTarget))]
public class BreakableObject : MonoBehaviour
{
	private Health _health;

	private AttachableTarget _attachable;

	private void Awake()
	{
		_health = GetComponent<Health>();
		_attachable = GetComponent<AttachableTarget>();
		_health.Initialize(50f);
	}

	private void OnEnable()
	{
		_health.OnDeath.AddListener(Break);
	}

	private void OnDisable()
	{
		_health.OnDeath.RemoveListener(Break);
	}

	private void Break()
	{
		Debug.Log(base.gameObject.name + " broke!");
		_attachable.EjectAllUnits();
		Object.Destroy(base.gameObject);
	}
}
