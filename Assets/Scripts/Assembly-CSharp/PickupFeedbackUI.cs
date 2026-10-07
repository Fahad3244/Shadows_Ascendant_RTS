using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class PickupFeedbackUI : MonoBehaviour
{
    private class Popup
    {
        public GameObject go;
        public TextMeshPro tmp;
        public float rise;
        public float stack;
    }

    [Header("References")]
    [Tooltip("Leave empty to auto-find the object tagged 'Player'.")]
    [SerializeField] private Transform target;

    [Header("Position")]
    [SerializeField] private Vector3 headOffset = new Vector3(0f, 3f, 0f);
    [Tooltip("Vertical gap between popups shown at the same time.")]
    [SerializeField] private float stackSpacing = 0.9f;

    [Header("Animation")]
    [SerializeField] private float floatDistance = 1.5f;
    [SerializeField] private float duration = 1.4f;
    [SerializeField] private float popInDuration = 0.25f;
    [SerializeField] private float startScale = 0.4f;
    [Range(0f, 1f)]
    [Tooltip("Fraction of the duration after which the fade-out begins.")]
    [SerializeField] private float fadeStart = 0.6f;

    [Header("Text")]
    [Tooltip("{0} = amount, {1} = loot type")]
    [SerializeField] private string format = "+{0} {1}";
    [SerializeField] private float fontSize = 6f;
    [SerializeField] private Color goldColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color gearColor = new Color(0.4f, 0.8f, 1f);
    [SerializeField] private Color potionColor = new Color(0.4f, 1f, 0.5f);

    private readonly List<Popup> _active = new List<Popup>();
    private Camera _cam;

    private void Awake()
    {
        _cam = Camera.main;
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }
    }

    private void OnEnable()  { PickupItem.OnPickupDelivered += HandleDelivered; }
    private void OnDisable() { PickupItem.OnPickupDelivered -= HandleDelivered; }

    private void HandleDelivered(LootType type, int amount)
    {
        if (target == null) return;

        GameObject go = new GameObject("PickupPopup");
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = string.Format(format, amount, type);
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = GetColor(type);
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableWordWrapping = false;
        go.transform.localScale = Vector3.one * startScale;

        Popup p = new Popup { go = go, tmp = tmp, stack = _active.Count * stackSpacing };
        _active.Add(p);

        Sequence seq = DOTween.Sequence().SetLink(go);
        seq.Append(go.transform.DOScale(1f, popInDuration).SetEase(Ease.OutBack));
        seq.Join(DOTween.To(() => p.rise, v => p.rise = v, floatDistance, duration).SetEase(Ease.OutQuad));
        seq.Insert(duration * fadeStart, tmp.DOFade(0f, duration * (1f - fadeStart)));
        seq.OnComplete(() =>
        {
            _active.Remove(p);
            Destroy(go);
        });
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (_cam == null) _cam = Camera.main;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            Popup p = _active[i];
            if (p.go == null) { _active.RemoveAt(i); continue; }

            // Re-stack smoothly as older popups finish
            p.stack = Mathf.MoveTowards(p.stack, i * stackSpacing, 4f * Time.deltaTime);

            p.go.transform.position = target.position + headOffset + Vector3.up * (p.rise + p.stack);
            if (_cam != null) p.go.transform.rotation = _cam.transform.rotation;
        }
    }

    private Color GetColor(LootType type)
    {
        switch (type)
        {
            case LootType.Gold: return goldColor;
            case LootType.Gear: return gearColor;
            case LootType.Potion: return potionColor;
            default: return Color.white;
        }
    }
}
