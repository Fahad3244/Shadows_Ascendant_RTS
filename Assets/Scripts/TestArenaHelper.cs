using UnityEngine;
using UnityEngine.SceneManagement;

public class TestArenaHelper : MonoBehaviour
{
    public Transform player;
    public SwarmController swarm;
    public Transform[] spawns;
    public GameObject helpPanel;
    public string[] spawnNames;   // level only: names for the Tab menu

    private bool _menu;
    private int _sel;
    private GUIStyle _style;
    private Texture2D _bg;

    private bool HasMenu
    {
        get { return spawnNames != null && spawns != null && spawns.Length > 0 && spawnNames.Length == spawns.Length; }
    }

    private void Update()
    {
        for (int i = 0; i < spawns.Length && i < 9; i++)
            if (Input.GetKeyDown(KeyCode.F1 + i)) Teleport(spawns[i]);

        if (helpPanel != null && Input.GetKeyDown(KeyCode.H))
            helpPanel.SetActive(!helpPanel.activeSelf);

        if (Input.GetKeyDown(KeyCode.F12))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        if (Input.GetKeyDown(KeyCode.N))
            foreach (EnemyBase e in FindObjectsOfType<EnemyBase>()) e.Stun(5f);

        HandleMenu();
    }

    private void HandleMenu()
    {
        if (!HasMenu) return;
        if (Input.GetKeyDown(KeyCode.Tab)) _menu = !_menu;
        if (!_menu) return;
        if (Input.GetKeyDown(KeyCode.LeftBracket)) _sel = (_sel - 1 + spawns.Length) % spawns.Length;
        if (Input.GetKeyDown(KeyCode.RightBracket)) _sel = (_sel + 1) % spawns.Length;
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Teleport(spawns[_sel]);
            _menu = false;
        }
    }

    private void OnGUI()
    {
        if (!_menu || !HasMenu) return;
        if (_style == null)
        {
            _bg = new Texture2D(1, 1);
            _bg.SetPixel(0, 0, new Color(0.05f, 0.07f, 0.10f, 0.92f));
            _bg.Apply();
            _style = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true };
            _style.normal.textColor = new Color(0.9f, 0.92f, 0.95f);
        }
        string t = "<b><color=#7fd1ff>STATIONS</color></b>   <color=#888888>[ ] choose   Enter go   Tab close</color>\n\n";
        for (int i = 0; i < spawnNames.Length; i++)
        {
            string key = i < 9 ? "F" + (i + 1) : "  ";
            string line = key + "   " + spawnNames[i];
            t += i == _sel ? "<color=#ffd966><b>> " + line + "</b></color>\n" : "   " + line + "\n";
        }
        float w = 380f;
        float h = _style.CalcHeight(new GUIContent(t), w - 24f) + 20f;
        Rect r = new Rect(10f, Screen.height * 0.5f - h * 0.5f, w, h);
        GUI.DrawTexture(r, _bg);
        GUI.Label(new Rect(r.x + 12f, r.y + 10f, w - 24f, h - 20f), t, _style);
    }

    private void Teleport(Transform spawn)
    {
        if (player == null || spawn == null) return;
        if (swarm != null) swarm.DespawnAllImmediate();
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.SetPositionAndRotation(spawn.position, spawn.rotation);
        if (cc != null) cc.enabled = true;
    }
}
