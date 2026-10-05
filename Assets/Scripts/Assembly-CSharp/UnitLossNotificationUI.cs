using TMPro;
using DG.Tweening;
using UnityEngine;

public class UnitLossNotificationUI : MonoBehaviour
{
	public static UnitLossNotificationUI Instance { get; private set; }

	[SerializeField]
	private TextMeshProUGUI messageText;

	private void Awake()
	{
		Instance = this;
		if (messageText != null)
		{
			messageText.gameObject.SetActive(false);
		}
	}

	public void ShowMessage(string message)
	{
		if (messageText == null) return;
		messageText.gameObject.SetActive(true);
		messageText.text = message;
		messageText.transform.localScale = Vector3.one * 0.5f;
		messageText.alpha = 1f;
		Sequence sequence = DOTween.Sequence();
		sequence.Join(messageText.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
		sequence.AppendInterval(1f);
		sequence.Append(messageText.DOFade(0f, 0.5f));
		sequence.OnComplete(() => messageText.gameObject.SetActive(false));
	}
}
