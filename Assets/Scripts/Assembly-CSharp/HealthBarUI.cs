using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
	public enum VisibilityMode
	{
		AlwaysShow = 0,
		ShowOnDamageAndTarget = 1,
		Hidden = 2
	}

	[Header("Dependencies")]
	[SerializeField]
	private Health targetHealth;

	[SerializeField]
	private Image fillImage;

	[Header("Settings")]
	public VisibilityMode visibilityMode;

	[SerializeField]
	private float hideDelay = 3f;

	private Canvas _uiCanvas;

	private float _hideTimer;

	private bool _isTargeted;

	private Camera _mainCamera;

	private void Awake()
	{
		_mainCamera = Camera.main;
		_uiCanvas = GetComponentInParent<Canvas>();
		if (targetHealth == null)
		{
			targetHealth = GetComponentInParent<Health>();
		}
	}

	private void OnEnable()
	{
		if (targetHealth != null)
		{
			targetHealth.OnDamaged += HandleHealthChanged;
			targetHealth.OnDeath.AddListener(HandleDeath);
		}
	}

	private void OnDisable()
	{
		if (targetHealth != null)
		{
			targetHealth.OnDamaged -= HandleHealthChanged;
			targetHealth.OnDeath.RemoveListener(HandleDeath);
		}
	}

	private void Start()
	{
		UpdateFillAmount();
		EvaluateInitialVisibility();
	}

	private void LateUpdate()
	{
		if (targetHealth != null && targetHealth.IsDead)
		{
			_uiCanvas.enabled = false;
			return;
		}
		if (visibilityMode == VisibilityMode.ShowOnDamageAndTarget)
		{
			if (_isTargeted)
			{
				_uiCanvas.enabled = true;
				_hideTimer = hideDelay;
			}
			else if (_hideTimer > 0f)
			{
				_uiCanvas.enabled = true;
				_hideTimer -= Time.deltaTime;
			}
			else
			{
				_uiCanvas.enabled = false;
			}
		}
		if (_uiCanvas.enabled && _uiCanvas.renderMode == RenderMode.WorldSpace && _mainCamera != null)
		{
			base.transform.rotation = _mainCamera.transform.rotation;
		}
	}

	private void HandleHealthChanged(DamageInfo info)
	{
		UpdateFillAmount();
		if (visibilityMode == VisibilityMode.ShowOnDamageAndTarget)
		{
			_hideTimer = hideDelay;
		}
	}

	public void SetTargetedState(bool isTargeted)
	{
		_isTargeted = isTargeted;
	}

	private void EvaluateInitialVisibility()
	{
		_uiCanvas.enabled = visibilityMode == VisibilityMode.AlwaysShow;
	}

	private void HandleDeath()
	{
		_uiCanvas.enabled = false;
	}

	private void UpdateFillAmount()
	{
		if (!(targetHealth == null) && !(targetHealth.MaxHP <= 0f))
		{
			fillImage.fillAmount = targetHealth.CurrentHP / targetHealth.MaxHP;
		}
	}
}
