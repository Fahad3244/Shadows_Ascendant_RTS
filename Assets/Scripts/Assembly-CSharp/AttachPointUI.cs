using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AttachPointUI : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI countText;

	[SerializeField]
	private Canvas canvas;

	[Header("Health Bar (enemies)")]
	[SerializeField]
	private Image healthFill;

	[SerializeField]
	private GameObject healthBarRoot;

	private Camera _mainCamera;

	private bool _isVisible;

	private void Awake()
	{
		_mainCamera = Camera.main;
		if ((bool)canvas)
		{
			canvas.enabled = false;
		}
	}

	private void Update()
	{
		if (_isVisible && canvas.enabled)
		{
			base.transform.rotation = _mainCamera.transform.rotation;
		}
	}

	public void SetVisibility(bool state)
	{
		if (_isVisible != state)
		{
			_isVisible = state;
			if ((bool)canvas)
			{
				canvas.enabled = state;
			}
		}
	}

	public void SetText(string text, Color color)
	{
		if (countText)
		{
			countText.text = text;
			countText.color = color;
		}
	}

	public void UpdateCount(int availableSlots)
	{
		if (countText)
		{
			countText.text = availableSlots.ToString();
			countText.color = ((availableSlots == 0) ? Color.red : Color.white);
		}
	}

	public void SetHealth01(float value)
	{
		if (healthFill)
		{
			healthFill.fillAmount = Mathf.Clamp01(value);
		}
	}

	public void ShowHealthBar(bool show)
	{
		if (healthBarRoot && healthBarRoot.activeSelf != show)
		{
			healthBarRoot.SetActive(show);
		}
	}
}
