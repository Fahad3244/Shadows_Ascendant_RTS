using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArenaHintDisplay : MonoBehaviour
{
    public Transform player;
    public TextMeshProUGUI label;
    public GameObject panel;

    private static readonly Color PanelColor = new Color(0.05f, 0.07f, 0.10f, 0.88f);
    private const float Pad = 16f;
    private string _lastText;

    private void Start()
    {
        StylePanel(panel, label, 700f);

        TestArenaHelper helper = GetComponent<TestArenaHelper>();
        if (helper != null && helper.helpPanel != null)
        {
            TextMeshProUGUI helpText = helper.helpPanel.GetComponentInChildren<TextMeshProUGUI>(true);
            StylePanel(helper.helpPanel, helpText, 480f);
            if (helpText != null)
            {
                helpText.text = HelpText;
                Resize(helper.helpPanel, helpText, 480f);
            }
        }
    }

    private static void StylePanel(GameObject p, TextMeshProUGUI t, float width)
    {
        if (p == null) return;
        Image img = p.GetComponent<Image>();
        if (img != null) img.color = PanelColor;
        RectTransform rt = p.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
        if (t != null)
        {
            t.richText = true;
            t.fontSize = 15f;
            t.color = new Color(0.9f, 0.92f, 0.95f);
            RectTransform tr = t.GetComponent<RectTransform>();
            tr.offsetMin = new Vector2(Pad, Pad);
            tr.offsetMax = new Vector2(-Pad, -Pad);
        }
    }

    private static void Resize(GameObject p, TextMeshProUGUI t, float width)
    {
        float h = t.GetPreferredValues(t.text, width - Pad * 2f, 0f).y + Pad * 2f;
        RectTransform rt = p.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, h);
    }

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

        string body = best.body
            .Replace("EXPECT:", "<b><color=#66ff88>EXPECT:</color></b>")
            .Replace("GOAL:", "<b><color=#ffd966>GOAL:</color></b>")
            .Replace("HOW:", "<b><color=#7fd1ff>HOW:</color></b>")
            .Replace("RESULT:", "<b><color=#66ff88>RESULT:</color></b>")
            .Replace("\n", "\n\n");
        string text = "<size=130%><b><color=#7fd1ff>" + best.title + "</color></b></size>\n\n" + body;
        if (text == _lastText) return;
        _lastText = text;
        label.text = text;
        Resize(panel, label, 700f);
    }

    private const string HelpText =
        "<size=125%><b><color=#7fd1ff>CONTROLS</color></b></size>\n\n" +
        "<b><color=#ffd966>MOVE</color></b>\n" +
        "<color=#9ad>WASD</color> move\n" +
        "<color=#9ad>Hold RIGHT mouse + move</color> rotate camera\n" +
        "<color=#9ad>Esc</color> free the mouse\n\n" +
        "<b><color=#ffd966>TROOP GROUP</color></b>\n" +
        "<color=#9ad>`</color> All   <color=#9ad>1</color> Builder   <color=#9ad>2</color> Ranged   <color=#9ad>3</color> Melee   <color=#9ad>4</color> Medic\n\n" +
        "<b><color=#ffd966>COMMANDS</color></b>\n" +
        "<color=#9ad>LEFT click</color> tap = send 1, hold = send group\n" +
        "<color=#9ad>RIGHT click</color> tap = recall 1, hold = recall group\n" +
        "<color=#9ad>LEFT + RIGHT</color> sweep (steer troops)\n" +
        "<color=#9ad>Z</color> target nearest / next   <color=#9ad>X</color> clear\n" +
        "<color=#9ad>Q</color> guard flag\n" +
        "<color=#9ad>Ctrl</color> hold = plan route, release = save, tap = run\n\n" +
        "<b><color=#ffd966>READING THE WORLD</color></b>\n" +
        "Number on an enemy = free attach points\n" +
        "Number on a movable = troops still needed\n" +
        "Waypoint ring: <color=#6fa8ff>blue</color> yours, <color=#ff6666>red</color> enemy\n\n" +
        "<b><color=#ffd966>TEST KEYS</color></b>\n" +
        "<color=#9ad>G</color> debug panel   <color=#9ad>Y</color> 6 enemies attack you\n" +
        "<color=#9ad>N</color> stun enemies   <color=#9ad>U</color> human target aware / unaware\n" +
        "<color=#9ad>E</color> wake enemy   <color=#9ad>M</color> killer   <color=#9ad>B</color> open walls   <color=#9ad>K / L</color> flip waypoint\n\n" +
        "<color=#9ad>Tab</color> station list (level only): <color=#9ad>[ ]</color> choose, <color=#9ad>Enter</color> go\n" +
        "<color=#9ad>F1</color> Hub   <color=#9ad>F2-F9</color> stations   <color=#9ad>H</color> hide   <color=#9ad>F12</color> reset";
}
