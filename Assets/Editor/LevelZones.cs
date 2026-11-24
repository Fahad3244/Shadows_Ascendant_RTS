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

    private static void AddPoi(string name, float cx, float cz, float r, float jx, float jz)
    {
        Pois.Add(new Poi { name = name, center = new Vector2(cx, cz), radius = r, junction = new Vector2(jx, jz) });
    }

    // ADD A NEW CLEARING: one AddPoi line here + one BuildXxx() method + one line in BuildZones().
    // junction = the point on the main path where the trail starts.
    private static void DefinePois()
    {
        Pois.Clear();
        AddPoi("GoldHollow", -58f, -64f, 16f, 0f, -66f);
        AddPoi("StashGlade", 58f, -64f, 16f, 0f, -54f);
        AddPoi("SentryGlade", 62f, -10f, 16f, 18f, -8f);
        AddPoi("Roadblock", -58f, -12f, 16f, 12f, -15f);
        AddPoi("CarryYard", -60f, 48f, 17f, 14f, 48f);
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
        // Step 19: BuildOutpost();
        // Step 20: BuildRuins();
        // Step 21: BuildBossArena();
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
        FieldInfo f = src.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
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
        FenceSegment(g, "WallWest", -129f, 0f, -4f, 0f);
        FenceSegment(g, "WallEast", 4f, 0f, 79f, 0f);

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
}
