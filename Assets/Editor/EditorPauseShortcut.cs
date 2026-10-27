
using UnityEditor;
using UnityEngine;
using UnityEditor.ShortcutManagement;

public static class EditorPauseShortcut
{
    [Shortcut("Tools/Toggle Play Mode Pause", KeyCode.F8)]
    private static void TogglePause()
    {
        if (!EditorApplication.isPlaying)
            return;

        EditorApplication.isPaused =
            !EditorApplication.isPaused;
    }
}
