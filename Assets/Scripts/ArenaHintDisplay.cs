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

    private class Anim
    {
        public CanvasGroup cg;
        public RectTransform rt;
        public Vector2 basePos;
        public Vector2 offset;
        public float vis;
        public bool open;

        public void Init(GameObject g, Vector2 off)
        {
            cg = g.GetComponent<CanvasGroup>();
            if (cg == null) cg = g.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
            rt = g.GetComponent<RectTransform>();
            basePos = rt.anchoredPosition;
            offset = off;
            vis = 0f;
            open = false;
            if (!g.activeSelf) g.SetActive(true);
            Apply();
        }

        public void Tick(float dt)
        {
            if (cg == null) return;
            vis = Mathf.MoveTowards(vis, open ? 1f : 0f, dt / 0.3f);
            Apply();
        }

        private void Apply()
        {
            float e = open ? UiEase.OutBack(vis) : UiEase.Smooth(vis);
            cg.alpha = UiEase.Smooth(vis);
            rt.anchoredPosition = basePos + offset * (1f - e);
            rt.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, e);
        }
    }

    private Anim _hint = new Anim();
    private Anim _help = new Anim();
    private float _startTime;
    private GUIStyle _chip;

    private void Start()
    {
        _startTime = Time.time;
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
            _help.Init(helper.helpPanel, new Vector2(70f, 0f));
        }
        if (panel != null) _hint.Init(panel, new Vector2(0f, 40f));
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
        if (Input.GetKeyDown(KeyCode.H)) _help.open = !_help.open;
        _help.Tick(Time.deltaTime);
        _hint.Tick(Time.deltaTime);

        if (player == null || panel == null || label == null) return;

        ArenaHintZone best = null;
        float bestD = float.MaxValue;
        foreach (ArenaHintZone z in ArenaHintZone.All)
        {
            if (!z.Contains(player.position)) continue;
            float d = (z.transform.position - player.position).sqrMagnitude;
            if (d < bestD) { bestD = d; best = z; }
        }

        _hint.open = best != null;
        if (best == null) return;

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

    // Corner reminder while the controls panel is closed
    private void OnGUI()
    {
        if (_help.open && _help.vis > 0.95f) return;
        if (_chip == null)
        {
            _chip = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true, alignment = TextAnchor.MiddleCenter };
            _chip.normal.textColor = Color.white;
        }
        float age = Time.time - _startTime;
        bool big = age < 12f;
        string t = big ? "Press <b><color=#ffd966>H</color></b> to open the controls" : "<b><color=#ffd966>H</color></b>  Controls";
        float w = big ? 290f : 120f;
        float h = 34f;

        float slideIn = UiEase.OutBack((age - 1f) / 0.5f);
        float hide = 1f - _help.vis;
        float x = Screen.width - w - 12f + (1f - slideIn) * (w + 20f);
        float pulse = big ? 1f + 0.03f * Mathf.Sin(Time.time * 4f) : 1f;

        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(slideIn) * hide);
        Matrix4x4 m = GUI.matrix;
        Rect r = new Rect(x, 12f, w, h);
        GUIUtility.ScaleAroundPivot(Vector2.one * pulse, r.center);
        GUI.DrawTexture(r, UiEase.Solid(new Color(0.05f, 0.07f, 0.10f, 0.9f)));
        GUI.Label(r, t, _chip);
        GUI.matrix = m;
        GUI.color = old;
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
        "<color=#9ad>R</color> roar (Turret Mode only)\n" +
        "<color=#9ad>Ctrl</color> hold = plan route, release = save, tap = run\n\n" +
        "<b><color=#ffd966>READING THE WORLD</color></b>\n" +
        "Number on an enemy = free attach points\n" +
        "Number on a movable = troops still needed\n" +
        "Waypoint ring: <color=#6fa8ff>blue</color> yours, <color=#ff6666>red</color> enemy\n\n" +
        "<b><color=#ffd966>TEST KEYS</color></b>\n" +
        "<color=#9ad>C</color> conceal / reveal Melee troops (test)\n" +
        "<color=#9ad>G</color> debug panel   <color=#9ad>Y</color> 6 enemies attack you\n" +
        "<color=#9ad>N</color> stun enemies   <color=#9ad>U</color> human target aware / unaware\n" +
        "<color=#9ad>E</color> wake enemy   <color=#9ad>M</color> killer   <color=#9ad>B</color> open walls   <color=#9ad>K / L</color> flip waypoint\n\n" +
        "<color=#9ad>Tab</color> station list (level only): <color=#9ad>[ ]</color> choose, <color=#9ad>Enter</color> go\n" +
        "<color=#9ad>F1</color> Hub   <color=#9ad>F2-F9</color> stations   <color=#9ad>Shift+P</color> level / test arena   <color=#9ad>H</color> hide   <color=#9ad>F12</color> reset";
}
