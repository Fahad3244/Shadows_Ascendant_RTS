using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TestArenaBuilder
{
    private const float Gap = 140f;

    private static Transform _root;
    private static float _playerY = 0.1f;
    private static float _groundY = 0.72f;
    private static EnemyData _dummy, _weak, _brute, _killer;
    private static readonly List<Transform> Spawns = new List<Transform>();
    private static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();

    private static readonly string[] OldNames =
    {
        "Enemies", "GoldMine", "GoldMine (1)", "Waypoint", "Waypoint (1)",
        "TestObject", "TestObject (1)", "TestObject (2)", "TestObject_Breakable", "TestObject_Barrel",
        "TestObject_Fence", "TestObject_Barricade", "TestGold", "TestGear", "TestGear (1)", "TestPotion",
        "Interactable", "Movable", "Retrievable", "RetrieveDestination",
        "Blockade (1)", "Blockade (2)", "Blockade (3)", "Blockade (4)"
    };

    [MenuItem("Tools/Test Arena/Build Arena")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!EditorUtility.DisplayDialog("Test Arena",
            "This changes the open scene (" + scene.name + "): it disables the old test objects and Flora, and builds the arena. " +
            "Duplicate the scene first if you want to keep the original. Continue?", "Build", "Cancel")) return;

        Mats.Clear();
        Spawns.Clear();

        GameObject oldArena = GameObject.Find("TestArena");
        if (oldArena != null) UnityEngine.Object.DestroyImmediate(oldArena);

        EnsureFolder("Assets/TestArena");
        EnsureFolder("Assets/TestArena/Materials");

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player != null) _playerY = player.transform.position.y;
        GameObject groundGo = GameObject.Find("World/Ground");
        if (groundGo != null)
        {
            Renderer gr = groundGo.GetComponent<Renderer>();
            if (gr != null) _groundY = gr.bounds.max.y;
        }

        _dummy = MakeEnemyData("EnemyData_Dummy", 600f, 0f, 0.5f, 1f, 1.5f, 1f);
        _weak = MakeEnemyData("EnemyData_Weak", 150f, 3f, 8f, 2f, 1.5f, 3.5f);
        _brute = MakeEnemyData("EnemyData_Brute", 500f, 30f, 8f, 2f, 1f, 3.5f);
        _killer = MakeEnemyData("EnemyData_Killer", 100f, 200f, 5f, 1f, 0.6f, 3.5f);

        _root = new GameObject("TestArena").transform;

        BuildHub();
        BuildS1();
        BuildS2();
        BuildS3();
        BuildS4();
        BuildS5();
        BuildS6();
        BuildS7();

        GameObject helpPanel, hintPanel;
        TextMeshProUGUI hintLabel;
        BuildHud(out helpPanel, out hintPanel, out hintLabel);

        GameObject g = new GameObject("TestArenaHelper");
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

        ArchiveOldContent();
        PrepareScene();

        if (player != null && Spawns.Count > 0) player.transform.position = Spawns[0].position;

        EditorSceneManager.MarkSceneDirty(scene);
        if (!string.IsNullOrEmpty(scene.path))
        {
            EditorSceneManager.SaveScene(scene);
            List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;
            foreach (EditorBuildSettingsScene s in list)
                if (s.path == scene.path) { s.enabled = true; found = true; }
            if (!found) list.Add(new EditorBuildSettingsScene(scene.path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        TryBake();
        EditorUtility.DisplayDialog("Test Arena",
            "Arena built. If a NavMesh bake progress bar appears, wait for it to finish, then press Ctrl+S. Then press Play and press F2.", "OK");
    }

    // ---------------------------------------------------------------- stations

    private static void BuildHub()
    {
        Transform hub = Station("Hub", 0f, 0f, "HUB",
            "Press F2-F8 to visit the test stations: F2 Targeting, F3 Sweep, F4 Hostiles, F5 Carry + Waypoints, F6 Breakables, F7 Pickups, F8 Failures. " +
            "F1 returns here. H hides the controls. F12 resets everything. In a station, stand on a green pad to read what to test.");
        Disc(hub, "HubDisc", 0f, -27f, 3f, new Color(0.3f, 0.6f, 1f));
    }

    private static void BuildS1()
    {
        Transform st = Station("S1_Targeting", -Gap, Gap, "S1  TARGETING",
            "How troops choose targets. Walk to a green pad and read the hint.");

        Lane(st, -45f, "S1-A  ENEMY FIRST",
            "TAP left-click sends 1 troop, HOLD sends all.\nEXPECT: troops ignore the closer crate and attack the ENEMY first. When the enemy dies they take the crate.");
        Breakable(st, "S1A_Crate", -45f, -9f, false, 400f);
        AddEnemy(st, "S1A_Enemy", -45f, -3f, _dummy);

        Lane(st, -15f, "S1-B  SCAN WHILE MOVING",
            "TAP left-click.\nEXPECT: the troop starts toward the crate. The enemy is out of range at first. When it comes into range the troop turns to the ENEMY and attaches there.");
        Breakable(st, "S1B_Crate", -15f, -2f, false, 400f);
        AddEnemy(st, "S1B_Enemy", -3f, 0f, _dummy);

        Lane(st, 15f, "S1-C  CHOSEN TARGET",
            "Press Z until the crate (far) has an outline, then TAP left-click. Press X to clear the target.\nEXPECT: troops walk past the closer enemy and go ONLY to the chosen crate. After X, a new dispatch goes to the enemy.");
        AddEnemy(st, "S1C_Enemy", 15f, -8f, _dummy);
        Breakable(st, "S1C_Crate", 15f, 5f, false, 400f);

        Lane(st, 45f, "S1-D  NEAREST FIRST + FULL",
            "Press 3 (Melee), then HOLD left-click.\nEXPECT: 3 troops fill the NEAR chest (its number counts down to 0). The other 2 go to the FAR chest.");
        Breakable(st, "S1D_ChestNear", 45f, -8f, true, 400f, 3);
        Breakable(st, "S1D_ChestFar", 45f, 2f, true, 400f, 3);
    }

    private static void BuildS2()
    {
        Transform st = Station("S2_Sweep", 0f, Gap, "S2  SWEEP",
            "Sweep = direct control. Hold LEFT + RIGHT mouse together and move the mouse.");

        Lane(st, -30f, "S2-A  SWEEP IGNORES THINGS",
            "FIRST hold left-click to send the troops out. THEN hold LEFT + RIGHT mouse together and move the mouse through the crates and the enemy.\nEXPECT: troops follow the cursor and do NOT stop for anything on the way. After you release they may pick a nearby target. That is normal.");
        Breakable(st, "S2A_CrateL", -33f, -8f, false, 400f);
        Breakable(st, "S2A_CrateR", -27f, -5f, false, 400f);
        Breakable(st, "S2A_CrateM", -30f, -2f, false, 400f);
        AddEnemy(st, "S2A_Enemy", -30f, -4f, _dummy);

        Lane(st, 0f, "S2-B  COMPARE",
            "First TAP or HOLD left-click: troops stop at the crates and the enemy. Hold RIGHT-click to recall. Now SWEEP through the same objects.\nEXPECT: dispatch stops at things, sweep ignores them.");
        Breakable(st, "S2B_CrateL", -3f, -8f, false, 400f);
        Breakable(st, "S2B_CrateR", 3f, -5f, false, 400f);
        Breakable(st, "S2B_CrateM", 0f, -2f, false, 400f);
        AddEnemy(st, "S2B_Enemy", 0f, -4f, _dummy);

        Lane(st, 30f, "S2-C  WALL",
            "FIRST hold left-click to send the troops out. THEN hold LEFT + RIGHT mouse together and sweep the cursor to the far side of the wall.\nEXPECT: troops walk AROUND the wall. Pathfinding detours are normal.");
        Wall(st, "S2C_Wall", 30f, -6f, 14f, 1f);
        Breakable(st, "S2C_Crate", 30f, 6f, false, 400f);
    }

    private static void BuildS3()
    {
        Transform st = Station("S3_Hostiles", Gap, Gap, "S3  HOSTILES",
            "Attach points, front attackers and health symbols. Press 3 (Melee) for 5 troops.");

        Lane(st, -30f, "S3-A  FRONT ATTACK",
            "Press 3 (Melee), press Z to target the enemy, HOLD left-click.\nEXPECT: 2 troops attach in the ring, the others attack from the FRONT. Ring troops show damage 10, front troops 5. Recall a ring troop and a front troop moves into the slot.");
        AddEnemy(st, "S3A_Enemy", -30f, -2f, _dummy, 2);

        Lane(st, 0f, "S3-B  SLOT NUMBER + HEALTH BAR",
            "Look at the enemy: no symbol. Press Z: number + health bar appear. Send troops one by one.\nEXPECT: number shows free slots (5 to 0, red at 0). Symbol hides again a few seconds after you clear the target and the troops leave.");
        AddEnemy(st, "S3B_Enemy", 0f, -2f, _dummy, 5);

        Lane(st, 30f, "S3-C  REAL FIGHT",
            "HOLD left-click to send everyone.\nEXPECT: the brute hits hard. Troops die within a few hits (a 'Lost!' message shows) while the rest keep attacking it.");
        AddEnemy(st, "S3C_Enemy", 30f, -2f, _brute, 3);
    }

    private static void BuildS4()
    {
        Transform st = Station("S4_Carry", -Gap, 0f, "S4  CARRY + WAYPOINTS",
            "Movable objects, waypoints, capture and redirect.");

        Lane(st, -45f, "S4-A  CARRY + SPEED",
            "TAP left-click 3 times (one troop each). The label is grey 3, turns blue at 3 troops and the object moves to the waypoint. Send more troops (max 6): it moves faster.\nEXPECT: delivered inside the ring, the label shows Placed, troops return. A cyan disc shows under the object while it is Active inside the waypoint ring.");
        Movable(st, "S4A_Movable", -45f, -5f);
        WP(st, "S4A_Waypoint", -45f, 25f, 0);

        Lane(st, -15f, "S4-B  STEER AROUND DANGER",
            "HOLD left-click so troops pick up the object. While it carries, press Z to target it, then SWEEP (LEFT + RIGHT mouse) to steer it around the three enemies.\nEXPECT: the object follows your cursor. After you release, it resumes toward the waypoint.");
        Movable(st, "S4B_Movable", -15f, -5f);
        WP(st, "S4B_Waypoint", -15f, 30f, 0);
        AddEnemy(st, "S4B_EnemyL", -19f, 14f, _dummy);
        AddEnemy(st, "S4B_EnemyM", -15f, 15f, _dummy);
        AddEnemy(st, "S4B_EnemyR", -11f, 14f, _dummy);

        Lane(st, 15f, "S4-C  WAYPOINT LOST",
            "HOLD left-click and wait until the object is carried. Press K: the left waypoint flips to the enemy. Press L for the right one. Press K / L again to restore.\nEXPECT: K redirects the object to the other waypoint. K and L together make it STOP. Restore one and it continues.");
        Movable(st, "S4C_Movable", 15f, -5f);
        Waypoint w1 = WP(st, "S4C_WaypointLeft", 5f, 25f, 0);
        Waypoint w2 = WP(st, "S4C_WaypointRight", 25f, 25f, 0);
        GameObject keys = new GameObject("S4C_Keys");
        keys.transform.SetParent(st, false);
        AddKey(keys.transform, KeyCode.K, null, new[] { w1 });
        AddKey(keys.transform, KeyCode.L, null, new[] { w2 });

        Lane(st, 45f, "S4-D  CAPTURE",
            "Walk into the red ring ahead, press ` (All) then Q to place a guard flag inside it with your troops.\nEXPECT: the ring turns blue after a few seconds (more troops = faster). The far blue ring has an enemy inside: it slowly turns RED.");
        WP(st, "S4D_WaypointEnemyOwned", 45f, 0f, 1);
        WP(st, "S4D_WaypointPlayerOwned", 45f, 30f, 0);
        AddEnemy(st, "S4D_Capturer", 45f, 30f, _dummy);

        Lane(st, 70f, "S4-E  GOLD MINE CARRY",
            "TAP left-click 2-3 times.\nEXPECT: troops attach to the mine, then each carries a load slowly to the nearest waypoint ring, deposits it and returns to you.");
        Clone("GoldMine", st, 70f, -4f, "S4E_GoldMine");
        WP(st, "S4E_Waypoint", 70f, 25f, 0);
    }

    private static void BuildS5()
    {
        Transform st = Station("S5_Breakables", Gap, 0f, "S5  BREAKABLES",
            "Small, large and indestructible objects, and a wheel.");

        Lane(st, -45f, "S5-A  SMALL BARREL",
            "Press ` (All) and HOLD left-click.\nEXPECT: ALL troops attack from every side, in rings. No slot number. It breaks fast.");
        Breakable(st, "S5A_Barrel", -45f, -4f, false, 200f);

        Lane(st, -15f, "S5-B  LARGE FENCE",
            "Press ` (All) and HOLD left-click.\nEXPECT: only 5 troops attach (the number counts down). It has 250 health and takes longer.");
        Breakable(st, "S5B_Fence", -15f, -4f, true, 250f, 5, false, "TestObject_Fence");

        Lane(st, 15f, "S5-C  INDESTRUCTIBLE",
            "Press Z to target the barricade, then TAP left-click.\nEXPECT: troops arrive, pause, and return. No damage. A plain dispatch ignores it.");
        Breakable(st, "S5C_Barricade", 15f, -4f, true, 250f, 5, true, "TestObject_Barricade");

        Lane(st, 45f, "S5-D  WHEEL + DOOR",
            "TAP left-click twice (2 troops): they cluster and nothing happens. TAP a third time.\nEXPECT: at 3 troops the wheel operates for 3 seconds and the door opens. The troops then return.");
        GameObject wheel = Clone("Interactable", st, 45f, -4f, "S5D_Wheel");
        if (wheel != null)
        {
            Set<AttachableTarget>(wheel, "category", (int)InteractionCategory.Interactable);
            Set<AttachableTarget>(wheel, "requiredUnitsToMove", 3);
            Set<AttachableTarget>(wheel, "maxAttachSlots", 5);
            Set<InteractableObject>(wheel, "operateDuration", 3f);
            Set<InteractableObject>(wheel, "singleUse", true);
        }
        GameObject door = Prim(PrimitiveType.Cube, "S5D_Door", st, new Vector3(45f, _groundY + 1.5f, 14f), new Vector3(8f, 3f, 0.5f), Mat("Door", new Color(0.5f, 0.3f, 0.1f)), true);
        NavMeshObstacle ob = door.AddComponent<NavMeshObstacle>();
        ob.shape = NavMeshObstacleShape.Box;
        ob.carving = true;
        ob.size = Vector3.one;
        if (wheel != null)
        {
            GameObject link = new GameObject("S5D_DoorLink");
            link.transform.SetParent(st, false);
            DoorLink dl = link.AddComponent<DoorLink>();
            dl.wheel = wheel.GetComponent<AttachableTarget>();
            dl.door = door;
            EditorUtility.SetDirty(dl);
        }
    }

    private static void BuildS6()
    {
        Transform st = Station("S6_Pickups", -Gap, -Gap, "S6  PICKUPS",
            "Gold, potions, gear and a retrievable object.");

        Lane(st, -45f, "S6-A  GOLD + POTION",
            "TAP left-click twice.\nEXPECT: each troop grabs an item and carries it back to you. A popup like +10 Gold appears over your head when it arrives.");
        PickupItemAt(st, "TestGold", "S6A_Gold", -49f, -3f, 0);
        PickupItemAt(st, "TestPotion", "S6A_Potion", -41f, -3f, 0);

        Lane(st, -15f, "S6-B  GEAR",
            "Press 3 (Melee), TAP left-click: that troop equips the gear (cyan marker). Then press Z to target the dummy and TAP again with the same group.\nEXPECT: the equipped troop deals 15 per hit, other troops 10. Builders carry the gear back instead.");
        PickupItemAt(st, "TestGear", "S6B_Gear", -15f, -3f, 1);
        AddEnemy(st, "S6B_Dummy", -15f, 14f, _dummy);

        Lane(st, 15f, "S6-C  RETRIEVABLE",
            "TAP left-click once, then more if needed (1 troop is enough). Press Z to target it and SWEEP to steer.\nEXPECT: the label turns blue and it is carried along a path to the green disc. At the disc it shows Placed.");
        GameObject ret = Clone("Retrievable", st, 15f, -4f, "S6C_Retrievable");
        GameObject dest = new GameObject("S6C_Destination");
        dest.transform.SetParent(st, false);
        dest.transform.localPosition = new Vector3(15f, _groundY, 22f);
        GameObject marker = Disc(dest.transform, "Marker", 0f, 0f, 3f, new Color(0.2f, 1f, 0.4f));
        marker.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        marker.SetActive(false);
        if (ret != null)
        {
            Set<AttachableTarget>(ret, "category", (int)InteractionCategory.Retrievable);
            Set<AttachableTarget>(ret, "requiredUnitsToMove", 1);
            Set<AttachableTarget>(ret, "maxAttachSlots", 3);
            Set<MovableObject>(ret, "fixedDestination", dest.transform);
            Set<MovableObject>(ret, "destinationMarker", marker);
        }

        Lane(st, 45f, "S6-D  STACKING POPUPS",
            "Press ` (All) and HOLD left-click.\nEXPECT: three troops each carry one item. The three popups stack above your head instead of overlapping.");
        PickupItemAt(st, "TestGold", "S6D_GoldA", 41f, -3f, 0);
        PickupItemAt(st, "TestPotion", "S6D_Potion", 45f, -3f, 0);
        PickupItemAt(st, "TestGold", "S6D_GoldB", 49f, -3f, 0);

        Lane(st, 70f, "S6-E  CARRIER KILLED",
            "TAP left-click once. As soon as the troop grabs the gold (it disappears), press M: a killer enemy appears on its way back.\nEXPECT: the troop dies ('Lost!'), the gold DROPS where it fell and can be collected again. Press M to remove the enemy.");
        PickupItemAt(st, "TestGold", "S6E_Gold", 70f, 12f, 0);
        GameObject killer = AddEnemy(st, "S6E_Killer", 70f, -2f, _killer);
        if (killer != null)
        {
            killer.SetActive(false);
            GameObject kKeys = new GameObject("S6E_Keys");
            kKeys.transform.SetParent(st, false);
            AddKey(kKeys.transform, KeyCode.M, new[] { killer }, null);
        }
    }

    private static void BuildS7()
    {
        Transform st = Station("S7_Failures", 0f, -Gap, "S7  FAILURE + INTERRUPTION",
            "Troops should never get stuck. Press F12 to reset after each test.");

        Lane(st, -45f, "S7-A  TARGET BREAKS",
            "Press ` (All) and HOLD left-click.\nEXPECT: the first crate breaks fast. Troops pause, go to the second crate, then return to you when it breaks too.");
        Breakable(st, "S7A_CrateWeak", -45f, -5f, false, 60f);
        Breakable(st, "S7A_CrateStrong", -45f, 5f, false, 400f);

        Lane(st, -15f, "S7-B  EMPTY AREA",
            "HOLD left-click (nothing is nearby).\nEXPECT: troops spread out over the blue disc, pause, then return to you.");
        Disc(st, "S7B_Area", -15f, 10f, 6f, new Color(0.3f, 0.6f, 1f));

        Lane(st, 15f, "S7-C  UNREACHABLE",
            "Press Z to target the crate inside the pen, TAP left-click.\nEXPECT: troops stop at the wall, pause, and return. A plain dispatch ignores the crate.");
        Wall(st, "S7C_WallN", 15f, 14f, 9f, 1f);
        Wall(st, "S7C_WallS", 15f, 6f, 9f, 1f);
        Wall(st, "S7C_WallE", 19f, 10f, 1f, 9f);
        Wall(st, "S7C_WallW", 11f, 10f, 1f, 9f);
        Breakable(st, "S7C_Crate", 15f, 10f, false, 200f);

        Lane(st, 45f, "S7-D  COMMITTED TROOPS",
            "HOLD left-click so troops attach to the chest. Then press E to wake the enemy next to it.\nEXPECT: troops stay on the chest and keep attacking. They are not pulled away by the new enemy.");
        Breakable(st, "S7D_Chest", 45f, -3f, true, 800f, 3);
        GameObject enemy = AddEnemy(st, "S7D_Enemy", 50f, 0f, _weak);
        if (enemy != null)
        {
            enemy.SetActive(false);
            GameObject keys = new GameObject("S7D_Keys");
            keys.transform.SetParent(st, false);
            AddKey(keys.transform, KeyCode.E, new[] { enemy }, null);
        }

        Lane(st, 70f, "S7-E  CARRY BLOCKED",
            "HOLD left-click so 3+ troops grab the object. Its waypoint is walled in.\nEXPECT: the object does NOT move (Blocked) and the troops wait on it. Press B to open the walls: it starts moving. Press B again to close them.");
        Movable(st, "S7E_Movable", 70f, -5f);
        WP(st, "S7E_Waypoint", 70f, 28f, 0);
        GameObject pen = new GameObject("S7E_Pen");
        pen.transform.SetParent(st, false);
        Wall(pen.transform, "S7E_WallN", 70f, 32f, 9f, 1f);
        Wall(pen.transform, "S7E_WallS", 70f, 24f, 9f, 1f);
        Wall(pen.transform, "S7E_WallE", 74f, 28f, 1f, 9f);
        Wall(pen.transform, "S7E_WallW", 66f, 28f, 1f, 9f);
        GameObject pKeys = new GameObject("S7E_Keys");
        pKeys.transform.SetParent(st, false);
        AddKey(pKeys.transform, KeyCode.B, new[] { pen }, null);
    }

    // ---------------------------------------------------------------- builders

    private static Transform Station(string name, float wx, float wz, string title, string desc)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(_root, false);
        g.transform.position = new Vector3(wx, 0f, wz);
        GameObject sp = new GameObject("Spawn");
        sp.transform.SetParent(g.transform, false);
        sp.transform.localPosition = new Vector3(0f, _playerY, -27f);
        Spawns.Add(sp.transform);
        AddHint(sp.transform, title, desc, 5f);
        Sign(g.transform, title, 0f, -24f, 5.5f);
        return g.transform;
    }

    private static void Lane(Transform st, float x, string title, string body)
    {
        GameObject pad = Prim(PrimitiveType.Cylinder, "Pad", st, new Vector3(x, _groundY + 0.03f, -15f), new Vector3(3f, 0.025f, 3f), Mat("Pad", new Color(0.2f, 0.9f, 0.3f)), false);
        AddHint(pad.transform, title, body, 3f);
        Sign(st, title, x, -18f, 3.5f);
    }

    private static void AddHint(Transform t, string title, string body, float radius)
    {
        ArenaHintZone z = t.gameObject.AddComponent<ArenaHintZone>();
        z.title = title;
        z.body = body;
        z.radius = radius;
        EditorUtility.SetDirty(z);
    }

    private static void Sign(Transform parent, string text, float x, float z, float y)
    {
        GameObject s = new GameObject("Sign");
        s.transform.SetParent(parent, false);
        s.transform.localPosition = new Vector3(x, y, z);
        Prim(PrimitiveType.Cube, "Board", s.transform, Vector3.zero, new Vector3(10f, 1.8f, 0.2f), Mat("Board", new Color(0.07f, 0.09f, 0.13f)), false);
        GameObject t = new GameObject("Text", typeof(RectTransform), typeof(TextMeshPro));
        t.transform.SetParent(s.transform, false);
        t.transform.localPosition = new Vector3(0f, 0f, -0.15f);
        t.GetComponent<RectTransform>().sizeDelta = new Vector2(9.6f, 1.5f);
        TextMeshPro tmp = t.GetComponent<TextMeshPro>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 1f;
        tmp.fontSizeMax = 8f;
        tmp.color = Color.white;
    }

    private static GameObject Breakable(Transform st, string name, float x, float z, bool large, float hp,
        int slots = -1, bool indestructible = false, string template = null)
    {
        string tpl = template ?? (large ? "TestObject_Breakable" : "TestObject_Barrel");
        GameObject go = Clone(tpl, st, x, z, name);
        if (go == null) return null;
        Set<BreakableObject>(go, "size", large ? 1 : 0);
        Set<BreakableObject>(go, "maxHealth", hp);
        Set<BreakableObject>(go, "indestructible", indestructible);
        Set<AttachableTarget>(go, "category", (int)InteractionCategory.Breakable);
        Set<AttachableTarget>(go, "requiredUnitsToMove", 0);
        if (slots > 0) Set<AttachableTarget>(go, "maxAttachSlots", slots);
        return go;
    }

    private static GameObject AddEnemy(Transform st, string name, float x, float z, EnemyData data, int slots = 5)
    {
        GameObject e = Clone("Enemy", st, x, z, name);
        if (e == null) return null;
        Set<EnemyBase>(e, "enemyData", data);
        Set<AttachableTarget>(e, "maxAttachSlots", slots);
        Set<AttachableTarget>(e, "requiredUnitsToMove", 0);
        return e;
    }

    private static GameObject Movable(Transform st, string name, float x, float z)
    {
        GameObject go = Clone("Movable", st, x, z, name);
        if (go == null) return null;
        Set<AttachableTarget>(go, "category", (int)InteractionCategory.Movable);
        Set<AttachableTarget>(go, "requiredUnitsToMove", 3);
        Set<AttachableTarget>(go, "maxAttachSlots", 6);
        Set<AttachableTarget>(go, "minMoveSpeed", 2f);
        Set<AttachableTarget>(go, "maxMoveSpeed", 5f);
        Set<MovableObject>(go, "fixedDestination", null);
        GameObject ring = Prim(PrimitiveType.Cylinder, "ActiveRing", go.transform, new Vector3(0f, -0.48f, 0f),
            new Vector3(3f, 0.01f, 3f), Mat("Active", new Color(0.2f, 0.9f, 1f)), false);
        ring.SetActive(false);
        Set<MovableObject>(go, "activeVisual", ring);
        return go;
    }

    private static Waypoint WP(Transform st, string name, float x, float z, int owner)
    {
        GameObject go = Clone("Waypoint", st, x, z, name);
        if (go == null) return null;
        Set<Waypoint>(go, "owner", owner);
        Set<Waypoint>(go, "debugFakePlayerUnits", 0);
        Set<Waypoint>(go, "debugFakeEnemyUnits", 0);
        return go.GetComponent<Waypoint>();
    }

    private static void PickupItemAt(Transform st, string template, string name, float x, float z, int mode)
    {
        GameObject go = Clone(template, st, x, z, name);
        if (go == null) return;
        Set<PickupItem>(go, "mode", mode);
    }

    private static void AddKey(Transform parent, KeyCode key, GameObject[] toggles, Waypoint[] flips)
    {
        GameObject g = new GameObject("Key_" + key);
        g.transform.SetParent(parent, false);
        KeyEventTrigger k = g.AddComponent<KeyEventTrigger>();
        k.key = key;
        k.toggleObjects = toggles ?? new GameObject[0];
        k.flipWaypoints = flips ?? new Waypoint[0];
        EditorUtility.SetDirty(k);
    }

    private static GameObject Wall(Transform st, string name, float x, float z, float sx, float sz)
    {
        GameObject g = Prim(PrimitiveType.Cube, name, st, new Vector3(x, _groundY + 1f, z), new Vector3(sx, 2f, sz), Mat("Wall", new Color(0.35f, 0.35f, 0.4f)), true);
        NavMeshObstacle o = g.AddComponent<NavMeshObstacle>();
        o.shape = NavMeshObstacleShape.Box;
        o.carving = true;
        o.center = Vector3.zero;
        o.size = Vector3.one;
        return g;
    }

    private static GameObject Disc(Transform parent, string name, float x, float z, float radius, Color c)
    {
        return Prim(PrimitiveType.Cylinder, name, parent, new Vector3(x, _groundY + 0.03f, z), new Vector3(radius * 2f, 0.02f, radius * 2f), Mat("Disc_" + ColorUtility.ToHtmlStringRGB(c), c), false);
    }

    private static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool collider)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        g.transform.localScale = scale;
        if (mat != null) g.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider)
        {
            Collider c = g.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c);
        }
        return g;
    }

    private static GameObject Clone(string template, Transform parent, float x, float z, string newName)
    {
        GameObject t = FindAny(template, true);
        if (t == null) return null;
        GameObject c = UnityEngine.Object.Instantiate(t);
        c.name = newName;
        c.transform.SetParent(parent, false);
        c.transform.localPosition = new Vector3(x, t.transform.position.y, z);
        c.transform.localRotation = Quaternion.identity;
        c.SetActive(true);
        return c;
    }

    // ---------------------------------------------------------------- HUD

    private static void BuildHud(out GameObject helpPanel, out GameObject hintPanel, out TextMeshProUGUI hintLabel)
    {
        GameObject canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null)
        {
            canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        }
        Transform canvas = canvasGo.transform;

        foreach (string n in new[] { "ArenaHelpPanel", "ArenaHintPanel" })
        {
            Transform old = canvas.Find(n);
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        }

        helpPanel = UiPanel(canvas, "ArenaHelpPanel", new Vector2(1f, 1f), new Vector2(-10f, -10f), new Vector2(560f, 330f));
        TextMeshProUGUI help = UiText(helpPanel.transform, 15f);
        help.text =
            "<b>CONTROLS</b>\n" +
            "WASD move  |  hold RIGHT mouse + move = rotate camera\n" +
            "`  All    1 Builder    2 Ranged    3 Melee    4 Medic\n" +
            "LEFT click: tap = send 1 troop, hold = send the group\n" +
            "RIGHT click: tap = recall 1, hold = recall the group\n" +
            "LEFT + RIGHT together = SWEEP (steer troops directly)\n" +
            "Z = target nearest / next    X = clear target\n" +
            "Q = guard flag    Ctrl tap = run stored path\n" +
            "K / L = flip a waypoint (S4)   E = wake enemy (S7)   M = killer (S6)   B = open walls (S7)\n" +
            "F1 Hub, F2-F8 stations 1-7   H hide help   F12 reset\n" +
            "Esc frees the mouse cursor";

        hintPanel = UiPanel(canvas, "ArenaHintPanel", new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(700f, 190f));
        hintLabel = UiText(hintPanel.transform, 18f);
        hintLabel.text = "";
        hintPanel.SetActive(false);
    }

    private static GameObject UiPanel(Transform canvas, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        GameObject p = new GameObject(name, typeof(RectTransform), typeof(Image));
        p.transform.SetParent(canvas, false);
        RectTransform rt = p.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        p.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
        p.GetComponent<Image>().raycastTarget = false;
        return p;
    }

    private static TextMeshProUGUI UiText(Transform parent, float size)
    {
        GameObject t = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        t.transform.SetParent(parent, false);
        RectTransform rt = t.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(12f, 12f);
        rt.offsetMax = new Vector2(-12f, -12f);
        TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    // ---------------------------------------------------------------- scene prep

    private static void ArchiveOldContent()
    {
        GameObject oldGo = FindAny("OldPrototype", false);
        if (oldGo == null) oldGo = new GameObject("OldPrototype");
        foreach (string n in OldNames)
        {
            GameObject g = FindAny(n, false);
            if (g != null && g.transform.parent != oldGo.transform) g.transform.SetParent(oldGo.transform, true);
        }
        oldGo.SetActive(false);
    }

    private static void PrepareScene()
    {
        GameObject flora = GameObject.Find("World/Flora");
        if (flora != null) flora.SetActive(false);

        UnitPool pool = UnityEngine.Object.FindObjectOfType<UnitPool>();
        if (pool != null)
        {
            SerializedObject so = new SerializedObject(pool);
            SerializedProperty list = so.FindProperty("prefabConfigs");
            if (list != null)
            {
                for (int i = 0; i < list.arraySize; i++)
                {
                    UnitAgent prefab = list.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue as UnitAgent;
                    if (prefab == null) continue;
                    SerializedObject pso = new SerializedObject(prefab);
                    SerializedProperty sr = pso.FindProperty("searchRadius");
                    if (sr != null && !Mathf.Approximately(sr.floatValue, 15f))
                    {
                        sr.floatValue = 15f;
                        pso.ApplyModifiedProperties();
                        EditorUtility.SetDirty(prefab);
                    }
                }
                AssetDatabase.SaveAssets();
            }
        }

        AddAlwaysIncludedShader("Sprites/Default");
    }

    private static void AddAlwaysIncludedShader(string shaderName)
    {
        Shader sh = Shader.Find(shaderName);
        if (sh == null) return;
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        if (assets == null || assets.Length == 0) return;
        SerializedObject so = new SerializedObject(assets[0]);
        SerializedProperty arr = so.FindProperty("m_AlwaysIncludedShaders");
        if (arr == null) return;
        for (int i = 0; i < arr.arraySize; i++)
            if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) return;
        arr.arraySize++;
        arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    private static void TryBake()
    {
        try
        {
            Type surfType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            Component surf = surfType != null ? UnityEngine.Object.FindObjectOfType(surfType) as Component : null;
            if (surf == null) { Debug.LogWarning("[TestArena] NavMesh Surface not found. Bake it manually (World > NavMesh Surface > Bake)."); return; }

            SerializedObject so = new SerializedObject(surf);
            SerializedProperty p = so.FindProperty("m_CollectObjects");
            if (p != null) { p.enumValueIndex = 2; so.ApplyModifiedProperties(); }

            Type mgr = Type.GetType("Unity.AI.Navigation.Editor.NavMeshAssetManager, Unity.AI.Navigation.Editor");
            object inst = mgr != null ? mgr.GetProperty("instance")?.GetValue(null) : null;
            var method = inst != null ? inst.GetType().GetMethod("StartBakingSurfaces") : null;
            if (method != null) method.Invoke(inst, new object[] { new UnityEngine.Object[] { surf } });
            else Debug.LogWarning("[TestArena] Could not start the bake automatically. Click Bake on World > NavMesh Surface once.");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[TestArena] Bake failed: " + e.Message + ". Click Bake on World > NavMesh Surface once.");
        }
    }

    // ---------------------------------------------------------------- helpers

    private static GameObject FindAny(string name, bool warn)
    {
        foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            GameObject go = t.gameObject;
            if (go.name != name || !go.scene.IsValid() || EditorUtility.IsPersistent(go)) continue;
            if (_root != null && t.IsChildOf(_root)) continue;
            return go;
        }
        if (warn) Debug.LogWarning("[TestArena] Template not found in scene: " + name);
        return null;
    }

    private static void Set<T>(GameObject go, string prop, object value) where T : Component
    {
        if (go == null) return;
        T c = go.GetComponent<T>();
        if (c == null) { Debug.LogWarning("[TestArena] " + go.name + " has no " + typeof(T).Name); return; }
        SerializedObject so = new SerializedObject(c);
        SerializedProperty p = so.FindProperty(prop);
        if (p == null) { Debug.LogWarning("[TestArena] Missing property '" + prop + "' on " + typeof(T).Name); return; }

        if (value == null) p.objectReferenceValue = null;
        else if (value is UnityEngine.Object o) p.objectReferenceValue = o;
        else if (value is bool b) p.boolValue = b;
        else if (value is int i) { if (p.propertyType == SerializedPropertyType.Enum) p.enumValueIndex = i; else p.intValue = i; }
        else if (value is float f) p.floatValue = f;
        else if (value is string s) p.stringValue = s;
        so.ApplyModifiedProperties();
    }

    private static EnemyData MakeEnemyData(string name, float hp, float dmg, float aggro, float patrol, float interval, float speed)
    {
        string path = "Assets/TestArena/" + name + ".asset";
        EnemyData d = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        if (d == null)
        {
            d = ScriptableObject.CreateInstance<EnemyData>();
            AssetDatabase.CreateAsset(d, path);
        }
        d.maxHP = hp;
        d.attackDamage = dmg;
        d.aggroRange = aggro;
        d.patrolRadius = patrol;
        d.attackInterval = interval;
        d.moveSpeed = speed;
        d.attackRange = 2f;
        EditorUtility.SetDirty(d);
        return d;
    }

    private static Material Mat(string name, Color c)
    {
        Material m;
        if (Mats.TryGetValue(name, out m) && m != null) return m;
        string path = "Assets/TestArena/Materials/" + name + ".mat";
        m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = c;
        EditorUtility.SetDirty(m);
        Mats[name] = m;
        return m;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
