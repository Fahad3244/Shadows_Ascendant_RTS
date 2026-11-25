using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public static partial class TestArenaBuilder
{
    private static void BuildDecor()
    {
        Transform d = new GameObject("Decor").transform;
        d.SetParent(_root, false);
        BuildPond(d, -32f, -42f, 7f);
        BuildWatchtower(d, -22f, 8f);
        BuildGiantTree(d, 38f, -38f);
        PlaceLanterns(d);
        ScatterFlowers(d);
        ScatterLogsAndCairns(d);
    }

    private static void AddSolid(Transform parent, Vector3 pos, Vector3 size)
    {
        GameObject g = new GameObject("Solid");
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.AddComponent<BoxCollider>().size = size;
        NavMeshObstacle ob = g.AddComponent<NavMeshObstacle>();
        ob.shape = NavMeshObstacleShape.Box;
        ob.carving = true;
        ob.size = size;
    }

    private static bool FreeSpot(float x, float z, float minNode)
    {
        return Mathf.Abs(x) < 98f && Mathf.Abs(z) < 98f && !PoiBlocked(x, z) && NodeDistance(x, z) > minNode && RouteDistance(x, z) > 5f;
    }

    private static void BuildPond(Transform d, float x, float z, float r)
    {
        Transform forest = _root.Find("Environment/Forest");
        if (forest != null)
        {
            for (int i = forest.childCount - 1; i >= 0; i--)
            {
                Vector3 p = forest.GetChild(i).position;
                if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(x, z)) < r + 4f)
                    UnityEngine.Object.DestroyImmediate(forest.GetChild(i).gameObject);
            }
        }
        Transform pond = new GameObject("Pond").transform;
        pond.SetParent(d, false);
        pond.position = new Vector3(x, 0f, z);
        Prim(PrimitiveType.Cylinder, "Shore", pond, new Vector3(0f, _groundY + 0.04f, 0f), new Vector3(r * 2f + 3f, 0.04f, r * 2f + 3f), Mat("Sand", new Color(0.8f, 0.72f, 0.5f)), false);
        Prim(PrimitiveType.Cylinder, "Water", pond, new Vector3(0f, _groundY + 0.09f, 0f), new Vector3(r * 2f, 0.04f, r * 2f), Mat("Water", new Color(0.2f, 0.5f, 0.85f)), false);
        AddSolid(pond, new Vector3(0f, 1f, 0f), new Vector3(r * 1.6f, 2f, r * 1.6f));
        for (int i = 0; i < 7; i++)
        {
            float a = R(0f, 6.28f), rr = R(0f, r * 0.7f);
            Prim(PrimitiveType.Cylinder, "LilyPad", pond, new Vector3(Mathf.Cos(a) * rr, _groundY + 0.12f, Mathf.Sin(a) * rr), new Vector3(1f, 0.02f, 1f), Mat("Lily", new Color(0.2f, 0.65f, 0.3f)), false);
        }
        for (int i = 0; i < 26; i++)
        {
            float a = R(0f, 6.28f), rr = r + R(0.2f, 1.2f);
            float h = R(1.2f, 2.4f);
            Prim(PrimitiveType.Cube, "Reed", pond, new Vector3(Mathf.Cos(a) * rr, _groundY + h * 0.5f, Mathf.Sin(a) * rr), new Vector3(0.12f, h, 0.12f), Mat("Reed", new Color(0.35f, 0.55f, 0.2f)), false);
        }
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            Rock(pond, Mathf.Cos(a) * (r + 1.8f), Mathf.Sin(a) * (r + 1.8f), R(0.8f, 1.6f));
        }
    }

    private static void BuildWatchtower(Transform d, float x, float z)
    {
        Transform t = new GameObject("Watchtower").transform;
        t.SetParent(d, false);
        t.position = new Vector3(x, 0f, z);
        Material wood = Mat("FenceWood", new Color(0.55f, 0.36f, 0.18f));
        Material dark = Mat("GateWood", new Color(0.35f, 0.2f, 0.1f));
        foreach (Vector2 l in new[] { new Vector2(-2f, -2f), new Vector2(2f, -2f), new Vector2(-2f, 2f), new Vector2(2f, 2f) })
            Prim(PrimitiveType.Cube, "Leg", t, new Vector3(l.x, _groundY + 3.5f, l.y), new Vector3(0.6f, 7f, 0.6f), wood, true);
        Prim(PrimitiveType.Cube, "BraceA", t, new Vector3(0f, _groundY + 2.5f, -2f), new Vector3(4f, 0.25f, 0.25f), wood, false);
        Prim(PrimitiveType.Cube, "BraceB", t, new Vector3(0f, _groundY + 2.5f, 2f), new Vector3(4f, 0.25f, 0.25f), wood, false);
        Prim(PrimitiveType.Cube, "Platform", t, new Vector3(0f, _groundY + 7.1f, 0f), new Vector3(5.2f, 0.3f, 5.2f), dark, false);
        foreach (Vector2 r in new[] { new Vector2(0f, -2.5f), new Vector2(0f, 2.5f) })
            Prim(PrimitiveType.Cube, "Rail", t, new Vector3(r.x, _groundY + 7.9f, r.y), new Vector3(5.2f, 0.9f, 0.15f), wood, false);
        foreach (Vector2 r in new[] { new Vector2(-2.5f, 0f), new Vector2(2.5f, 0f) })
            Prim(PrimitiveType.Cube, "Rail", t, new Vector3(r.x, _groundY + 7.9f, r.y), new Vector3(0.15f, 0.9f, 5.2f), wood, false);
        GameObject roof = Prim(PrimitiveType.Cube, "Roof", t, new Vector3(0f, _groundY + 9.4f, 0f), new Vector3(4.6f, 0.4f, 4.6f), Mat("TentBase", new Color(0.75f, 0.25f, 0.2f)), false);
        roof.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        Prim(PrimitiveType.Cube, "FlagPole", t, new Vector3(0f, _groundY + 10.8f, 0f), new Vector3(0.15f, 2.6f, 0.15f), wood, false);
        Prim(PrimitiveType.Cube, "Flag", t, new Vector3(0.8f, _groundY + 11.7f, 0f), new Vector3(1.4f, 0.8f, 0.08f), Mat("RallyFlag", new Color(0.2f, 0.5f, 1f)), false);
        Prim(PrimitiveType.Cube, "Ladder", t, new Vector3(0f, _groundY + 3.5f, -2.35f), new Vector3(1f, 7f, 0.1f), dark, false);
        AddSolid(t, new Vector3(0f, 2f, 0f), new Vector3(4.6f, 4f, 4.6f));
        StationSign(t, 0f, -6f, "OLD LOOKOUT");
    }

    private static void BuildGiantTree(Transform d, float x, float z)
    {
        Tree(d, x, z, 3.2f, false, true);
        for (int i = 0; i < 5; i++)
        {
            float a = i * 72f * Mathf.Deg2Rad + 0.4f;
            Rock(d, x + Mathf.Cos(a) * 5.5f, z + Mathf.Sin(a) * 5.5f, R(0.8f, 1.5f));
        }
    }

    private static void PlaceLanterns(Transform d)
    {
        Material wood = Mat("FenceWood", new Color(0.55f, 0.36f, 0.18f));
        Material glow = Mat("Lantern", new Color(1f, 0.85f, 0.4f));
        for (int s = 0; s < Route.Length - 1; s++)
        {
            Vector2 a = Route[s], b = Route[s + 1];
            Vector2 dir = (b - a).normalized, n = new Vector2(dir.y, -dir.x);
            float len = Vector2.Distance(a, b);
            for (float t = 12f; t < len - 8f; t += 18f)
            {
                Vector2 p = a + dir * t + n * 5.2f;
                if (!FreeSpot(p.x, p.y, 10f)) continue;
                Prim(PrimitiveType.Cube, "LanternPost", d, new Vector3(p.x, _groundY + 1.4f, p.y), new Vector3(0.2f, 2.8f, 0.2f), wood, false);
                Prim(PrimitiveType.Cube, "Lantern", d, new Vector3(p.x, _groundY + 3f, p.y), new Vector3(0.55f, 0.7f, 0.55f), glow, false);
            }
        }
    }

    private static void ScatterFlowers(Transform d)
    {
        Color[] cols = { new Color(1f, 0.5f, 0.7f), new Color(1f, 0.9f, 0.3f), new Color(0.95f, 0.95f, 1f), new Color(0.7f, 0.5f, 1f) };
        Material stem = Mat("Stem", new Color(0.25f, 0.6f, 0.25f));
        for (int i = 0; i < 320; i++)
        {
            int seg = _rng.Next(Route.Length - 1);
            Vector2 a = Route[seg], b = Route[seg + 1];
            Vector2 dir = (b - a).normalized, n = new Vector2(-dir.y, dir.x);
            Vector2 p = Vector2.Lerp(a, b, R(0f, 1f)) + n * R(5f, 11f) * (_rng.Next(2) == 0 ? 1f : -1f);
            if (!FreeSpot(p.x, p.y, 14f)) continue;
            int c = _rng.Next(cols.Length);
            Material head = Mat("Flower" + c, cols[c]);
            for (int k = 0; k < 3; k++)
            {
                Vector3 pos = new Vector3(p.x + R(-0.8f, 0.8f), _groundY, p.y + R(-0.8f, 0.8f));
                Prim(PrimitiveType.Cube, "Stem", d, pos + Vector3.up * 0.25f, new Vector3(0.07f, 0.5f, 0.07f), stem, false);
                Prim(PrimitiveType.Cube, "Bloom", d, pos + Vector3.up * 0.55f, new Vector3(0.28f, 0.18f, 0.28f), head, false);
            }
        }
    }

    private static void ScatterLogsAndCairns(Transform d)
    {
        Material bark = Mat("Bark", new Color(0.38f, 0.25f, 0.14f));
        Material stone = Mat("Rock", new Color(0.5f, 0.5f, 0.55f));
        for (int i = 0; i < 40; i++)
        {
            int seg = _rng.Next(Route.Length - 1);
            Vector2 a = Route[seg], b = Route[seg + 1];
            Vector2 dir = (b - a).normalized, n = new Vector2(-dir.y, dir.x);
            Vector2 p = Vector2.Lerp(a, b, R(0f, 1f)) + n * R(6.5f, 10f) * (_rng.Next(2) == 0 ? 1f : -1f);
            if (!FreeSpot(p.x, p.y, 16f)) continue;
            if (_rng.Next(2) == 0)
            {
                GameObject log = Prim(PrimitiveType.Cube, "FallenLog", d, new Vector3(p.x, _groundY + 0.4f, p.y), new Vector3(0.8f, 0.8f, 3.6f), bark, true);
                log.transform.localRotation = Quaternion.Euler(0f, R(0f, 360f), 0f);
            }
            else
            {
                for (int k = 0; k < 3; k++)
                {
                    float s = 1.2f - k * 0.3f;
                    Prim(PrimitiveType.Cube, "Cairn", d, new Vector3(p.x, _groundY + 0.25f + k * 0.45f, p.y), new Vector3(s, 0.45f, s), stone, false);
                }
            }
        }
    }
}
