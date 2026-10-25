using UnityEngine;
using UnityEngine.Events;

public enum BreakableSize
{
	Small,
	Large
}

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(AttachableTarget))]
public class BreakableObject : MonoBehaviour
{
	[Header("Type")]
	[SerializeField]
	private BreakableSize size = BreakableSize.Small;

	[SerializeField]
	private float maxHealth = 50f;

	[Tooltip("Troops cannot damage it. Auto-scan ignores it; targeted troops pause and return.")]
	[SerializeField]
	private bool indestructible;

	[Header("Break")]
	[SerializeField]
	private float destroyDelay = 0f;

	[SerializeField]
	private GameObject breakEffectPrefab;

	[SerializeField]
	private UnityEvent onBroken;

	private Health _health;

	private AttachableTarget _attachable;

	private void Awake()
	{
		_health = GetComponent<Health>();
		_attachable = GetComponent<AttachableTarget>();
		_health.Initialize(maxHealth);
		_attachable.SetUnlimitedSlots(size == BreakableSize.Small);
		_attachable.SetIndestructible(indestructible);
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
		if (breakEffectPrefab)
		{
			Object.Instantiate(breakEffectPrefab, transform.position, transform.rotation);
		}
		_attachable.EjectAllUnits();
		onBroken.Invoke();
		Object.Destroy(base.gameObject, destroyDelay);
	}
}
