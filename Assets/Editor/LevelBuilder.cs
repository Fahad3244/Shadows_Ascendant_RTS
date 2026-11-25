using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static partial class TestArenaBuilder
{
    private const string LevelName = "Prototype_Level";
    private const float LevelHalf = 108f;

    // Main route: camp -> meadow -> gate -> carry -> capture -> miniboss -> boss
    private static readonly Vector2[] Route =
    {
        new Vector2(0f, -100f), new Vector2(0f, -30f), new Vector2(25f, 0f),
        new Vector2(25f, 35f), new Vector2(0f, 65f), new Vector2(0f, 100f)
    };

    private static System.Random _rng;

    [MenuItem("Tools/Test Arena/Build Playable Level")]
    public static void BuildLevel()
    {
        string basePath = FindScenePath("Prototype");
        if (basePath == null) { Debug.LogWarning("[Level] Scene 'Prototype' not found."); return; }
        if (!EditorUtility.DisplayDialog("Playable Level",
            "This (re)creates the scene '" + LevelName + "' as a copy of 'Prototype' and builds the level in it. Continue?", "Build", "Cancel")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string levelPath = Path.GetDirectoryName(basePath).Replace('\\', '/') + "/" + LevelName + ".unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(levelPath) != null) AssetDatabase.DeleteAsset(levelPath);
        AssetDatabase.CopyAsset(basePath, levelPath);
        Scene scene = EditorSceneManager.OpenScene(levelPath);

        Mats.Clear();
        Spawns.Clear();
        _root = null;
        _rng = new System.Random(7);
        EnsureFolder("Assets/TestArena");
        EnsureFolder("Assets/TestArena/Materials");
        EnsureFolder("Assets/TestArena/Settings");

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player != null) _playerY = player.transform.position.y;
        GameObject groundGo = GameObject.Find("World/Ground");
        if (groundGo != null)
        {
            Renderer gr = groundGo.GetComponent<Renderer>();
            if (gr != null)
            {
                _groundY = gr.bounds.max.y;
                Debug.Log("[Level] Ground size: " + gr.bounds.size + " center: " + gr.bounds.center);
            }
        }

        PrepareLevelData(player);

        _root = new GameObject("Level").transform;
        DefinePois();
        BuildEnvironment();
        BuildCamp(player);

        BuildZones();
        BuildLevelHelpers(player);

        BuildDecor();
        GameObject introGo = new GameObject("LevelIntro");
        introGo.transform.SetParent(_root, false);
        introGo.AddComponent<LevelIntro>();

        ArchiveOldContent();
        PrepareScene();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>();
        list.Add(new EditorBuildSettingsScene(levelPath, true));
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            if (s.path != levelPath) list.Add(s);
        EditorBuildSettings.scenes = list.ToArray();

        TryBake();
        EditorUtility.DisplayDialog("Playable Level",
            "Level built. If a NavMesh bake bar appears, wait for it, then press Ctrl+S. Then press Play.", "OK");
    }

    private static string FindScenePath(string sceneName)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == sceneName) return path;
        }
        return null;
    }

    private static void PrepareLevelData(GameObject player)
    {
        _dummy = MakeEnemyData("EnemyData_Dummy", 600f, 0f, 0.5f, 1f, 1.5f, 1f);
        _weak = MakeEnemyData("EnemyData_Weak", 150f, 3f, 8f, 2f, 0.8f, 3.5f);
        _brute = MakeEnemyData("EnemyData_Brute", 500f, 30f, 8f, 2f, 0.7f, 3.5f);
        _killer = MakeEnemyData("EnemyData_Killer", 100f, 200f, 10f, 1f, 0.3f, 3.5f);
        _tank = MakeEnemyData("EnemyData_Tank", 5000f, 0f, 0.5f, 0f, 1.5f, 1f);
        _hunter = MakeEnemyData("EnemyData_Hunter", 800f, 10f, 10f, 1f, 1.2f, 3.5f);
        _mini = MakeEnemyData("EnemyData_Miniboss", 1500f, 40f, 10f, 1f, 1.2f, 3.5f);
        _boss = MakeEnemyData("EnemyData_Boss", 3000f, 60f, 12f, 1f, 1.5f, 3.5f);
        _mini.attackRange = 4f;
        _boss.attackRange = 5f;
        _dummy.passive = true;
        _tank.passive = true;
        foreach (EnemyData d in new[] { _dummy, _tank, _mini, _boss }) EditorUtility.SetDirty(d);

        AttachBySlots.Clear();
        MakeAttach("Attach_Critter", 1);
        AttachBySlots[1] = MakeAttach("Attach_Melee", 1);
        AttachBySlots[2] = MakeAttach("Attach_Ranged", 2);
        MakeAttach("Attach_Medic", 2);
        MakeAttach("Attach_Builder", 3);
        AttachBySlots[3] = MakeAttach("Attach_EnemyDemo", 3);
        AttachBySlots[4] = MakeAttach("Attach_EnemyStandard", 4);
        AttachBySlots[5] = MakeAttach("Attach_Miniboss", 5);
        AttachBySlots[6] = MakeAttach("Attach_Boss", 6);
        _attachPlayer = MakeAttach("Attach_Player", 4);

        if (player != null)
        {
            if (player.GetComponent<PlayerAttachment>() == null) player.AddComponent<PlayerAttachment>();
            Set<PlayerAttachment>(player, "settings", _attachPlayer);
            Set<PlayerHealthHandler>(player, "maxHealth", 250);
        }
        AssetDatabase.SaveAssets();
    }

    // ---------------------------------------------------------------- environment

    private static float R(float a, float b) { return a + (float)_rng.NextDouble() * (b - a); }

    private static float RouteDistance(float x, float z)
    {
        float best = float.MaxValue;
        Vector2 p = new Vector2(x, z);
        for (int i = 0; i < Route.Length - 1; i++)
        {
            Vector2 a = Route[i], b = Route[i + 1];
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            float d = Vector2.Distance(p, a + ab * t);
            if (d < best) best = d;
        }
        return best;
    }

    private static float NodeDistance(float x, float z)
    {
        float best = float.MaxValue;
        foreach (Vector2 n in Route) best = Mathf.Min(best, Vector2.Distance(new Vector2(x, z), n));
        return best;
    }

    private static Transform Zone(string name, float wx, float wz)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(_root, false);
        g.transform.position = new Vector3(wx, 0f, wz);
        return g.transform;
    }

    private static void BuildEnvironment()
    {
        Transform env = new GameObject("Environment").transform;
        env.SetParent(_root, false);
        Transform pathT = new GameObject("Path").transform;
        pathT.SetParent(env, false);
        Transform forest = new GameObject("Forest").transform;
        forest.SetParent(env, false);

        // dirt path
        Material dirt = Mat("Path", new Color(0.55f, 0.42f, 0.28f));
        for (int i = 0; i < Route.Length - 1; i++)
        {
            Vector2 a = Route[i], b = Route[i + 1];
            Vector2 mid = (a + b) * 0.5f;
            float len = Vector2.Distance(a, b);
            float yaw = Mathf.Atan2(b.x - a.x, b.y - a.y) * Mathf.Rad2Deg;
            GameObject s = Prim(PrimitiveType.Cube, "PathSegment", pathT, new Vector3(mid.x, _groundY + 0.04f, mid.y), new Vector3(8f, 0.08f, len), dirt, false);
            s.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
        foreach (Vector2 n in Route)
            Prim(PrimitiveType.Cylinder, "PathNode", pathT, new Vector3(n.x, _groundY + 0.045f, n.y), new Vector3(8f, 0.05f, 8f), dirt, false);
        BuildSpurs(pathT);

        // invisible boundary
        GameObject bounds = new GameObject("Bounds");
        bounds.transform.SetParent(env, false);
        float h = LevelHalf + 4f, len2 = h * 2f;
        AddBound(bounds.transform, new Vector3(0f, 10f, h), new Vector3(len2, 20f, 2f));
        AddBound(bounds.transform, new Vector3(0f, 10f, -h), new Vector3(len2, 20f, 2f));
        AddBound(bounds.transform, new Vector3(h, 10f, 0f), new Vector3(2f, 20f, len2));
        AddBound(bounds.transform, new Vector3(-h, 10f, 0f), new Vector3(2f, 20f, len2));

        // scattered forest (kept clear of the route and the zone centers)
        for (float gx = -LevelHalf; gx <= LevelHalf; gx += 8f)
        {
            for (float gz = -LevelHalf; gz <= LevelHalf; gz += 8f)
            {
                float x = gx + R(-3f, 3f);
                float z = gz + R(-3f, 3f);
                float d = RouteDistance(x, z);
                if (PoiBlocked(x, z)) continue;
                if (d < 12f || NodeDistance(x, z) < 18f) continue;
                if (_rng.NextDouble() > 0.55) continue;
                bool solid = d < 24f;
                double r = _rng.NextDouble();
                if (r < 0.12) Rock(forest, x, z, R(1f, 2.6f));
                else if (r < 0.24) Bush(forest, x, z);
                else Tree(forest, x, z, R(0.9f, 1.5f), r > 0.62, solid);
            }
        }

        // dense border
        for (float i = -LevelHalf; i <= LevelHalf; i += 4f)
        {
            BorderTree(forest, i, LevelHalf);
            BorderTree(forest, i, -LevelHalf);
            BorderTree(forest, LevelHalf, i);
            BorderTree(forest, -LevelHalf, i);
        }
    }

    private static void AddBound(Transform parent, Vector3 pos, Vector3 size)
    {
        GameObject g = new GameObject("Bound");
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.AddComponent<BoxCollider>().size = size;
    }

    private static void BorderTree(Transform parent, float x, float z)
    {
        Tree(parent, x + R(-1.2f, 1.2f), z + R(-1.2f, 1.2f), R(1.2f, 1.7f), _rng.NextDouble() < 0.5, false);
    }

    private static void Tree(Transform parent, float x, float z, float s, bool pine, bool solid)
    {
        GameObject t = new GameObject(pine ? "Pine" : "Tree");
        t.transform.SetParent(parent, false);
        t.transform.localPosition = new Vector3(x, _groundY, z);
        t.transform.localRotation = Quaternion.Euler(0f, R(0f, 360f), 0f);
        float h = 3f * s;
        GameObject trunk = Prim(PrimitiveType.Cube, "Trunk", t.transform, new Vector3(0f, h * 0.5f, 0f), new Vector3(0.9f * s, h, 0.9f * s), Mat("Trunk", new Color(0.4f, 0.26f, 0.14f)), true);
        if (pine)
        {
            Material m = Mat("Pine", new Color(0.12f, 0.42f, 0.2f));
            Prim(PrimitiveType.Cube, "Crown1", t.transform, new Vector3(0f, h + 0.3f * s, 0f), new Vector3(3.2f * s, 1.4f * s, 3.2f * s), m, false);
            Prim(PrimitiveType.Cube, "Crown2", t.transform, new Vector3(0f, h + 1.4f * s, 0f), new Vector3(2.3f * s, 1.3f * s, 2.3f * s), m, false);
            Prim(PrimitiveType.Cube, "Crown3", t.transform, new Vector3(0f, h + 2.4f * s, 0f), new Vector3(1.3f * s, 1.2f * s, 1.3f * s), m, false);
        }
        else
        {
            Prim(PrimitiveType.Cube, "Crown1", t.transform, new Vector3(0f, h + 0.6f * s, 0f), new Vector3(3.8f * s, 1.8f * s, 3.8f * s), Mat("LeafA", new Color(0.25f, 0.62f, 0.2f)), false);
            Prim(PrimitiveType.Cube, "Crown2", t.transform, new Vector3(0f, h + 2f * s, 0f), new Vector3(2.5f * s, 1.5f * s, 2.5f * s), Mat("LeafB", new Color(0.35f, 0.72f, 0.25f)), false);
        }
        if (solid)
        {
            NavMeshObstacle ob = trunk.AddComponent<NavMeshObstacle>();
            ob.shape = NavMeshObstacleShape.Box;
            ob.carving = true;
            ob.center = Vector3.zero;
            ob.size = Vector3.one;
        }
    }

    private static void Rock(Transform parent, float x, float z, float s)
    {
        GameObject r = Prim(PrimitiveType.Cube, "Rock", parent, new Vector3(x, _groundY + s * 0.4f, z), new Vector3(s * 1.3f, s * 0.8f, s), Mat("Rock", new Color(0.5f, 0.5f, 0.55f)), true);
        r.transform.localRotation = Quaternion.Euler(R(-8f, 8f), R(0f, 360f), R(-8f, 8f));
        if (s >= 1.5f && RouteDistance(x, z) < 24f)
        {
            NavMeshObstacle ob = r.AddComponent<NavMeshObstacle>();
            ob.shape = NavMeshObstacleShape.Box;
            ob.carving = true;
            ob.size = Vector3.one;
        }
    }

    private static void Bush(Transform parent, float x, float z)
    {
        float s = R(0.9f, 1.5f);
        Prim(PrimitiveType.Cube, "Bush", parent, new Vector3(x, _groundY + s * 0.4f, z), new Vector3(s * 1.4f, s * 0.8f, s * 1.4f), Mat("Bush", new Color(0.2f, 0.55f, 0.25f)), false);
    }

    // ---------------------------------------------------------------- camp

    private static void BuildCamp(GameObject player)
    {
        Transform c = Zone("Camp", 0f, -95f);

        if (player != null)
            player.transform.SetPositionAndRotation(new Vector3(0f, _playerY, -100f), Quaternion.identity);

        // campfire
        Material rock = Mat("Rock", new Color(0.5f, 0.5f, 0.55f));
        Prim(PrimitiveType.Cylinder, "FireBase", c, new Vector3(8f, _groundY + 0.1f, 4f), new Vector3(1.8f, 0.1f, 1.8f), Mat("Ash", new Color(0.15f, 0.12f, 0.1f)), false);
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            Prim(PrimitiveType.Cube, "FireRock", c, new Vector3(8f + Mathf.Cos(a) * 1.4f, _groundY + 0.2f, 4f + Mathf.Sin(a) * 1.4f), Vector3.one * 0.5f, rock, false);
        }
        GameObject flame = Prim(PrimitiveType.Cube, "Flame", c, new Vector3(8f, _groundY + 0.7f, 4f), new Vector3(0.7f, 1f, 0.7f), Mat("Flame", new Color(1f, 0.55f, 0.1f)), false);
        GameObject lg = new GameObject("FireLight");
        lg.transform.SetParent(flame.transform, false);
        lg.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        Light l = lg.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.6f, 0.25f);
        l.range = 14f;
        l.intensity = 2f;

        Tent(c, -9f, 4f, 15f);
        Tent(c, -12f, 14f, -10f);

        // things to break and collect
        Breakable(c, "Camp_BarrelA", -7f, 18f, false, 80f);
        Breakable(c, "Camp_BarrelB", -9f, 21f, false, 80f);
        Breakable(c, "Camp_BarrelC", 8f, 17f, false, 80f);
        Breakable(c, "Camp_Crate", 9f, 23f, true, 200f, 4);
        PickupItemAt(c, "TestGold", "Camp_GoldA", -6f, 10f, 0);
        PickupItemAt(c, "TestGold", "Camp_GoldB", 6f, 10f, 0);
        PickupItemAt(c, "TestPotion", "Camp_Potion", -6f, 13f, 0);
        PickupItemAt(c, "TestGear", "Camp_Gear", 5f, 24f, 1);

        Sign(c, "1-4 PICK TROOPS    LEFT CLICK SEND    RIGHT CLICK RECALL", -11f, -2f, 3.5f);
        Sign(c, "BREAK THE BARRELS, TAKE THE LOOT, FOLLOW THE PATH", 11f, -2f, 3.5f);
    }

    private static void Tent(Transform parent, float x, float z, float yaw)
    {
        GameObject t = new GameObject("Tent");
        t.transform.SetParent(parent, false);
        t.transform.localPosition = new Vector3(x, _groundY, z);
        t.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        GameObject b = Prim(PrimitiveType.Cube, "Base", t.transform, new Vector3(0f, 1f, 0f), new Vector3(3.2f, 2f, 4f), Mat("TentBase", new Color(0.75f, 0.25f, 0.2f)), true);
        GameObject roof = Prim(PrimitiveType.Cube, "Roof", t.transform, new Vector3(0f, 2f, 0f), new Vector3(2.4f, 2.4f, 4.2f), Mat("TentRoof", new Color(0.9f, 0.85f, 0.7f)), false);
        roof.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        NavMeshObstacle ob = b.AddComponent<NavMeshObstacle>();
        ob.shape = NavMeshObstacleShape.Box;
        ob.carving = true;
        ob.size = Vector3.one;
    }
}
