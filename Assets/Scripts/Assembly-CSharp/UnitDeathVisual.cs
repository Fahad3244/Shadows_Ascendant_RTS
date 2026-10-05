using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class UnitDeathVisual : MonoBehaviour
{
	[Header("Low Health Warning")]
	[SerializeField] private float warningThreshold = 0.25f;
	[SerializeField] private Vector3 warningOffset = new Vector3(0f, 2.5f, 0f);
	[SerializeField] private Color warningColor = Color.red;
	[SerializeField] private float pulseSpeed = 6f;
	[SerializeField] private float warningSize = 0.5f;

	[Header("Death")]
	[Tooltip("Same model parent used by HitFeedbackHandler. Falls back to the root if empty.")]
	[SerializeField] private Transform modelParent;
	[SerializeField] private float fallDuration = 0.35f;
	[SerializeField] private float lingerTime = 2.5f;
	[SerializeField] private float shrinkDuration = 0.5f;
	[SerializeField] private Color deadTint = new Color(0.35f, 0.35f, 0.35f);
	[Tooltip("Vertical shift applied on death. Negative = lower to the ground.")]
	[SerializeField] private float deadYOffset = -1f;
	[SerializeField] private float blinkSpeed = 5f;
	[SerializeField] private Color blinkColor = new Color(0.9f, 0.25f, 0.2f);

	private Health _health;
	private GameObject _warning;
	private Material _warningMat;
	private Sequence _deathSeq;
	private Renderer[] _deadRenderers;
	private MaterialPropertyBlock _deadBlock;
	private bool _isDead;

	private void Awake()
	{
		_health = GetComponent<Health>();
	}

	private void OnEnable()
	{
		_health.OnHealthChanged += HandleHealthChanged;
	}

	private void OnDisable()
	{
		_health.OnHealthChanged -= HandleHealthChanged;
		SetWarning(false);
	}

	private void Update()
	{
		if (_warning != null && _warning.activeSelf)
		{
			float s = warningSize * (1f + 0.35f * Mathf.Sin(Time.time * pulseSpeed));
			_warning.transform.localScale = Vector3.one * s;
		}
		if (_isDead && _deadRenderers != null)
		{
			float k = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f;
			Color c = Color.Lerp(deadTint, blinkColor, k);
			_deadBlock.SetColor("_Color", c);
			_deadBlock.SetColor("_BaseColor", c);
			foreach (Renderer r in _deadRenderers)
			{
				if (r != null) r.SetPropertyBlock(_deadBlock);
			}
		}
	}

	private void HandleHealthChanged(float current, float max)
	{
		if (_isDead || max <= 0f) return;
		SetWarning(current > 0f && current / max <= warningThreshold);
	}

	private void SetWarning(bool show)
	{
		if (show && _warning == null)
		{
			_warning = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			Destroy(_warning.GetComponent<Collider>());
			_warning.transform.SetParent(transform, false);
			_warning.transform.localPosition = warningOffset;
			_warningMat = new Material(Shader.Find("Sprites/Default")) { color = warningColor };
			_warning.GetComponent<Renderer>().material = _warningMat;
		}
		if (_warning != null) _warning.SetActive(show);
	}

	public void PlayDeath(System.Action onComplete)
	{
		_isDead = true;
		SetWarning(false);
		TintModel();

		Transform t = modelParent != null ? modelParent : transform;
		_deathSeq = DOTween.Sequence();
		_deathSeq.Append(t.DOLocalRotate(new Vector3(0f, 0f, 90f), fallDuration).SetEase(Ease.OutBounce));
		_deathSeq.Join(t.DOLocalMoveY(t.localPosition.y + deadYOffset, fallDuration).SetEase(Ease.OutQuad));
		_deathSeq.AppendInterval(lingerTime);
		_deathSeq.Append(t.DOScale(Vector3.zero, shrinkDuration).SetEase(Ease.InQuad));
		_deathSeq.OnComplete(() => onComplete?.Invoke());
	}

	private void TintModel()
	{
		Transform root = modelParent != null ? modelParent : transform;
		_deadBlock = new MaterialPropertyBlock();
		_deadRenderers = root.GetComponentsInChildren<Renderer>();
	}

	private void OnDestroy()
	{
		_deathSeq?.Kill();
		if (_warningMat != null) Destroy(_warningMat);
	}
}