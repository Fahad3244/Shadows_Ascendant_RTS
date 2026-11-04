using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SceneSwitchMenu
{
    private const string NormalName = "Prototype";
    private const string ArenaName = "Prototype_TestArena";

    [MenuItem("Tools/Test Arena/Open Normal Scene")]
    private static void OpenNormal() { Open(NormalName); }

    [MenuItem("Tools/Test Arena/Open Test Arena Scene")]
    private static void OpenArena() { Open(ArenaName); }

    [MenuItem("Tools/Test Arena/Add Both Scenes To Build Settings")]
    private static void AddBoth()
    {
        string normal = Find(NormalName);
        string arena = Find(ArenaName);
        if (normal == null || arena == null)
        {
            Debug.LogWarning("[SceneSwitchMenu] Could not find both scenes. Check the names at the top of this script.");
            return;
        }
        List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>();
        list.Add(new EditorBuildSettingsScene(normal, true));
        list.Add(new EditorBuildSettingsScene(arena, true));
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            if (s.path != normal && s.path != arena) list.Add(s);
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("[SceneSwitchMenu] Build Settings: 0 = " + NormalName + ", 1 = " + ArenaName);
    }

    private static void Open(string sceneName)
    {
        string path = Find(sceneName);
        if (path == null) { Debug.LogWarning("[SceneSwitchMenu] Scene not found: " + sceneName); return; }
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(path);
    }

    private static string Find(string sceneName)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == sceneName) return path;
        }
        return null;
    }
}
