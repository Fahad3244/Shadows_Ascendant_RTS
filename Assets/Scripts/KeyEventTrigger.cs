using UnityEngine;

public class KeyEventTrigger : MonoBehaviour
{
    public KeyCode key = KeyCode.K;
    public GameObject[] toggleObjects;
    public Waypoint[] flipWaypoints;

    private void Update()
    {
        if (!Input.GetKeyDown(key)) return;
        foreach (GameObject go in toggleObjects) if (go != null) go.SetActive(!go.activeSelf);
        foreach (Waypoint w in flipWaypoints) if (w != null) w.FlipPlayerEnemy();
    }
}
