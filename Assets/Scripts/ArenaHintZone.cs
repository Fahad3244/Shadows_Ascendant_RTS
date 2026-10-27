using System.Collections.Generic;
using UnityEngine;

public class ArenaHintZone : MonoBehaviour
{
    public static readonly List<ArenaHintZone> All = new List<ArenaHintZone>();

    public string title;
    [TextArea(3, 8)] public string body;
    public float radius = 3.5f;

    private void OnEnable() { All.Add(this); }
    private void OnDisable() { All.Remove(this); }

    public bool Contains(Vector3 p)
    {
        Vector3 d = p - transform.position;
        d.y = 0f;
        return d.magnitude <= radius;
    }
}
