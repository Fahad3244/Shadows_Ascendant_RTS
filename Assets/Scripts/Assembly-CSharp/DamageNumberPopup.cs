using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class DamageNumberPopup : MonoBehaviour
{
	[Header("Animation Settings")]
	[SerializeField]
	private float floatDistance = 2f;

	[SerializeField]
	private float duration = 1f;

	[SerializeField]
	private Ease easeType = Ease.OutQuart;

	[Header("Visual Settings")]
	[SerializeField]
	private float fontSize = 16f;

	[SerializeField]
	private Color textColor = new Color(1f, 0.25f, 0.15f);

	private TextMeshPro _textMesh;

	private Camera _mainCamera;

	private void Awake()
	{
		_textMesh = GetComponent<TextMeshPro>();
		_mainCamera = Camera.main;
		_textMesh.fontSize = fontSize;
		Debug.Log($"[DamageNumberPopup] fontSize field = {fontSize}, applied to textMesh = {_textMesh.fontSize}");
		_textMesh.color = textColor;
	}

	private void Update()
	{
		if (_mainCamera != null)
		{
			base.transform.rotation = _mainCamera.transform.rotation;
		}
	}

	public void Initialize(float damageAmount)
	{
		_textMesh.text = damageAmount.ToString("F0");
		Sequence sequence = DOTween.Sequence();
		sequence.Join(base.transform.DOMoveY(base.transform.position.y + floatDistance, duration).SetEase(easeType));
		sequence.Join(_textMesh.DOFade(0f, duration).SetEase(easeType));
		sequence.OnComplete(delegate
		{
			Object.Destroy(base.gameObject);
		});
	}

	private void OnDestroy()
	{
		base.transform.DOKill();
		if (_textMesh != null)
		{
			_textMesh.DOKill();
		}
	}
}
