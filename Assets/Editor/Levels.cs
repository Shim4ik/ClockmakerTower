using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Рівні гри «Вежа Годинникаря»: п'ять поверхів вежі. Кожен рівень будується як вимкнений шаблон (Level_N_Template);
/// LevelManager створює його копію під час гри. Частина PrototypeSceneBuilder (partial).
/// </summary>
public static partial class PrototypeSceneBuilder
{
    class LevelSpec
    {
        public string name;
        public string intro;
        public Vector2 spawn = new Vector2(-8f, -2.9f);
        public float left = -10f;
        public float right = 30f;
        public int gateCount;
        public string gateTask = "Відкрити браму (ящик на плиту)";
        public bool hasNpc;
        public bool hasBoss;
    }

    // ======================= побудова шаблонів =======================

    /// <summary>
    /// Виконує build(root) і переносить усі нові кореневі об'єкти сцени під root, потім вимикає root (шаблон).
    /// </summary>
    static GameObject BuildLevelRoot(string rootName, LevelSpec spec, Action<Transform> build)
    {
        var root = new GameObject(rootName);
        var before = new HashSet<GameObject>(SceneManager.GetActiveScene().GetRootGameObjects());

        build(root.transform);

        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (before.Contains(go)) continue;
            go.transform.SetParent(root.transform, true);
        }

        var spawn = new GameObject("Spawn");
        spawn.transform.SetParent(root.transform, false);
        spawn.transform.position = new Vector3(spec.spawn.x, spec.spawn.y, 0f);

        var data = root.AddComponent<LevelData>();
        data.levelName = spec.name;
        data.intro = spec.intro;
        data.spawn = spawn.transform;
        data.leftX = spec.left;
        data.rightX = spec.right;
        data.camMinY = -1f;
        data.camMaxY = 2f;
        data.gateCount = spec.gateCount;
        data.gateTask = spec.gateTask;
        data.hasNpc = spec.hasNpc;
        data.hasBoss = spec.hasBoss;

        root.SetActive(false);
        return root;
    }

    static void Adopt(string objectName, Transform root)
    {
        GameObject go = GameObject.Find(objectName);
        if (go != null) go.transform.SetParent(root, true);
    }

    static void BuildAllLevels()
    {
        var templates = new List<GameObject>();

        // Рівень 1 — Майстерня (навчальний; нажимна плита, ящик, NPC)
        templates.Add(BuildLevelRoot("Level_1_Template", new LevelSpec
        {
            name = "Майстерня",
            intro = "Збери 3 шестерні й дістанься до виходу.",
            left = -10f, right = 30f, gateCount = 1, hasNpc = true
        }, root =>
        {
            BuildLevel();
            BuildPuzzle();
            BuildLamps();
            BuildEnemies();
            BuildNpc();
            Adopt("WorldSound", root);
            Adopt("WorldSoundMachine", root);
        }));

        templates.Add(BuildLevelRoot("Level_2_Template", new LevelSpec
        {
            name = "Зала шестерень",
            intro = "Рухомі платформи несуть через прірви.",
            left = -10f, right = 51f
        }, root => BuildLevel2()));

        templates.Add(BuildLevelRoot("Level_3_Template", new LevelSpec
        {
            name = "Маятникова зала",
            intro = "Пробігай між маятниками або перестрибуй їх.",
            left = -10f, right = 48f, gateCount = 1, gateTask = "Відкрити браму (ящик на плиту)"
        }, root => BuildLevel3()));

        templates.Add(BuildLevelRoot("Level_4_Template", new LevelSpec
        {
            name = "Лабораторія терезів",
            intro = "Важкі брами відкриваються лише під вагою ящиків.",
            left = -10f, right = 50f, gateCount = 2, gateTask = "Відкрити обидві брами (ящики на плити)"
        }, root => BuildLevel4()));

        templates.Add(BuildLevelRoot("Level_5_Template", new LevelSpec
        {
            name = "Дзвіниця",
            intro = "Переможи головного сторожа: стрибни йому на голову тричі!",
            left = -10f, right = 39f, hasBoss = true
        }, root => BuildLevel5()));

        var lmGo = new GameObject("LevelManager");
        var lm = lmGo.AddComponent<LevelManager>();
        var so = new SerializedObject(lm);
        var prop = so.FindProperty("levelTemplates");
        prop.arraySize = templates.Count;
        for (int i = 0; i < templates.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = templates[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ======================= допоміжні об'єкти =======================

    static void GroundSeg(string name, float x0, float x1)
    {
        Tiled(name, new Vector2((x0 + x1) / 2f, -4f), new Vector2(x1 - x0, 1f), brickTex, brickMat, 0);
    }

    static void Walls(float left, float right)
    {
        var wl = Box("Wall_Left", new Vector2(left - 0.5f, 0f), new Vector2(1f, 40f), Color.clear, 0);
        wl.GetComponent<SpriteRenderer>().enabled = false;
        var wr = Box("Wall_Right", new Vector2(right + 0.5f, 0f), new Vector2(1f, 40f), Color.clear, 0);
        wr.GetComponent<SpriteRenderer>().enabled = false;
    }

    static void MovingPlat(string name, Vector2 center, Vector2 size, Vector2 offset, float speed)
    {
        var go = Tiled(name, center, size, plankTex, woodMat, 1);
        Kinematic(go);
        var mp = go.AddComponent<MovingPlatform>();
        mp.offset = offset;
        mp.speed = speed;
    }

    static void PendulumAt(float x, float pivotY, float length, float amplitude, float period, float phase)
    {
        var root = new GameObject("Pendulum");
        root.transform.position = new Vector3(x, pivotY, 0f);
        Box("Beam", new Vector2(x, pivotY + 0.25f), new Vector2(1.2f, 0.5f), new Color(0.25f, 0.20f, 0.17f), 1, false, root.transform);

        var pivot = new GameObject("Pivot");
        pivot.transform.SetParent(root.transform, false);
        pivot.transform.position = new Vector3(x, pivotY, 0f);
        var pend = pivot.AddComponent<Pendulum>();
        pend.amplitudeDeg = amplitude;
        pend.period = period;
        pend.phase = phase;

        Box("Rope", new Vector2(x, pivotY - length / 2f), new Vector2(0.08f, length), Metal, 2, false, pivot.transform);
        var weight = Box("Weight", new Vector2(x, pivotY - length), new Vector2(1f, 1f), new Color(0.70f, 0.25f, 0.20f), 3, false, pivot.transform);
        var bc = weight.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(0.9f, 0.9f);
        weight.AddComponent<Hazard>();
    }

    static void CheckpointAt(float x)
    {
        var root = new GameObject("Checkpoint");
        root.transform.position = new Vector3(x, -2.5f, 0f);
        var bc = root.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(1.6f, 3f);

        Box("Pole", new Vector2(x, -2.7f), new Vector2(0.12f, 1.6f), new Color(0.25f, 0.20f, 0.17f), 1, false, root.transform);
        var flag = Box("Flag", new Vector2(x + 0.35f, -2.1f), new Vector2(0.6f, 0.4f), new Color(0.80f, 0.30f, 0.25f), 2, false, root.transform);
        var cp = root.AddComponent<Checkpoint>();
        cp.flag = flag.GetComponent<SpriteRenderer>();
    }

    static void LampAt(float x)
    {
        var root = new GameObject("Lamp");
        root.transform.position = new Vector3(x, -1.8f, 0f);
        var bc = root.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(3f, 4f);

        Box("Pole", new Vector2(x, -2f), new Vector2(0.15f, 3f), new Color(0.2f, 0.18f, 0.16f), 1, false, root.transform);
        var bulb = Box("Bulb", new Vector2(x, -0.3f), new Vector2(0.4f, 0.4f), new Color(0.3f, 0.25f, 0.15f), 42, false, root.transform);

        var glowGo = new GameObject("Glow");
        glowGo.transform.SetParent(root.transform, false);
        glowGo.transform.position = new Vector3(x, -0.3f, 0f);
        glowGo.transform.localScale = new Vector3(7f, 7f, 1f);
        var gsr = glowGo.AddComponent<SpriteRenderer>();
        gsr.sprite = glowTex;
        gsr.color = new Color(1f, 0.75f, 0.35f, 0f);
        gsr.sortingOrder = 41;

        var lamp = root.AddComponent<LampLight>();
        lamp.glow = gsr;
        lamp.bulb = bulb.GetComponent<SpriteRenderer>();
    }

    static Door GateAt(float x)
    {
        var gate = Tiled("Gate", new Vector2(x, -0.5f), new Vector2(1f, 6f), metalTex, metalMat, 2);
        Kinematic(gate);
        var door = gate.AddComponent<Door>();
        door.openOffset = new Vector2(0f, 6.5f);
        return door;
    }

    static void PlateAt(float x, Door target, float requiredMass, float width = 1.6f)
    {
        var plate = Box("PressurePlate", new Vector2(x, -3.35f), new Vector2(width, 0.3f), new Color(0.8f, 0.3f, 0.25f), 1, false);
        var trigger = plate.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = Vector2.one;
        var pp = plate.AddComponent<PressurePlate>();
        pp.target = target;
        pp.requiredMass = requiredMass;
        pp.latch = true;
    }

    static void CrateAt(float x)
    {
        var crate = Tiled("Crate", new Vector2(x, -3f), new Vector2(1f, 1f), plankTex, woodMat, 2);
        var rb = crate.AddComponent<Rigidbody2D>();
        rb.mass = 2f;
        rb.freezeRotation = true;
        crate.AddComponent<Crate>();
    }

    /// <summary>Важіль + двері виходу + зона виходу (двері на doorX, зона за дверима).</summary>
    static void ExitAssembly(float leverX, float doorX)
    {
        var exitDoorGo = Tiled("ExitDoor", new Vector2(doorX, -2f), new Vector2(1f, 3f), metalTex, metalMat, 2);
        Kinematic(exitDoorGo);
        var exitDoor = exitDoorGo.AddComponent<Door>();
        exitDoor.openOffset = new Vector2(0f, 3.3f);
        exitDoor.isExit = true;

        var zone = new GameObject("ExitZone");
        zone.transform.position = new Vector3(doorX + 1.6f, -2.5f, 0f);
        var zc = zone.AddComponent<BoxCollider2D>();
        zc.isTrigger = true;
        zc.size = new Vector2(1.6f, 3f);
        zone.AddComponent<ExitZone>().exitDoor = exitDoor;

        var lever = new GameObject("Lever");
        lever.transform.position = new Vector3(leverX, -3f, 0f);
        var lc = lever.AddComponent<BoxCollider2D>();
        lc.isTrigger = true;
        lc.size = new Vector2(2.5f, 2f);

        Box("LeverBase", new Vector2(leverX, -3.35f), new Vector2(0.8f, 0.3f), Metal, 1, false, lever.transform);
        var pivot = new GameObject("LeverPivot");
        pivot.transform.SetParent(lever.transform, false);
        pivot.transform.position = new Vector3(leverX, -3.2f, 0f);
        pivot.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        var handle = Box("LeverHandle", Vector2.zero, new Vector2(0.15f, 0.9f), new Color(0.9f, 0.3f, 0.25f), 2, false, pivot.transform);
        handle.transform.localPosition = new Vector3(0f, 0.45f, 0f);

        var lv = lever.AddComponent<Lever>();
        lv.door = exitDoor;
        lv.pivot = pivot.transform;
    }

    static void BuildBoss(float x, float leftX, float rightX)
    {
        const float scale = 1.6f;
        float y = -3.5f + 0.5f * scale;

        var enemy = new GameObject("Boss");
        enemy.transform.position = new Vector3(x, y, 0f);

        var rb = enemy.AddComponent<Rigidbody2D>();
        rb.mass = 60f;
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        var bc = enemy.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(0.9f * scale, 1.0f * scale);
        bc.sharedMaterial = noFriction;

        // Візуал у збільшеному дочірньому об'єкті: кліпи Animator змінюють масштаб лише всередині нього
        var scaleRoot = new GameObject("ScaleRoot");
        scaleRoot.transform.SetParent(enemy.transform, false);
        scaleRoot.transform.localScale = new Vector3(scale, scale, 1f);

        Color bossColor = new Color(0.55f, 0.16f, 0.22f);
        var visual = Box("Visual", new Vector2(x, y), new Vector2(0.9f, 1.0f), bossColor, 4, false, scaleRoot.transform);
        var eye = Box("Eye", Vector2.zero, new Vector2(0.28f, 0.2f), new Color(1f, 0.9f, 0.3f), 6, false, visual.transform);
        eye.transform.localPosition = new Vector3(0.25f, 0.25f, 0f);

        var animator = visual.AddComponent<Animator>();
        animator.runtimeAnimatorController = enemyController;
        var events = visual.AddComponent<EnemyVisualEvents>();

        var ai = enemy.AddComponent<EnemyAI>();
        ai.leftX = leftX;
        ai.rightX = rightX;
        ai.animator = animator;
        ai.sprite = visual.GetComponent<SpriteRenderer>();
        ai.eye = eye.transform;
        ai.patrolColor = bossColor;
        ai.stompsToDefeat = 3;
        ai.mustDefeat = true;
        ai.stompHeight = 1.0f;
        ai.detectionRange = 9f;
        ai.closeRadius = 3f;
        ai.patrolSpeed = 2.5f;
        ai.chaseSpeed = 4f;
        ai.attackRange = 2.2f;
        ai.attackCooldown = 1.0f;
        ai.knockback = new Vector2(10f, 7f);
        ai.retreatSpeed = 6f;
        ai.retreatTime = 2.2f;
        ai.boundsMargin = 1.5f;
        events.ai = ai;
    }

    // ======================= Рівень 2: Зала шестерень =======================

    static void BuildLevel2()
    {
        Walls(-10f, 51f);

        GroundSeg("Ground_1", -10f, 4f);
        GroundSeg("Ground_2", 10f, 22f);
        GroundSeg("Ground_3", 34f, 51f);

        // Рухома платформа через першу прірву (x: 5,5 → 8,5)
        MovingPlat("MovingPlatform_H", new Vector2(5.5f, -3.75f), new Vector2(2.5f, 0.5f), new Vector2(3f, 0f), 2f);

        // Сходи на другій ділянці
        Platform("Platform_1", 13f, -1.5f, 3f);
        Platform("Platform_2", 17f, 0.5f, 3f);
        Platform("Platform_3", 21f, -1.5f, 3f);

        // Ліфт (рухома платформа вгору) та верхній поміст
        MovingPlat("MovingPlatform_V", new Vector2(24.5f, -3.75f), new Vector2(2.4f, 0.5f), new Vector2(0f, 3.5f), 1.6f);
        Platform("Platform_Up", 30f, 0f, 6f);

        Gear(new Vector2(7f, -1.2f));
        Gear(new Vector2(17f, 1.5f));
        Gear(new Vector2(30f, 1.0f));
        Gear(new Vector2(37f, -2.7f));

        Spikes(new Vector2(-3f, -3.5f), 3);
        Spikes(new Vector2(20.5f, -3.5f), 3);

        CheckpointAt(16f);
        CheckpointAt(35.5f);

        LampAt(-6f);
        LampAt(16f);
        LampAt(36f);

        var enemies = new GameObject("Enemies");
        BuildEnemy("Guard_1", 41f, 38.5f, 44f, enemies.transform);

        ExitAssembly(45.5f, 48f);
    }

    // ======================= Рівень 3: Маятникова зала =======================

    static void BuildLevel3()
    {
        Walls(-10f, 48f);
        GroundSeg("Ground", -10f, 48f);

        // Три маятники: можна пробігти у вільний момент або перестрибнути, коли вантаж унизу
        PendulumAt(6f, 3f, 5.2f, 50f, 3.0f, 0f);
        PendulumAt(16f, 3f, 5.2f, 50f, 3.0f, Mathf.PI * 0.5f);
        PendulumAt(26f, 3f, 5.2f, 50f, 3.0f, Mathf.PI);

        Gear(new Vector2(3.5f, -2.7f));
        Gear(new Vector2(21f, -2.7f));
        Platform("Platform_1", 31f, -1.5f, 3f);
        Gear(new Vector2(31f, -0.5f));

        CheckpointAt(11f);
        CheckpointAt(34.5f);

        // Ящик і нажимна плита відчиняють браму
        CrateAt(32f);
        Door gate = GateAt(39f);
        PlateAt(36f, gate, 1.5f);

        LampAt(-6f);
        LampAt(21f);
        LampAt(37f);

        var enemies = new GameObject("Enemies");
        BuildEnemy("Guard_1", 41f, 40f, 41.8f, enemies.transform);

        ExitAssembly(42.5f, 45.5f);
    }

    // ======================= Рівень 4: Лабораторія терезів =======================

    static void BuildLevel4()
    {
        Walls(-10f, 50f);
        GroundSeg("Ground", -10f, 50f);

        // Перша брама: достатньо одного ящика
        CrateAt(-3f);
        Door gate1 = GateAt(7f);
        PlateAt(3.5f, gate1, 1.5f);

        // Друга брама: потрібні ДВА ящики (маса 4 проти порога 3,5; гравець + один ящик = 3 — замало)
        CrateAt(10f);
        Door gate2 = GateAt(24.5f);
        PlateAt(21f, gate2, 3.5f, 3.2f);   // широка плита: на неї мають поміститися обидва ящики

        Gear(new Vector2(-6f, -2.7f));
        Platform("Platform_1", 12f, -1.5f, 3f);
        Gear(new Vector2(12f, -0.5f));
        Platform("Platform_2", 16f, 0.5f, 3f);
        Gear(new Vector2(16f, 1.5f));
        Gear(new Vector2(28.5f, -2.7f));

        CheckpointAt(8.5f);
        CheckpointAt(26f);

        LampAt(-6f);
        LampAt(17f);
        LampAt(38f);

        var enemies = new GameObject("Enemies");
        BuildEnemy("Guard_1", 30.5f, 28f, 33f, enemies.transform);
        BuildEnemy("Guard_2", 38.5f, 36f, 41f, enemies.transform);

        ExitAssembly(44f, 47f);
    }

    // ======================= Рівень 5: Дзвіниця (бос) =======================

    static void BuildLevel5()
    {
        Walls(-10f, 39f);
        GroundSeg("Ground", -10f, 39f);

        Platform("Platform_1", 2f, -1.5f, 3f);
        Platform("Platform_2", 6f, 0.5f, 3f);
        Platform("Platform_3", 28f, -1.5f, 3f);
        Gear(new Vector2(2f, -0.5f));
        Gear(new Vector2(6f, 1.5f));
        Gear(new Vector2(28f, -0.5f));

        // Два маятники над ареною
        PendulumAt(10f, 3f, 5.2f, 45f, 3.2f, 0f);
        PendulumAt(22f, 3f, 5.2f, 45f, 3.2f, Mathf.PI);

        CheckpointAt(4f);
        CheckpointAt(26f);

        LampAt(-6f);
        LampAt(16f);
        LampAt(34f);

        BuildBoss(16f, 10f, 24f);

        ExitAssembly(33f, 36f);
    }
}
