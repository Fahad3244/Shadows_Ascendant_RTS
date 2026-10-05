using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class HitFeedbackHandler : MonoBehaviour
{
	[Header("Visual Feedback")]
	[Tooltip("The parent transform containing all renderers (e.g., the 'Model' or 'MovementModes' object).")]
	[SerializeField]
	private Transform modelParent;

	[SerializeField]
	private Material hitMaterial;

	[SerializeField]
	private float flashDuration = 0.15f;

	[Header("Damage Numbers")]
	[Tooltip("Requires a prefab with the DamageNumberPopup script attached.")]
	[SerializeField]
	private DamageNumberPopup damageNumberPrefab;

	[SerializeField]
	private Vector3 numberOffset = new Vector3(0f, 2f, 0f);

	[Header("Hit Punch")]
	[SerializeField]
	private float punchScaleAmount = 0.8f;

	[SerializeField]
	private float punchDuration = 0.15f;

	private Health _health;

	private Dictionary<Renderer, Material[]> _originalMaterialsCache = new Dictionary<Renderer, Material[]>();

	private float _flashTimer;

	private bool _isFlashing;

	private Vector3 _originalModelScale;

	private Coroutine _punchCoroutine;

	private void Awake()
	{
		_health = GetComponent<Health>();
		CacheRenderers();
		if (modelParent != null)
		{
			_originalModelScale = modelParent.localScale;
		}
	}

	private void OnEnable()
	{
		_health.OnDamaged += HandleDamageFeedback;
	}

	private void OnDisable()
	{
		_health.OnDamaged -= HandleDamageFeedback;
	}

	private void Update()
	{
		if (_isFlashing)
		{
			_flashTimer -= Time.deltaTime;
			if (_flashTimer <= 0f)
			{
				RestoreMaterials();
			}
		}
	}

	private void CacheRenderers()
	{
		if (modelParent == null)
		{
			Debug.LogWarning("[" + base.gameObject.name + "] HitFeedbackHandler is missing a Model Parent assignment.");
			return;
		}
		Renderer[] componentsInChildren = modelParent.GetComponentsInChildren<Renderer>(includeInactive: true);
		foreach (Renderer renderer in componentsInChildren)
		{
			_originalMaterialsCache[renderer] = renderer.materials;
		}
	}

	private void HandleDamageFeedback(DamageInfo info)
	{
		ApplyFlash();
		PlayHitPunch();
		if (damageNumberPrefab != null)
		{
			Object.Instantiate(damageNumberPrefab, base.transform.position + numberOffset, Quaternion.identity).Initialize(info.amount);
		}
	}

	private void PlayHitPunch()
	{
		if (modelParent == null)
		{
			return;
		}
		if (_punchCoroutine != null)
		{
			StopCoroutine(_punchCoroutine);
		}
		_punchCoroutine = StartCoroutine(HitPunchRoutine());
	}

	private System.Collections.IEnumerator HitPunchRoutine()
	{
		float elapsed = 0f;
		Vector3 squashScale = new Vector3(_originalModelScale.x * (2f - punchScaleAmount), _originalModelScale.y * punchScaleAmount, _originalModelScale.z * (2f - punchScaleAmount));
		while (elapsed < punchDuration)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / punchDuration;
			modelParent.localScale = Vector3.Lerp(squashScale, _originalModelScale, t);
			yield return null;
		}
		modelParent.localScale = _originalModelScale;
		_punchCoroutine = null;
	}

	private void ApplyFlash()
	{
		if (hitMaterial == null || _originalMaterialsCache.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<Renderer, Material[]> item in _originalMaterialsCache)
		{
			Renderer key = item.Key;
			if (key != null && key.gameObject.activeInHierarchy)
			{
				Material[] array = new Material[item.Value.Length];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = hitMaterial;
				}
				key.materials = array;
			}
		}
		_flashTimer = flashDuration;
		_isFlashing = true;
	}

	private void RestoreMaterials()
	{
		foreach (KeyValuePair<Renderer, Material[]> item in _originalMaterialsCache)
		{
			Renderer key = item.Key;
			if (key != null)
			{
				key.materials = item.Value;
			}
		}
		_isFlashing = false;
	}
}
