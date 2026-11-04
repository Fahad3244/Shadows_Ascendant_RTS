using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    // Change these if your scene names are different
    private const string ArenaSceneName = "Prototype_TestArena";
    private const string NormalSceneName = ""; // empty = first other scene in Build Settings
    private static readonly KeyCode SwitchKey = KeyCode.F10;
    private static readonly bool ShowHint = true;

    private static SceneSwitcher _instance;
    private GUIStyle _style;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;
        GameObject go = new GameObject("SceneSwitcher");
        _instance = go.AddComponent<SceneSwitcher>();
        DontDestroyOnLoad(go);
    }

    private void Update()
    {
        if (Input.GetKeyDown(SwitchKey)) Switch();
    }

    private void Switch()
    {
        bool inArena = SceneManager.GetActiveScene().name == ArenaSceneName;
        string target = inArena ? FindNormalScene() : ArenaSceneName;
        if (string.IsNullOrEmpty(target) || !InBuild(target))
        {
            Debug.LogWarning("[SceneSwitcher] Scene not in Build Settings: " + target);
            return;
        }
        SceneManager.LoadScene(target);
    }

    private static string FindNormalScene()
    {
        if (!string.IsNullOrEmpty(NormalSceneName)) return NormalSceneName;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string n = SceneNameAt(i);
            if (n != ArenaSceneName) return n;
        }
        return null;
    }

    private static bool InBuild(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            if (SceneNameAt(i) == sceneName) return true;
        return false;
    }

    private static string SceneNameAt(int i)
    {
        return Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
    }

    private void OnGUI()
    {
        if (!ShowHint) return;
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            _style.normal.textColor = Color.white;
        }
        string next = SceneManager.GetActiveScene().name == ArenaSceneName ? "Normal scene" : "Test Arena";
        GUI.Label(new Rect(10, Screen.height - 28, 400, 24), "F10: switch to " + next, _style);
    }
}
