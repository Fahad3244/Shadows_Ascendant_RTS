using TMPro;
using UnityEngine;

public class ArenaHintDisplay : MonoBehaviour
{
    public Transform player;
    public TextMeshProUGUI label;
    public GameObject panel;

    private void Update()
    {
        if (player == null || panel == null || label == null) return;

        ArenaHintZone best = null;
        float bestD = float.MaxValue;
        foreach (ArenaHintZone z in ArenaHintZone.All)
        {
            if (!z.Contains(player.position)) continue;
            float d = (z.transform.position - player.position).sqrMagnitude;
            if (d < bestD) { bestD = d; best = z; }
        }

        if (best == null)
        {
            if (panel.activeSelf) panel.SetActive(false);
            return;
        }
        if (!panel.activeSelf) panel.SetActive(true);
        label.text = "<b>" + best.title + "</b>\n" + best.body;
    }
}
