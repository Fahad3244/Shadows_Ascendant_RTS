using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
	private const string LevelSceneName = "Prototype_Level";
	private const string ArenaSceneName = "Prototype_TestArena";

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

	private static string TargetScene()
	{
		string cur = SceneManager.GetActiveScene().name;
		if (cur == LevelSceneName) return ArenaSceneName;
		if (cur == ArenaSceneName) return LevelSceneName;
		return null;
	}

	private void Update()
	{
		bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
		if (!shift || !Input.GetKeyDown(KeyCode.P)) return;
		string target = TargetScene();
		if (target == null) return;
		if (!Application.CanStreamedLevelBeLoaded(target))
		{
			Debug.LogWarning("[SceneSwitcher] Scene not in Build Settings: " + target);
			return;
		}
		SceneManager.LoadScene(target);
	}

	private void OnGUI()
	{
		string target = TargetScene();
		if (target == null) return;
		if (_style == null)
		{
			_style = new GUIStyle(GUI.skin.label) { fontSize = 14 };
			_style.normal.textColor = Color.white;
		}
		string next = target == ArenaSceneName ? "Test Arena" : "Playable Level";
		GUI.Label(new Rect(10, Screen.height - 28, 400, 24), "Shift+P: switch to " + next, _style);
	}
}
