using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public static partial class TestArenaBuilder
{
    // ---------------------------------------------------------------- points of interest

    private struct Poi { public string name; public Vector2 center; public float radius; public Vector2 junction; }
    private static readonly List<Poi> Pois = new List<Poi>();
    private static readonly List<string> SpawnNames = new List<string>();

    private static void AddPoi(string name, float cx, float cz, float r, float jx, float jz)
    {
        Pois.Add(new Poi { name = name, center = new Vector2(cx, cz), radius = r, junction = new Vector2(jx, jz) });
    }

    // ADD A NEW CLEARING: one AddPoi line here + one BuildXxx() method + one line in BuildZones().
    // junction = the point on the main path where the trail starts.
    private static void DefinePois()
    {
        SpawnNames.Clear();
        Pois.Clear();
        AddPoi("GoldHollow", -58f, -64f, 16f, 0f, -66f);
        AddPoi("StashGlade", 58f, -64f, 16f, 0f, -54f);
        AddPoi("SentryGlade", 62f, -10f, 16f, 18f, -8f);
        AddPoi("Roadblock", -58f, -12f, 16f, 12f, -15f);
        AddPoi("CarryYard", -45f, 38f, 17f, 22.5f, 38f);
        AddPoi("Outpost", 62f, 45f, 30f, 21f, 40f);
        AddPoi("Ruins", -78f, 80f, 22f, 10f, 53f);
        AddPoi("BossArena", 0f, 88f, 17f, 0f, 68f);
    }

    private static Poi GetPoi(string name)
    {
        foreach (Poi p in Pois) if (p.name == name) return p;
        Debug.LogWarning("[Level] Unknown POI: " + name);
        return default(Poi);
    }

    private static Transform PoiZone(string name)
    {
        Poi p = GetPoi(name);
        return Zone(name, p.center.x, p.center.y);
    }

    private static bool PoiBlocked(float x, float z)
    {
        Vector2 q = new Vector2(x, z);
        foreach (Poi p in Pois)
        {
            if (Vector2.Distance(q, p.center) < p.radius + 5f) return true;
            Vector2 a = p.junction, ab = p.center - p.junction;
            float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude);
            if (Vector2.Distance(q, a + ab * t) < 7f) return true;
        }
        return false;
    }

    private static void BuildSpurs(Transform pathT)
    {
        Material dirt = Mat("Path", new Color(0.55f, 0.42f, 0.28f));
        Material clearing = Mat("Clearing", new Color(0.5f, 0.45f, 0.3f));
        foreach (Poi p in Pois)
        {
            Vector2 a = p.junction, b = p.center;
            Vector2 mid = (a + b) * 0.5f;
            float len = Vector2.Distance(a, b);
            float yaw = Mathf.Atan2(b.x - a.x, b.y - a.y) * Mathf.Rad2Deg;
            GameObject s = Prim(PrimitiveType.Cube, "Trail_" + p.name, pathT, new Vector3(mid.x, _groundY + 0.05f, mid.y), new Vector3(5f, 0.08f, len), dirt, false);
            s.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            float d = p.radius * 1.7f;
            Prim(PrimitiveType.Cylinder, "Clearing_" + p.name, pathT, new Vector3(b.x, _groundY + 0.05f, b.y), new Vector3(d, 0.05f, d), clearing, false);
        }
    }

    // ---------------------------------------------------------------- zones

    // ADD NEW FEATURE ZONES HERE
    private static void BuildZones()
    {
        GetMessenger();
        CampHints();          // F1
        BuildGuardPost();     // F2
        BuildGoldHollow();    // F3
        BuildStashGlade();    // F4
        BuildSentryGlade();   // F5
        BuildRoadblock();     // F6
        BuildGate();          // F7
        BuildCarryYard();     // F8
        BuildOutpost();       // F9
        BuildRuins();         // Tab menu only
        BuildBossArena();     // Tab menu only
        // Step 21: BuildBossArena();
        BuildRallyPoints();
    }

    // ---------------------------------------------------------------- helpers

    private static void StationSign(Transform zone, float x, float z, string text, float yaw = 0f)
    {
        GameObject s = new GameObject("StationSign");
        s.transform.SetParent(zone, false);
        s.transform.localPosition = new Vector3(x, _groundY, z);
        s.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Material wood = Mat("FenceWood", new Color(0.55f, 0.36f, 0.18f));
        Prim(PrimitiveType.Cube, "Post", s.transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.25f, 2.4f, 0.25f), wood, false);
        Prim(PrimitiveType.Cube, "Board", s.transform, new Vector3(0f, 2.7f, 0f), new Vector3(5.2f, 1.1f, 0.15f), Mat("Board", new Color(0.07f, 0.09f, 0.13f)), false);
        GameObject t = new GameObject("Text", typeof(RectTransform), typeof(TextMeshPro));
        t.transform.SetParent(s.transform, false);
        t.transform.localPosition = new Vector3(0f, 2.7f, -0.12f);
        t.GetComponent<RectTransform>().sizeDelta = new Vector2(5f, 1f);
        TextMeshPro tmp = t.GetComponent<TextMeshPro>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 1f;
        tmp.fontSizeMax = 6f;
        tmp.color = Color.white;
    }

    // Signpost on the main path pointing to a trail
    private static void JunctionSign(string poiName, string label)
    {
        Poi p = GetPoi(poiName);
        bool west = p.center.x < p.junction.x;
        StationSign(_root, p.junction.x + (west ? -7f : 7f), p.junction.y, west ? "<<< " + label : label + " >>>");
    }

    private static GameObject FenceSegment(Transform parent, string name, float x1, float z1, float x2, float z2)
    {
        Vector2 a = new Vector2(x1, z1), b = new Vector2(x2, z2);
        float len = Vector2.Distance(a, b);
        Vector2 mid = (a + b) * 0.5f;
        float yaw = Mathf.Atan2(b.x - a.x, b.y - a.y) * Mathf.Rad2Deg;

        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(mid.x, _groundY, mid.y);
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        Material wood = Mat("FenceWood", new Color(0.55f, 0.36f, 0.18f));
        int posts = Mathf.Max(2, Mathf.CeilToInt(len / 2f) + 1);
        for (int i = 0; i < posts; i++)
        {
            float lz = Mathf.Lerp(-len * 0.5f, len * 0.5f, (float)i / (posts - 1));
            Prim(PrimitiveType.Cube, "Post", root.transform, new Vector3(0f, 0.8f, lz), new Vector3(0.3f, 1.6f, 0.3f), wood, false);
        }
        Prim(PrimitiveType.Cube, "RailTop", root.transform, new Vector3(0f, 1.3f, 0f), new Vector3(0.12f, 0.14f, len), wood, false);
        Prim(PrimitiveType.Cube, "RailLow", root.transform, new Vector3(0f, 0.65f, 0f), new Vector3(0.12f, 0.14f, len), wood, false);

        GameObject blk = new GameObject("Blocker");
        blk.transform.SetParent(root.transform, false);
        blk.transform.localPosition = new Vector3(0f, 1f, 0f);
        blk.AddComponent<BoxCollider>().size = new Vector3(0.8f, 2f, len);
        NavMeshObstacle ob = blk.AddComponent<NavMeshObstacle>();
        ob.shape = NavMeshObstacleShape.Box;
        ob.carving = true;
        ob.center = Vector3.zero;
        ob.size = new Vector3(0.8f, 2f, len);
        return root;
    }

    private static void OpenOnBreak(GameObject breakable, GameObject target)
    {
        if (breakable == null || target == null) return;
        BreakableObject bo = breakable.GetComponent<BreakableObject>();
        FieldInfo f = typeof(BreakableObject).GetField("onBroken", BindingFlags.NonPublic | BindingFlags.Instance);
        UnityEngine.Events.UnityEvent ev = f != null ? f.GetValue(bo) as UnityEngine.Events.UnityEvent : null;
        if (ev == null) { Debug.LogWarning("[Level] Could not hook onBroken for " + breakable.name); return; }
        UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(ev, target.SetActive, false);
        EditorUtility.SetDirty(bo);
    }

    private static void HookDeactivate(Component src, string field, GameObject target)
    {
        if (src == null || target == null) return;
        FieldInfo f = src.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
        UnityEngine.Events.UnityEvent ev = f != null ? f.GetValue(src) as UnityEngine.Events.UnityEvent : null;
        if (ev == null) { Debug.LogWarning("[Level] Could not hook " + field + " on " + src.name); return; }
        UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(ev, target.SetActive, false);
        EditorUtility.SetDirty(src);
    }

    private static LevelMessenger GetMessenger()
    {
        LevelMessenger m = UnityEngine.Object.FindObjectOfType<LevelMessenger>();
        if (m == null)
        {
            GameObject g = new GameObject("LevelMessenger");
            g.transform.SetParent(_root, false);
            m = g.AddComponent<LevelMessenger>();
        }
        return m;
    }

    private static UnityEngine.Events.UnityEvent FindEvent(Component src, string field)
    {
        if (src == null) return null;
        FieldInfo f = src.GetType().GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        UnityEngine.Events.UnityEvent ev = f != null ? f.GetValue(src) as UnityEngine.Events.UnityEvent : null;
        if (ev == null) Debug.LogWarning("[Level] Could not hook " + field + " on " + src.name);
        return ev;
    }

    // Show an on-screen banner when the event fires
    private static void HookSay(Component src, string field, string message)
    {
        UnityEngine.Events.UnityEvent ev = FindEvent(src, field);
        if (ev == null) return;
        UnityEditor.Events.UnityEventTools.AddStringPersistentListener(ev, GetMessenger().Say, message);
        EditorUtility.SetDirty(src);
    }

    private static void HookSetActive(Component src, string field, GameObject target, bool active)
    {
        UnityEngine.Events.UnityEvent ev = FindEvent(src, field);
        if (ev == null || target == null) return;
        UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(ev, target.SetActive, active);
        EditorUtility.SetDirty(src);
    }

    private static void FaceYaw(GameObject g, float yaw)
    {
        if (g != null) g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private static void HintPad(Transform zone, float x, float z, string title, string body,
        string sign = null, float signDx = 3.6f, float signDz = 0f, float signYaw = 0f)
    {
        GameObject pad = Prim(PrimitiveType.Cylinder, "HintPad", zone,
            new Vector3(x, _groundY + 0.06f, z), new Vector3(3f, 0.03f, 3f),
            Mat("Pad", new Color(0.2f, 0.9f, 0.3f)), false);
        AddHint(pad.transform, title, body, 4f);
        if (sign != null) StationSign(zone, x + signDx, z + signDz, sign, signYaw);
    }

    private static void AddSpawn(Transform zone, float x, float z)
    {
        GameObject sp = new GameObject("Spawn");
        sp.transform.SetParent(zone, false);
        sp.transform.localPosition = new Vector3(x, _playerY, z);
        Spawns.Add(sp.transform);
        SpawnNames.Add(System.Text.RegularExpressions.Regex.Replace(zone.name, "(?<=[a-z])(?=[A-Z])", " "));
    }

    private static void BuildLevelHelpers(GameObject player)
    {
        GameObject helpPanel, hintPanel;
        TextMeshProUGUI hintLabel;
        BuildHud(out helpPanel, out hintPanel, out hintLabel);

        GameObject g = new GameObject("LevelHelper");
        g.transform.SetParent(_root, false);
        TestArenaHelper helper = g.AddComponent<TestArenaHelper>();
        helper.player = player != null ? player.transform : null;
        helper.swarm = UnityEngine.Object.FindObjectOfType<SwarmController>();
        helper.spawns = Spawns.ToArray();
        helper.spawnNames = SpawnNames.ToArray();
        helper.helpPanel = helpPanel;
        EditorUtility.SetDirty(helper);

        ArenaHintDisplay hint = g.AddComponent<ArenaHintDisplay>();
        hint.player = helper.player;
        hint.label = hintLabel;
        hint.panel = hintPanel;
        EditorUtility.SetDirty(hint);
    }

    // ---------------------------------------------------------------- camp (world 0,-95)

    private static void CampHints()
    {
        Transform c = _root.Find("Camp");
        if (c == null) return;
        AddSpawn(c, 0f, -5f);
        HintPad(c, 0f, 0f, "BASE CAMP",
            "1-4 pick the troop type, ` picks all.\nLEFT click tap = send 1 troop, hold = send the group.\nRIGHT click tap = recall 1, hold = recall the group.\nZ targets what is near you, X clears the target.",
            "BASE CAMP: LEARN THE CONTROLS");
        HintPad(c, 0f, 12f, "LOOT",
            "Send a troop at gold or a potion and it carries it back to you.\nGear is equipped by the troop that finds it (up to 4 pieces).\nBarrels break fast. The big crate fits only 4 troops.",
            "LOOT AND BARRELS", -3.6f);
    }

    // ---------------------------------------------------------------- guard post (on the path, world 0,-26)

    private static void BuildGuardPost()
    {
        Transform g = Zone("GuardPost", 0f, -26f);
        AddSpawn(g, 0f, -16f);
        GameObject guard = AddEnemy(g, "Guard", 0f, 0f, _hunter, 4);
        FaceYaw(guard, 180f);
        Rock(g, -9f, -4f, 2.2f);
        Rock(g, 10f, 3f, 1.8f);
        HintPad(g, 0f, -16f, "ALERT GUARD AHEAD",
            "The guard ahead (4 attach points) is awake and faces you.\nA lone troop attacking from the front is countered and thrown back. Send 3 or more together, or lure it and go around to its back.\nIf it reaches you, up to 4 enemies can attach to you from any direction.",
            "GUARD AHEAD", 3.6f);
    }

    // ---------------------------------------------------------------- gold hollow (west jungle)

    private static void BuildGoldHollow()
    {
        Transform z = PoiZone("GoldHollow");
        JunctionSign("GoldHollow", "GOLD MINE");
        AddSpawn(z, 11f, 0f);

        Clone("GoldMine", z, -6f, -4f, "GoldHollow_Mine");
        WP(z, "GoldHollow_Depot", -6f, 8f, 0);
        Prim(PrimitiveType.Cube, "OrePile", z, new Vector3(-10f, _groundY + 0.4f, -1f), new Vector3(1.4f, 0.8f, 1.2f), Mat("Ore", new Color(0.9f, 0.75f, 0.2f)), false);
        Rock(z, -11f, -8f, 1.8f);
        Rock(z, -12f, 4f, 1.3f);

        HintPad(z, 11f, 0f, "GOLD MINE",
            "Send 2 or 3 troops at the mine. Each one carries a load slowly to the glowing ring (the depot) to the north, deposits it and walks back to you by itself.\nPress Z on the mine first so the group goes to it.",
            "GOLD MINE: CARRY THE LOADS", 0f, 4f, -90f);
    }

    // ---------------------------------------------------------------- stash glade (east jungle)

    private static void BuildStashGlade()
    {
        Transform z = PoiZone("StashGlade");
        JunctionSign("StashGlade", "SEALED STASH");
        AddSpawn(z, -12f, 0f);

        // walled pen with a closed gate; a breakable fence stands in front of the gate
        Transform pen = new GameObject("StashPen").transform;
        pen.SetParent(z, false);
        FenceSegment(pen, "WallNorth", -4f, 4f, 4f, 4f);
        FenceSegment(pen, "WallSouth", -4f, -4f, 4f, -4f);
        FenceSegment(pen, "WallEast", 4f, -4f, 4f, 4f);
        FenceSegment(pen, "WallWestA", -4f, -4f, -4f, -2f);
        FenceSegment(pen, "WallWestB", -4f, 2f, -4f, 4f);
        GameObject gate = FenceSegment(pen, "PenGate", -4f, -2f, -4f, 2f);
        Prim(PrimitiveType.Cube, "GateBoards", gate.transform, new Vector3(0f, 0.8f, 0f), new Vector3(0.25f, 1.5f, 3.8f), Mat("GateWood", new Color(0.35f, 0.2f, 0.1f)), false);

        PickupItemAt(z, "TestGold", "Stash_GoldA", 0f, -1f, 0);
        PickupItemAt(z, "TestGold", "Stash_GoldB", 2f, 1f, 0);
        PickupItemAt(z, "TestPotion", "Stash_Potion", 0f, 2f, 0);

        GameObject fence = Breakable(z, "Stash_Fence", -6.5f, 0f, true, 250f, 5, false, "TestObject_Fence");
        FaceYaw(fence, 90f);
        OpenOnBreak(fence, gate);
        if (fence != null) HookSay(fence.GetComponent<BreakableObject>(), "onBroken", "FENCE BROKEN! The stash gate is open. Send troops for the loot.");

        HintPad(z, -12f, 0f, "SEALED STASH",
            "The gold inside the pen is walled in. Troops cannot reach it.\nThe wooden fence blocks the gate. Send up to 5 troops at the fence (the number shows free spots). When it breaks, the gate opens and you can collect the stash.",
            "LOCKED STASH: BREAK THE FENCE", 0f, 4f, 90f);
    }

    // ---------------------------------------------------------------- sentry glade (east jungle)

    private static void BuildSentryGlade()
    {
        Transform z = PoiZone("SentryGlade");
        JunctionSign("SentryGlade", "SENTRY GLADE");
        AddSpawn(z, -12f, 0f);
        EnemyData sentry = MakeEnemyData("EnemyData_Sentry", 300f, 8f, 1f, 0f, 1f, 3.5f);

        GameObject a = AddEnemy(z, "Sentry_A", 6f, 4f, sentry, 3);
        GameObject b = AddEnemy(z, "Sentry_B", 10f, -3f, sentry, 3);
        GameObject c = AddEnemy(z, "Sentry_C", 2f, -9f, sentry, 3);
        FaceYaw(a, 90f);
        FaceYaw(b, 90f);
        FaceYaw(c, -90f);
        foreach (GameObject s in new[] { a, b, c }) Set<EnemyBase>(s, "alertAfterHit", 3f);
        Rock(z, 12f, 8f, 2f);
        Rock(z, 0f, 9f, 1.5f);

        HintPad(z, -12f, 0f, "STEALTH",
            "Three sentries (3 attach points each) only notice you from about 1 unit away.\nSentries A and B face east, away from you. Attack them from behind: triple damage (30 instead of 10).\nAfter the first hit they need about 3 seconds to react, then they fight back. Kill them fast with a group.\nSentry C faces you: it is still unaware, so it cannot counter, but there is no backstab bonus.\nPress Z until one is outlined, then send troops.",
            "SENTRIES: STAY BEHIND THEM", 0f, 4f, 90f);
    }

    // ---------------------------------------------------------------- roadblock (west jungle)

    private static void BuildRoadblock()
    {
        Transform z = PoiZone("Roadblock");
        JunctionSign("Roadblock", "ROADBLOCK");
        AddSpawn(z, 11f, 0f);

        FenceSegment(z, "RoadblockNorth", -3f, 2.5f, -3f, 9f);
        FenceSegment(z, "RoadblockSouth", -3f, -9f, -3f, -2.5f);
        GameObject bar = Breakable(z, "Roadblock_Barricade", -3f, 0f, true, 250f, 5, true, "TestObject_Barricade");
        FaceYaw(bar, 90f);
        PickupItemAt(z, "TestGold", "Roadblock_GoldA", -9f, -2f, 0);
        PickupItemAt(z, "TestPotion", "Roadblock_Potion", -9f, 2f, 0);

        HintPad(z, 11f, 0f, "UNBREAKABLE",
            "Not everything can be destroyed.\nIf you press Z on the barricade and send troops at it, they walk up, find they cannot hurt it, pause and return.\nThe loot behind it is not sealed. Troops sent at the gold find a way around the fence.",
            "ROADBLOCK: CANNOT BE BROKEN", 0f, 4f, -90f);
    }

    // ---------------------------------------------------------------- the gate (on the path, world 25,20)

    private static void BuildGate()
    {
        Transform g = Zone("Gate", 25f, 20f);
        AddSpawn(g, 0f, -16f);
        EnemyData guardData = MakeEnemyData("EnemyData_GateGuard", 200f, 6f, 12f, 1f, 1f, 3.5f);

        // wall across the whole level, one 8-unit gap closed by the door
        FenceSegment(g, "WallWest", -140f, 0f, -4f, 0f);   // world x -115, past the boundary at -111
        FenceSegment(g, "WallEast", 4f, 0f, 90f, 0f);      // world x 115, past the boundary at 111

        GameObject door = Prim(PrimitiveType.Cube, "Gate_Door", g, new Vector3(0f, _groundY + 2f, 0f), new Vector3(8f, 4f, 0.7f), Mat("Door", new Color(0.5f, 0.3f, 0.1f)), true);
        NavMeshObstacle ob = door.AddComponent<NavMeshObstacle>();
        ob.shape = NavMeshObstacleShape.Box;
        ob.carving = true;
        ob.size = Vector3.one;
        Material wood = Mat("FenceWood", new Color(0.55f, 0.36f, 0.18f));
        Prim(PrimitiveType.Cube, "PillarW", g, new Vector3(-4.5f, _groundY + 2.5f, 0f), new Vector3(1f, 5f, 1f), wood, true);
        Prim(PrimitiveType.Cube, "PillarE", g, new Vector3(4.5f, _groundY + 2.5f, 0f), new Vector3(1f, 5f, 1f), wood, true);

        // the wheel, defended by two guards that spot you from 12 units away
        GameObject wheel = Clone("Interactable", g, 12f, -9f, "Gate_Wheel");
        if (wheel != null)
        {
            Set<AttachableTarget>(wheel, "category", (int)InteractionCategory.Interactable);
            Set<AttachableTarget>(wheel, "requiredUnitsToMove", 3);
            Set<AttachableTarget>(wheel, "maxAttachSlots", 5);
            Set<InteractableObject>(wheel, "operateDuration", 3f);
            Set<InteractableObject>(wheel, "singleUse", true);
            GameObject link = new GameObject("Gate_DoorLink");
            link.transform.SetParent(g, false);
            DoorLink dl = link.AddComponent<DoorLink>();
            dl.wheel = wheel.GetComponent<AttachableTarget>();
            dl.door = door;
            EditorUtility.SetDirty(dl);

            InteractableObject io = wheel.GetComponent<InteractableObject>();
            HookSay(io, "onOperateStart", "THE WHEEL IS TURNING... keep the troops on it!");
            HookSay(io, "onCompleted", "THE GATE IS OPEN! Follow the path north.");
        }
        FaceYaw(AddEnemy(g, "Gate_GuardA", 8f, -4f, guardData, 3), 180f);
        FaceYaw(AddEnemy(g, "Gate_GuardB", 17f, -6f, guardData, 3), 180f);

        HintPad(g, 0f, -16f, "THE GATE",
            "GOAL: get through the locked gate. The wall runs to both edges of the jungle, so there is no way round.\nHOW: the wheel to the east opens it, but two guards defend it. Deal with them first.\nRESULT: a message tells you when the gate is open.",
            "GATE LOCKED: WHEEL TO THE EAST >>>", 3.6f);
        HintPad(g, 13f, -17f, "THE WHEEL",
            "GOAL: turn the wheel with 3 troops.\nHOW: kill the guards (they charge when you come close). Walk up to the wheel and tap left-click 3 times, one troop each. The number on the wheel counts down 3, 2, 1, 0. With 1 or 2 troops nothing happens.\nRESULT: at 0 the wheel turns for 3 seconds, the troops return and the door disappears.",
            "WHEEL: NEEDS 3 TROOPS", 0f, 3.5f, 0f);
    }

    // ---------------------------------------------------------------- carry yard (west jungle, beyond the gate)

    private static void BuildCarryYard()
    {
        Transform z = PoiZone("CarryYard");
        JunctionSign("CarryYard", "CARRY YARD");
        AddSpawn(z, 13f, 0f);

        // ===== JOB 1 (north): carry the crate round a wall to the ring =====
        FenceSegment(z, "CrateWall", 0f, 4f, 0f, 12f);
        GameObject crate = Movable(z, "Yard_Crate", 8f, 7f);
        WP(z, "Yard_Depot", -8f, 7f, 0);
        PickupItemAt(z, "TestGear", "Yard_RewardGear", -11.5f, 4.5f, 1);
        PickupItemAt(z, "TestGold", "Yard_RewardGold", -11.5f, 9.5f, 0);
        Transform rewardGear = z.Find("Yard_RewardGear");
        Transform rewardGold = z.Find("Yard_RewardGold");
        if (rewardGear != null) rewardGear.gameObject.SetActive(false);
        if (rewardGold != null) rewardGold.gameObject.SetActive(false);
        if (crate != null)
        {
            MovableObject mv = crate.GetComponent<MovableObject>();
            if (rewardGear != null) HookSetActive(mv, "onDelivered", rewardGear.gameObject, true);
            if (rewardGold != null) HookSetActive(mv, "onDelivered", rewardGold.gameObject, true);
            HookSay(mv, "onDelivered", "CRATE DELIVERED! A supply drop appeared at the ring.");
        }

        // ===== JOB 2 (south): carry the key to the pedestal, the vault door opens =====
        Transform pen = new GameObject("VaultPen").transform;
        pen.SetParent(z, false);
        FenceSegment(pen, "VaultNorth", -13f, -5f, -7f, -5f);
        FenceSegment(pen, "VaultSouth", -13f, -12f, -7f, -12f);
        FenceSegment(pen, "VaultWest", -13f, -12f, -13f, -5f);
        FenceSegment(pen, "VaultEastA", -7f, -12f, -7f, -10f);
        FenceSegment(pen, "VaultEastB", -7f, -7f, -7f, -5f);
        GameObject vaultGate = FenceSegment(pen, "VaultGate", -7f, -10f, -7f, -7f);
        Prim(PrimitiveType.Cube, "GateBoards", vaultGate.transform, new Vector3(0f, 0.8f, 0f), new Vector3(0.25f, 1.5f, 2.8f), Mat("GateWood", new Color(0.35f, 0.2f, 0.1f)), false);
        PickupItemAt(z, "TestGold", "Vault_GoldA", -10f, -9f, 0);
        PickupItemAt(z, "TestGold", "Vault_GoldB", -11f, -7f, 0);
        PickupItemAt(z, "TestGear", "Vault_Gear", -11.5f, -10.5f, 1);

        // permanent, visible pedestal in front of the vault door
        Prim(PrimitiveType.Cylinder, "KeyPedestal", z, new Vector3(-3.5f, _groundY + 0.07f, -8.5f), new Vector3(3.6f, 0.05f, 3.6f), Mat("KeySlot", new Color(1f, 0.85f, 0.2f)), false);

        GameObject key = Clone("Retrievable", z, 9f, -8f, "Yard_Key");
        GameObject dest = new GameObject("Yard_KeySlot");
        dest.transform.SetParent(z, false);
        dest.transform.localPosition = new Vector3(-3.5f, _groundY, -8.5f);
        GameObject marker = Disc(dest.transform, "Marker", 0f, 0f, 3f, new Color(0.2f, 1f, 0.4f));
        marker.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        marker.SetActive(false);
        if (key != null)
        {
            Set<AttachableTarget>(key, "category", (int)InteractionCategory.Retrievable);
            Set<AttachableTarget>(key, "requiredUnitsToMove", 1);
            Set<AttachableTarget>(key, "maxAttachSlots", 3);
            Set<MovableObject>(key, "fixedDestination", dest.transform);
            Set<MovableObject>(key, "destinationMarker", marker);
            MovableObject km = key.GetComponent<MovableObject>();
            HookDeactivate(km, "onDelivered", vaultGate);
            HookSay(km, "onDelivered", "KEY PLACED! The vault door is open. Send troops for the loot inside.");
        }

        // ===== signs and hints =====
        StationSign(z, -8f, 12f, "DROP ZONE: CRATE GOES HERE", -90f);
        StationSign(z, -3.5f, -12.5f, "KEY GOES ON THE YELLOW DISC", -90f);
        StationSign(z, -10f, -2f, "VAULT: OPENS WHEN THE KEY ARRIVES", -90f);

        HintPad(z, 13f, 0f, "CARRY YARD",
            "GOAL: finish two jobs. Both are done by carrying.\nHOW: JOB 1 is north, carry the crate to the glowing ring. JOB 2 is south, carry the key to the yellow disc. Follow the signs.\nRESULT: a message appears every time you finish a job.",
            "CARRY YARD: JOB 1 NORTH, JOB 2 SOUTH", 0f, 4f, -90f);
        HintPad(z, 6f, 12f, "JOB 1: THE CRATE",
            "GOAL: bring the heavy crate to the glowing ring in the west. A wall is in the way, so the troops will go round it.\nHOW: stand next to the crate and tap left-click 3 times (one troop each). The number counts down 3, 2, 1, 0. At 0 it moves by itself. More troops carry it faster. To steer it yourself, press Z (it freezes), hold LEFT + RIGHT and sweep, then press X to let it continue.\nRESULT: at the ring it shows Placed and a supply drop (gear and gold) appears.",
            "JOB 1: CRATE TO THE RING", 0f, 3.5f, -90f);
        HintPad(z, 6f, -13f, "JOB 2: THE KEY",
            "GOAL: open the vault. The gold and gear inside are fenced in and troops cannot reach them.\nHOW: stand next to the key and tap left-click once. One troop carries it to the yellow disc in front of the vault door. (If you used Z on it, press X so it can move.)\nRESULT: the key shows Placed, the vault door disappears and the loot can be collected.",
            "JOB 2: KEY TO THE VAULT", 0f, 3.5f, -90f);
    }

    // ---------------------------------------------------------------- outpost (east jungle, beyond the gate)

    private static void BuildOutpost()
    {
        Transform z = PoiZone("Outpost");
        JunctionSign("Outpost", "OUTPOST");
        AddSpawn(z, -23f, -5f);
        Spawns[Spawns.Count - 1].localRotation = Quaternion.Euler(0f, 90f, 0f);

        EnemyData guardData = MakeEnemyData("EnemyData_OutpostGuard", 300f, 8f, 9f, 1f, 1f, 3.5f);
        EnemyData hunterData = MakeEnemyData("EnemyData_CartHunter", 250f, 9f, 9f, 1f, 1.2f, 3.5f);
        EnemyData ambushData = MakeEnemyData("EnemyData_Ambusher", 120f, 25f, 10f, 1f, 1f, 4f);

        // ===== JOB 1: enemy-held ring with two guards =====
        Waypoint ring = WP(z, "Outpost_Ring", 8f, 14f, 1);
        if (ring != null)
        {
            Set<Waypoint>(ring.gameObject, "zoneRadius", 6f);
            GameObject ann = new GameObject("Outpost_Announcer");
            ann.transform.SetParent(z, false);
            WaypointAnnouncer wa = ann.AddComponent<WaypointAnnouncer>();
            wa.target = ring;
            wa.capturedMessage = "OUTPOST CAPTURED! The ring is yours. Carts can now be delivered here.";
            wa.lostMessage = "The enemy retook the ring! Carts can no longer be delivered here.";
            EditorUtility.SetDirty(wa);
        }
        FaceYaw(AddEnemy(z, "Outpost_GuardA", 5f, 17f, guardData, 3), -90f);
        FaceYaw(AddEnemy(z, "Outpost_GuardB", 11f, 17f, guardData, 3), -90f);

        // ===== JOB 2: two carts and two hidden hunters beside the route =====
        GameObject cartA = Movable(z, "Outpost_CartA", -6f, -19f);
        GameObject cartB = Movable(z, "Outpost_CartB", -6f, -14f);
        foreach (GameObject c in new[] { cartA, cartB })
        {
            if (c == null) continue;
            Set<MovableObject>(c, "isCart", true);
            Set<MovableObject>(c, "cartHealth", 400f);
        }
        FaceYaw(AddEnemy(z, "Outpost_CartHunterA", 0f, -2f, hunterData, 4), 200f);
        FaceYaw(AddEnemy(z, "Outpost_CartHunterB", 4f, -6f, hunterData, 4), 160f);
        Rock(z, -2.5f, 0f, 1.8f);
        Rock(z, 6.5f, -4f, 1.5f);

        // ===== JOB 3: sealed armory, opens when a cart arrives =====
        Transform armory = new GameObject("Armory").transform;
        armory.SetParent(z, false);
        FenceSegment(armory, "WallWest", 16.5f, -15.5f, 16.5f, -8.5f);
        FenceSegment(armory, "WallEast", 23.5f, -15.5f, 23.5f, -8.5f);
        FenceSegment(armory, "WallSouth", 16.5f, -15.5f, 23.5f, -15.5f);
        FenceSegment(armory, "WallNorthA", 16.5f, -8.5f, 18.5f, -8.5f);
        FenceSegment(armory, "WallNorthB", 21.5f, -8.5f, 23.5f, -8.5f);
        GameObject armoryGate = FenceSegment(armory, "ArmoryGate", 18.5f, -8.5f, 21.5f, -8.5f);
        Prim(PrimitiveType.Cube, "GateBoards", armoryGate.transform, new Vector3(0f, 0.8f, 0f), new Vector3(0.25f, 1.5f, 2.8f), Mat("GateWood", new Color(0.35f, 0.2f, 0.1f)), false);
        PickupItemAt(z, "TestGear", "Armory_GearA", 19f, -12f, 1);
        PickupItemAt(z, "TestGear", "Armory_GearB", 21f, -11f, 1);
        PickupItemAt(z, "TestGear", "Armory_GearC", 20.5f, -14f, 1);

        foreach (GameObject c in new[] { cartA, cartB })
        {
            if (c == null) continue;
            MovableObject mv = c.GetComponent<MovableObject>();
            HookSay(mv, "onDelivered", "CART DELIVERED! The armory gate is open. Send troops for the gear.");
            HookDeactivate(mv, "onDelivered", armoryGate);
        }

        // ===== BAIT: gold that wakes an ambush when lifted =====
        PickupItemAt(z, "TestGold", "Trap_Gold", -14f, 18f, 0);
        GameObject ambA = AddEnemy(z, "Trap_AmbusherA", -19f, 22f, ambushData, 3);
        GameObject ambB = AddEnemy(z, "Trap_AmbusherB", -8f, 23f, ambushData, 3);
        GameObject ambC = AddEnemy(z, "Trap_AmbusherC", -14f, 11f, ambushData, 3);
        foreach (GameObject g in new[] { ambA, ambB, ambC }) if (g != null) g.SetActive(false);
        GameObject trig = new GameObject("Trap_Trigger");
        trig.transform.SetParent(z, false);
        AmbushTrigger at = trig.AddComponent<AmbushTrigger>();
        Transform bait = z.Find("Trap_Gold");
        at.watched = bait != null ? bait.gameObject : null;
        at.activate = new[] { ambA, ambB, ambC };
        at.message = "AMBUSH! Enemies jumped out of the trees. Fight them off, or protect the troop carrying the gold.";
        EditorUtility.SetDirty(at);

        // ===== signs and hints =====
        StationSign(z, 20f, -6f, "ARMORY: OPENS WHEN A CART ARRIVES", 180f);

        HintPad(z, -23f, -5f, "THE OUTPOST",
            "GOAL: take this outpost from the enemy and win its supplies.\nHOW: there are three jobs. JOB 1, capture the red ring to the north-east. JOB 2, escort a cart to that ring (it only works once the ring is yours). JOB 3, the armory in the south-east opens when a cart arrives. In the north-west there is some gold lying about.\nRESULT: a message appears every time you finish something.",
            "THE OUTPOST: CAPTURE, ESCORT, ARMORY", 0f, 5f, 90f);
        HintPad(z, -4f, 14f, "JOB 1: CAPTURE THE RING",
            "GOAL: turn the red ring blue. Two guards defend it, and a ring cannot be captured while an enemy is inside it.\nHOW: press ` (all troops), walk into the middle of the ring and press Q to plant a guard flag. Your troops gather around the flag, fight whatever is near it, and capture the ring. More troops capture it faster. Afterwards hold right-click to recall them.\nRESULT: the ring turns blue and a banner says OUTPOST CAPTURED.",
            "ENEMY RING: CAPTURE IT", 4f, 0f, 90f);
        HintPad(z, -14f, -19f, "JOB 2: ESCORT A CART",
            "GOAL: bring a cart to the captured ring. A cart only moves to a ring you own, so do job 1 first.\nHOW: press Z until the cart is outlined, tap left-click 3 times (one troop each, the number counts 3, 2, 1, 0), then press X. The cart rolls off by itself. Two hunters hide beside the route and attack the cart as it passes. Send your other troops at them (press Z on a hunter, hold left-click). Each cart has 400 health and there are two carts, so one is a spare.\nRESULT: at the ring the cart shows Placed and the armory opens.",
            "CARTS: Z, SEND 3, THEN X", 4f, 0f, 90f);
        HintPad(z, 20f, -1f, "JOB 3: THE ARMORY",
            "GOAL: collect the gear inside the walled armory.\nHOW: it is sealed and troops cannot reach it. It opens when a cart arrives at the ring. Then tap-send troops at the gear. A troop can wear up to 4 pieces.\nRESULT: an Army strength popup appears for every piece equipped.");
        HintPad(z, -20f, 10f, "THE BAIT",
            "GOAL: collect the gold pile to the north-east of this pad.\nHOW: tap-send one troop at it. It looks too easy. When the gold is lifted, enemies hidden in the trees wake up and charge. Keep a group close and hold left-click to answer the ambush. If the carrier dies, the gold drops where it fell and can be picked up again.\nRESULT: a banner says AMBUSH, and the gold counts once a troop delivers it to you.",
            "FREE GOLD ...", 4f, 0f, 90f);
    }

    // ---------------------------------------------------------------- the ancient ruins (north-west jungle)

    private class RunePillar { public GameObject wheel, lit, unlit, dormant; }

    private static MissionStep NewStep(Transform parent, string name)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        return g.AddComponent<MissionStep>();
    }

    private static void HookStep(Component src, string field, MissionStep step)
    {
        UnityEngine.Events.UnityEvent ev = FindEvent(src, field);
        if (ev == null || step == null) return;
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(ev, step.Complete);
        EditorUtility.SetDirty(src);
    }

    private static RunePillar MakePillar(Transform z, string name, float x, float zz, float obDx, float obDz,
        int troops, bool dormant, string label, string dormantSign)
    {
        RunePillar p = new RunePillar();
        p.wheel = Clone("Interactable", z, x, zz, name + "_Wheel");
        if (p.wheel != null)
        {
            Set<AttachableTarget>(p.wheel, "category", (int)InteractionCategory.Interactable);
            Set<AttachableTarget>(p.wheel, "requiredUnitsToMove", troops);
            Set<AttachableTarget>(p.wheel, "maxAttachSlots", 5);
            Set<InteractableObject>(p.wheel, "operateDuration", 4f);
            Set<InteractableObject>(p.wheel, "singleUse", true);
            Set<InteractableObject>(p.wheel, "resetProgressWhenTroopsLeave", true);
            InteractableObject io = p.wheel.GetComponent<InteractableObject>();
            HookSay(io, "onOperateStart", "THE RUNE IS CHARGING... keep the troops on it!");
            HookSay(io, "onOperateStop", "The rune faded because the troops left it. Progress is lost, try again.");
        }

        float ox = x + obDx, oz = zz + obDz;
        GameObject ob = Prim(PrimitiveType.Cube, name + "_Obelisk", z, new Vector3(ox, _groundY + 2.25f, oz), new Vector3(1.2f, 4.5f, 1.2f), Mat("RuneStone", new Color(0.42f, 0.42f, 0.48f)), true);
        NavMeshObstacle nob = ob.AddComponent<NavMeshObstacle>();
        nob.shape = NavMeshObstacleShape.Box;
        nob.carving = true;
        nob.size = Vector3.one;
        p.unlit = Prim(PrimitiveType.Cube, name + "_CapOff", z, new Vector3(ox, _groundY + 5f, oz), new Vector3(1.5f, 0.9f, 1.5f), Mat("RuneOff", new Color(0.25f, 0.25f, 0.3f)), false);
        p.lit = Prim(PrimitiveType.Cube, name + "_CapOn", z, new Vector3(ox, _groundY + 5f, oz), new Vector3(1.5f, 0.9f, 1.5f), Mat("RuneOn", new Color(0.3f, 0.9f, 1f)), false);
        GameObject lg = new GameObject("Glow");
        lg.transform.SetParent(p.lit.transform, false);
        lg.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        Light l = lg.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.4f, 0.9f, 1f);
        l.range = 12f;
        l.intensity = 2.5f;
        p.lit.SetActive(false);
        StationSign(z, ox + 3.4f, oz, label, 0f);

        if (dormant)
        {
            if (p.wheel != null) p.wheel.SetActive(false);
            p.dormant = new GameObject(name + "_Dormant");
            p.dormant.transform.SetParent(z, false);
            Prim(PrimitiveType.Cylinder, "Slab", p.dormant.transform, new Vector3(x, _groundY + 0.07f, zz), new Vector3(3.6f, 0.06f, 3.6f), Mat("RuneOff", new Color(0.25f, 0.25f, 0.3f)), false);
            StationSign(p.dormant.transform, x, zz - 3.5f, dormantSign, 0f);
        }
        return p;
    }

    private static void BuildRuins()
    {
        Transform z = PoiZone("Ruins");
        JunctionSign("Ruins", "ANCIENT RUINS");
        AddSpawn(z, 17f, -3f);
        Spawns[Spawns.Count - 1].localRotation = Quaternion.Euler(0f, -90f, 0f);

        EnemyData warden = MakeEnemyData("EnemyData_Warden", 700f, 14f, 9f, 1f, 1.2f, 3.2f);
        EnemyData cultist = MakeEnemyData("EnemyData_Cultist", 120f, 5f, 8f, 1f, 1f, 3.5f);
        EnemyData escort = MakeEnemyData("EnemyData_TempleEscort", 150f, 6f, 10f, 1f, 1f, 3.5f);

        // ----- ruined columns around the edge (decoration) -----
        Material ruin = Mat("RuinStone", new Color(0.62f, 0.6f, 0.55f));
        for (int i = 0; i < 9; i++)
        {
            float ang = i * 40f + 10f;
            if (Mathf.Abs(Mathf.DeltaAngle(ang, 0f)) < 28f) continue;
            float rx = Mathf.Cos(ang * Mathf.Deg2Rad) * 23f, rz = Mathf.Sin(ang * Mathf.Deg2Rad) * 23f;
            if (rx > 0f && rx < 15f && rz < -8f) continue; // temple
            float h = R(1.5f, 4f);
            GameObject col = Prim(PrimitiveType.Cube, "BrokenColumn", z, new Vector3(rx, _groundY + h * 0.5f, rz), new Vector3(1.4f, h, 1.4f), ruin, true);
            NavMeshObstacle co = col.AddComponent<NavMeshObstacle>();
            co.shape = NavMeshObstacleShape.Box;
            co.carving = true;
            co.size = Vector3.one;
            Prim(PrimitiveType.Cube, "Rubble", z, new Vector3(rx + R(-2f, 2f), _groundY + 0.3f, rz + R(-2f, 2f)), new Vector3(1.1f, 0.6f, 0.9f), ruin, false);
        }

        // ----- the three rune pillars -----
        RunePillar p1 = MakePillar(z, "Rune1", 7f, 11f, 0f, 4.5f, 2, false, "RUNE 1", null);
        RunePillar p2 = MakePillar(z, "Rune2", -9f, 12f, 0f, 4.5f, 2, true, "RUNE 2", "RUNE 2 IS ASLEEP: LIGHT RUNE 1 FIRST");
        RunePillar p3 = MakePillar(z, "Rune3", -14f, -12f, 0f, 4.5f, 3, true, "RUNE 3", "RUNE 3 IS ASLEEP: PLACE THE FRAGMENT FIRST");

        // ----- guards -----
        FaceYaw(AddEnemy(z, "Rune1_GuardA", 3f, 14f, _weak, 3), 180f);
        FaceYaw(AddEnemy(z, "Rune1_GuardB", 10f, 16f, _weak, 3), 180f);
        FaceYaw(AddEnemy(z, "Rune2_Warden", -4f, 7f, warden, 4), 150f);
        AddEnemy(z, "Rune3_CultistA", -19f, -14f, cultist, 3);
        AddEnemy(z, "Rune3_CultistB", -14f, -18f, cultist, 3);
        AddEnemy(z, "Rune3_CultistC", -18f, -8f, cultist, 3);

        // ----- the rune fragment (hidden until rune 2 is lit) -----
        GameObject frag = Clone("Retrievable", z, -14f, -1f, "Ruins_Fragment");
        Prim(PrimitiveType.Cylinder, "FragmentPedestal", z, new Vector3(-8f, _groundY + 0.07f, -6f), new Vector3(3.6f, 0.05f, 3.6f), Mat("KeySlot", new Color(1f, 0.85f, 0.2f)), false);
        GameObject dest = new GameObject("Ruins_FragmentSlot");
        dest.transform.SetParent(z, false);
        dest.transform.localPosition = new Vector3(-8f, _groundY, -6f);
        GameObject marker = Disc(dest.transform, "Marker", 0f, 0f, 3f, new Color(0.2f, 1f, 0.4f));
        marker.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        marker.SetActive(false);
        if (frag != null)
        {
            Set<AttachableTarget>(frag, "category", (int)InteractionCategory.Retrievable);
            Set<AttachableTarget>(frag, "requiredUnitsToMove", 1);
            Set<AttachableTarget>(frag, "maxAttachSlots", 3);
            Set<MovableObject>(frag, "fixedDestination", dest.transform);
            Set<MovableObject>(frag, "destinationMarker", marker);
            frag.SetActive(false);
        }

        // ----- the temple (walls, door, guardian, escorts, treasure) -----
        Transform temple = new GameObject("Temple").transform;
        temple.SetParent(z, false);
        FenceSegment(temple, "WallWest", 2f, -20f, 2f, -10f);
        FenceSegment(temple, "WallEast", 12f, -20f, 12f, -10f);
        FenceSegment(temple, "WallSouth", 2f, -20f, 12f, -20f);
        FenceSegment(temple, "WallNorthA", 2f, -10f, 4.5f, -10f);
        FenceSegment(temple, "WallNorthB", 9.5f, -10f, 12f, -10f);
        GameObject templeDoor = FenceSegment(temple, "TempleDoor", 4.5f, -10f, 9.5f, -10f);
        Prim(PrimitiveType.Cube, "DoorBoards", templeDoor.transform, new Vector3(0f, 1f, 0f), new Vector3(0.3f, 2f, 4.8f), Mat("GateWood", new Color(0.35f, 0.2f, 0.1f)), false);

        GameObject guardian = AddBigEnemy(z, "Temple_Guardian", 7f, -16f, _mini, 5, 1.6f, 2.6f, 3.8f, 5f);
        GameObject escA = AddEnemy(z, "Temple_EscortA", 4f, -13f, escort, 3);
        GameObject escB = AddEnemy(z, "Temple_EscortB", 10f, -13f, escort, 3);
        foreach (GameObject g in new[] { guardian, escA, escB }) if (g != null) g.SetActive(false);

        PickupItemAt(z, "TestGear", "Temple_GearA", 4f, -18.5f, 1);
        PickupItemAt(z, "TestGear", "Temple_GearB", 10f, -18.5f, 1);
        PickupItemAt(z, "TestGold", "Temple_Gold", 7f, -19f, 0);
        PickupItemAt(z, "TestPotion", "Temple_Potion", 7f, -18f, 0);
        GameObject[] rewards =
        {
            z.Find("Temple_GearA").gameObject, z.Find("Temple_GearB").gameObject,
            z.Find("Temple_Gold").gameObject, z.Find("Temple_Potion").gameObject
        };
        foreach (GameObject r in rewards) r.SetActive(false);

        // ----- mission steps -----
        MissionStep intro = NewStep(z, "Mission_Intro");
        intro.trackerId = "ruins";
        intro.beginRadius = 28f;
        intro.beginText = "RUINS 0/3 | Light rune 1: the wheel in the north-east.";
        intro.beginMessage = "THE ANCIENT RUINS: wake the temple. Follow the MISSIONS list on the left.";

        MissionStep s1 = NewStep(z, "Mission_Step1");
        s1.trackerId = "ruins";
        s1.nextText = "RUINS 1/3 | Light rune 2: the wheel in the north-west. A warden guards it.";
        s1.message = "RUNE 1 LIT! Rune 2 in the north-west is awake. A warden guards it.";
        s1.activate = new[] { p1.lit, p2.wheel };
        s1.deactivate = new[] { p1.unlit, p2.dormant };

        MissionStep s2 = NewStep(z, "Mission_Step2");
        s2.trackerId = "ruins";
        s2.nextText = "RUINS 2/3 | Carry the rune fragment (west) to the yellow disc (south-west).";
        s2.message = "RUNE 2 LIT! A rune fragment rose from the ground in the west. Carry it to the yellow disc.";
        s2.activate = new[] { p2.lit, frag };
        s2.deactivate = new[] { p2.unlit };

        MissionStep s3 = NewStep(z, "Mission_Step3");
        s3.trackerId = "ruins";
        s3.nextText = "RUINS 2/3 | Light rune 3: the wheel in the south-west. 3 troops needed, cultists guard it.";
        s3.message = "FRAGMENT PLACED! Rune 3 in the south-west is awake. It needs 3 troops, and cultists guard it.";
        s3.activate = new[] { p3.wheel };
        s3.deactivate = new[] { p3.dormant };

        MissionStep s4 = NewStep(z, "Mission_Step4");
        s4.trackerId = "ruins";
        s4.nextText = "RUINS 3/3 | The temple is open (south-east). Defeat its guardian.";
        s4.message = "ALL RUNES LIT! The temple door opens, and something large wakes up inside...";
        s4.activate = new[] { p3.lit, guardian, escA, escB };
        s4.deactivate = new[] { p3.unlit, templeDoor };

        MissionStep s5 = NewStep(z, "Mission_Step5");
        s5.trackerId = "ruins";
        s5.nextText = "RUINS COMPLETE | Guardian defeated. Take the treasure in the temple.";
        s5.finishes = true;
        s5.message = "GUARDIAN DEFEATED! Treasure appeared in the temple. RUINS COMPLETE!";
        s5.activate = rewards;

        if (p1.wheel != null) HookStep(p1.wheel.GetComponent<InteractableObject>(), "onCompleted", s1);
        if (p2.wheel != null) HookStep(p2.wheel.GetComponent<InteractableObject>(), "onCompleted", s2);
        if (frag != null) HookStep(frag.GetComponent<MovableObject>(), "onDelivered", s3);
        if (p3.wheel != null) HookStep(p3.wheel.GetComponent<InteractableObject>(), "onCompleted", s4);
        if (guardian != null) HookStep(guardian.GetComponent<Health>(), "OnDeath", s5);
        foreach (MissionStep s in new[] { intro, s1, s2, s3, s4, s5 }) EditorUtility.SetDirty(s);

        // ----- signs and hints (every pad is at least 9 units from the next one) -----
        StationSign(z, 2.5f, -7f, "TEMPLE: OPENS AFTER ALL 3 RUNES", 180f);

        HintPad(z, 17f, -3f, "THE ANCIENT RUINS",
            "GOAL: wake the temple and defeat its guardian.\nHOW: this is a mission with five steps. The MISSIONS list on the left of the screen always shows the current one. 1) Light rune 1 (north-east). 2) Light rune 2 (north-west). 3) Carry the rune fragment (west) to the yellow disc. 4) Light rune 3 (south-west). 5) Enter the temple (south-east). Each step unlocks the next, and each pad tells you what to do.\nRESULT: a message appears at every step, and the temple door opens after the third rune.",
            "THE RUINS: A 5-STEP MISSION", 0f, 4f, 0f);
        HintPad(z, 18f, 10f, "STEP 1: RUNE 1",
            "GOAL: light the first rune pillar, the stone wheel to the west of this pad.\nHOW: two guards stand beside it. Deal with them first (press Z on one, hold left-click). Then walk up to the wheel and tap left-click twice, one troop each. The number counts down 2, 1, 0. At 0 it charges for 4 seconds. If a troop leaves, the charge restarts.\nRESULT: the obelisk behind the wheel lights up and rune 2 wakes up.",
            "<<< STEP 1: RUNE 1", 0f, 4f, 0f);
        HintPad(z, -3f, 17f, "STEP 2: RUNE 2",
            "GOAL: light the second pillar (the wheel to the south-west of this pad). It sleeps until rune 1 is lit.\nHOW: a warden guards it. About 5 seconds after troops attach to it, it glows yellow and then throws them off. Tap right-click during the glow to recall them, then send them back. Kill the warden first, then tap left-click twice at the wheel.\nRESULT: rune 2 lights up and a rune fragment appears in the west.",
            "STEP 2: RUNE 2 (WARDEN)", 0f, 4f, 0f);
        HintPad(z, -13f, 5f, "STEP 3: THE FRAGMENT",
            "GOAL: carry the rune fragment to the yellow disc in the south.\nHOW: the fragment appears south of this pad after rune 2 is lit. Walk up to it and tap left-click once. One troop carries it to the yellow disc. If you pressed Z on it, press X so it can move.\nRESULT: the fragment is placed and rune 3 wakes up.",
            "STEP 3: FRAGMENT TO THE YELLOW DISC", 0f, 4f, 0f);
        HintPad(z, -5f, -13f, "STEP 4: RUNE 3",
            "GOAL: light the third pillar (the wheel to the west of this pad). It needs 3 troops.\nHOW: three cultists guard it. Clear them first with a group (hold left-click). Then tap left-click 3 times at the wheel. The number counts 3, 2, 1, 0, and then it charges for 4 seconds.\nRESULT: all three runes burn, the temple door opens and its guardian wakes up.",
            "STEP 4: RUNE 3 (3 TROOPS)", 0f, 4f, 0f);
        HintPad(z, 6f, -6f, "STEP 5: THE TEMPLE",
            "GOAL: defeat the temple guardian and take the treasure.\nHOW: the door south of this pad opens when all three runes are lit. The guardian is a miniboss with 1500 health, 5 attach points and two escorts. It hits for 40, so troops die fast. Send a mixed group: melee fill the 5 points, ranged add safe damage. A lone front attacker gets countered, so go in as a group. Recall hurt troops and send fresh ones.\nRESULT: when it falls, treasure appears inside the temple and the mission is complete.");
    }

    // ---------------------------------------------------------------- rally points (recruit lost troops)

    private static void PlaceRally(string name, float wx, float wz, int reserve)
    {
        Transform r = Zone(name, wx, wz);
        GameObject pad = Prim(PrimitiveType.Cylinder, "RallyPad", r, new Vector3(0f, _groundY + 0.06f, 0f), new Vector3(7f, 0.04f, 7f), Mat("Rally", new Color(0.2f, 0.5f, 1f)), false);
        Material wood = Mat("FenceWood", new Color(0.55f, 0.36f, 0.18f));
        Prim(PrimitiveType.Cube, "FlagPole", r, new Vector3(0f, _groundY + 2.5f, 0f), new Vector3(0.2f, 5f, 0.2f), wood, false);
        Prim(PrimitiveType.Cube, "Flag", r, new Vector3(0.9f, _groundY + 4.3f, 0f), new Vector3(1.8f, 1.1f, 0.1f), Mat("RallyFlag", new Color(0.2f, 0.5f, 1f)), false);
        AddHint(pad.transform, "RALLY POINT",
            "GOAL: replace the troops you have lost.\nHOW: stand on the blue pad. Recruits join you automatically, up to the number you lost, from this point's limited reserve. They fill the troop type you lost the most of.\nRESULT: a banner shows how many joined. If your army is full, nothing is used. Recruits arrive without gear.", 4.5f);
        StationSign(r, 6.5f, 0f, "RALLY POINT: RECRUITS JOIN YOU");
        RallyPoint rp = r.gameObject.AddComponent<RallyPoint>();
        rp.reserve = reserve;
        rp.radius = 3.5f;
        EditorUtility.SetDirty(rp);
    }

    private static void BuildRallyPoints()
    {
        PlaceRally("Rally_BeforeGate", 14f, -6f, 6);
        PlaceRally("Rally_AfterGate", 29f, 28f, 6);
        PlaceRally("Rally_Ruins", -36f, 62f, 8);
        PlaceRally("Rally_BossApproach", 16f, 60f, 8);
    }

    private static GameObject FindDeep(string name)
    {
        foreach (Transform t in _root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t.gameObject;
        Debug.LogWarning("[Level] Not found: " + name);
        return null;
    }

    private static void HookVoid(Component src, string field, UnityEngine.Events.UnityAction call)
    {
        UnityEngine.Events.UnityEvent ev = FindEvent(src, field);
        if (ev == null) return;
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(ev, call);
        EditorUtility.SetDirty(src);
    }

    private static void HookString(Component src, string field, UnityEngine.Events.UnityAction<string> call, string arg)
    {
        UnityEngine.Events.UnityEvent ev = FindEvent(src, field);
        if (ev == null) return;
        UnityEditor.Events.UnityEventTools.AddStringPersistentListener(ev, call, arg);
        EditorUtility.SetDirty(src);
    }

    // ---------------------------------------------------------------- boss arena (end of the path)

    private static void BuildBossArena()
    {
        Transform z = PoiZone("BossArena");
        AddSpawn(z, 0f, -30f);

        EnemyData addData = MakeEnemyData("EnemyData_BossAdd", 150f, 8f, 12f, 1f, 1f, 3.8f);
        EnemyData escort = MakeEnemyData("EnemyData_BossEscort", 200f, 10f, 12f, 1f, 1f, 3.5f);

        // ----- octagon wall; the south side is the sealed door -----
        Transform wall = new GameObject("ArenaWall").transform;
        wall.SetParent(z, false);
        float R = 15f;
        GameObject door = null;
        for (int k = 0; k < 8; k++)
        {
            float a1 = (22.5f + k * 45f) * Mathf.Deg2Rad, a2 = (22.5f + (k + 1) * 45f) * Mathf.Deg2Rad;
            Vector2 p1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * R, p2 = new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * R;
            GameObject seg = FenceSegment(wall, k == 5 ? "BossDoor" : "Wall" + k, p1.x, p1.y, p2.x, p2.y);
            if (k == 5) door = seg;   // segment between 247.5 and 292.5 degrees = south
        }
        if (door != null)
            Prim(PrimitiveType.Cube, "DoorBoards", door.transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.4f, 2.4f, 11f), Mat("GateWood", new Color(0.35f, 0.2f, 0.1f)), false);

        // ----- torches (decoration) -----
        foreach (Vector2 tp in new[] { new Vector2(-9f, -6f), new Vector2(9f, -6f), new Vector2(-9f, 8f), new Vector2(9f, 8f) })
        {
            Prim(PrimitiveType.Cube, "TorchPost", z, new Vector3(tp.x, _groundY + 1.2f, tp.y), new Vector3(0.4f, 2.4f, 0.4f), Mat("FenceWood", new Color(0.55f, 0.36f, 0.18f)), false);
            GameObject f = Prim(PrimitiveType.Cube, "TorchFlame", z, new Vector3(tp.x, _groundY + 2.7f, tp.y), new Vector3(0.7f, 0.9f, 0.7f), Mat("Flame", new Color(1f, 0.55f, 0.1f)), false);
            GameObject lg = new GameObject("Light");
            lg.transform.SetParent(f.transform, false);
            Light l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.6f, 0.25f);
            l.range = 12f;
            l.intensity = 2f;
        }

        // ----- three seal stones on the approach -----
        GameObject[] on = new GameObject[3], off = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            float x = -7f + i * 7f;
            off[i] = Prim(PrimitiveType.Cube, "SealStoneOff" + i, z, new Vector3(x, _groundY + 1.6f, -22f), new Vector3(1.6f, 3.2f, 1.6f), Mat("RuneOff", new Color(0.25f, 0.25f, 0.3f)), true);
            on[i] = Prim(PrimitiveType.Cube, "SealStoneOn" + i, z, new Vector3(x, _groundY + 1.6f, -22f), new Vector3(1.6f, 3.2f, 1.6f), Mat("RuneOn", new Color(0.3f, 0.9f, 1f)), false);
            GameObject lg = new GameObject("Glow");
            lg.transform.SetParent(on[i].transform, false);
            lg.transform.localPosition = new Vector3(0f, 2f, 0f);
            Light l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.4f, 0.9f, 1f);
            l.range = 10f;
            l.intensity = 2.5f;
            on[i].SetActive(false);
        }

        GameObject gateObj = new GameObject("BossGate");
        gateObj.transform.SetParent(z, false);
        BossGate bg = gateObj.AddComponent<BossGate>();
        bg.needed = 3;
        bg.door = door;
        bg.stonesOn = on;
        bg.stonesOff = off;
        EditorUtility.SetDirty(bg);

        // ----- boss, escorts, phase-2 reinforcements -----
        GameObject boss = AddBigEnemy(z, "Boss", 0f, 5f, _boss, 6, 2.2f, 3.4f, 4.8f, 6.5f);
        FaceYaw(boss, 180f);
        AddEnemy(z, "Boss_EscortA", -7f, 1f, escort, 3);
        AddEnemy(z, "Boss_EscortB", 7f, 1f, escort, 3);
        GameObject addA = AddEnemy(z, "Boss_AddA", -9f, 8f, addData, 3);
        GameObject addB = AddEnemy(z, "Boss_AddB", 9f, 8f, addData, 3);
        GameObject addC = AddEnemy(z, "Boss_AddC", 0f, 11f, addData, 3);
        GameObject[] adds = { addA, addB, addC };
        foreach (GameObject g in adds) if (g != null) g.SetActive(false);

        GameObject ph = new GameObject("Boss_Phase2");
        ph.transform.SetParent(z, false);
        BossPhase bp = ph.AddComponent<BossPhase>();
        bp.boss = boss != null ? boss.GetComponent<Health>() : null;
        bp.threshold = 0.5f;
        bp.activate = adds;
        bp.message = "THE BOSS ROARS! Reinforcements charge in from the shadows.";
        EditorUtility.SetDirty(bp);

        GameObject vic = new GameObject("VictoryScreen");
        vic.transform.SetParent(z, false);
        VictoryScreen vs = vic.AddComponent<VictoryScreen>();
        if (boss != null) HookVoid(boss.GetComponent<Health>(), "OnDeath", vs.Show);

        // ----- wire the three seals to the earlier areas -----
        GameObject key = FindDeep("Yard_Key");
        if (key != null) HookString(key.GetComponent<MovableObject>(), "onDelivered", bg.Seal, "key");

        GameObject ring = FindDeep("Outpost_Ring");
        if (ring != null)
        {
            GameObject ws = new GameObject("Seal_OutpostRing");
            ws.transform.SetParent(z, false);
            WaypointSeal w = ws.AddComponent<WaypointSeal>();
            w.target = ring.GetComponent<Waypoint>();
            w.gate = bg;
            w.id = "outpost";
            EditorUtility.SetDirty(w);
        }

        GameObject tg = FindDeep("Temple_Guardian");
        if (tg != null) HookString(tg.GetComponent<Health>(), "OnDeath", bg.Seal, "ruins");

        // ----- signs and hints -----
        StationSign(z, 0f, -26f, "3 SEALS OPEN THE ARENA", 0f);
        HintPad(z, 0f, -30f, "THE BOSS ARENA",
            "GOAL: defeat the boss and finish the level.\nHOW: the door is sealed. Three stones in front of it light up as you break seals elsewhere in the jungle: 1) carry the key to the yellow disc in the Carry Yard, 2) capture the ring at the Outpost, 3) defeat the temple guardian in the Ruins. The MISSIONS list on the left shows which are done.\nRESULT: after the third seal the door opens. Rally with recruits at the blue pad first.",
            "SEALED: BREAK 3 SEALS", 6f, 0f, 0f);
        HintPad(z, 0f, -10f, "THE BOSS",
            "GOAL: bring the boss down. It has 3000 health, 6 attach points and hits for 60, so troops die fast.\nHOW: send a mixed group and fill all 6 attach points. About 5 seconds after troops attach, the boss glows yellow and throws them off, so tap right-click to recall them during the glow, then send them back. Attack from behind while it is busy for triple damage. At half health, 3 reinforcements join. Stun (N) is a test key only.\nRESULT: when it falls, a victory screen shows your time and surviving troops.");
    }
}
