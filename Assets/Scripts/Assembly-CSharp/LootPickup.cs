using UnityEngine;

public class LootPickup : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField]
	private LootType type;

	[SerializeField]
	private int amount = 1;

	[Header("Magnet Settings")]
	[SerializeField]
	private float magnetRange = 3f;

	[SerializeField]
	private float magnetSpeed = 10f;

	[SerializeField]
	private LayerMask collectorLayers;

	private Transform _targetTransform;

	private float _scanTimer;

	private const float SCAN_INTERVAL = 0.2f;

	private void Update()
	{
		if (_targetTransform == null)
		{
			HandleScanning();
		}
		else
		{
			HandleMovement();
		}
	}

	private void HandleScanning()
	{
		_scanTimer += Time.deltaTime;
		if (_scanTimer >= 0.2f)
		{
			_scanTimer = 0f;
			ScanForCollector();
		}
	}

	private void ScanForCollector()
	{
		Collider[] array = Physics.OverlapSphere(base.transform.position, magnetRange, collectorLayers);
		if (array.Length != 0)
		{
			_targetTransform = array[0].transform;
		}
	}

	private void HandleMovement()
	{
		if (_targetTransform == null || !_targetTransform.gameObject.activeInHierarchy)
		{
			_targetTransform = null;
			return;
		}
		base.transform.position = Vector3.MoveTowards(base.transform.position, _targetTransform.position, magnetSpeed * Time.deltaTime);
		if (Vector3.Distance(base.transform.position, _targetTransform.position) < 0.5f)
		{
			Collect();
		}
	}

	private void Collect()
	{
		Debug.Log($"[Loot] Collected {amount} {type}!");
		Object.Destroy(base.gameObject);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(base.transform.position, magnetRange);
	}
}
