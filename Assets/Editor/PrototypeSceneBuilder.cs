using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Автоматично будує сцену-прототип «Вежа Годинникаря» з простих спрайтів (без зовнішніх ресурсів).
/// Запускається сам при першому відкритті проєкту, або вручну: меню Clockmaker → Build Prototype Scene.
/// </summary>
public static class PrototypeSceneBuilder
{
    const string ScenePath = "Assets/Scenes/Prototype.unity";
    const string SpriteDir = "Assets/Sprites";
    const string MaterialDir = "Assets/Materials";

    static Sprite square, gearSprite, spikeSprite;
    static PhysicsMaterial2D noFriction;

    static readonly Color Ground = new Color(0.36f, 0.25f, 0.16f);
    static readonly Color Copper = new Color(0.62f, 0.40f, 0.20f);
    static readonly Color Wood = new Color(0.72f, 0.52f, 0.26f);
    static readonly Color Brass = new Color(0.55f, 0.48f, 0.28f);
    static readonly Color Gold = new Color(1f, 0.80f, 0.20f);
    static readonly Color Blue = new Color(0.30f, 0.60f, 0.90f);
    static readonly Color Metal = new Color(0.72f, 0.74f, 0.78f);

    [InitializeOnLoadMethod]
    static void AutoBuildOnFirstOpen()
    {
        if (File.Exists(ScenePath)) return;
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(ScenePath)) Build();
        };
    }

    [MenuItem("Clockmaker/Build Prototype Scene")]
    public static void Build()
    {
        EnsureDir("Assets/Scenes");
        EnsureDir(SpriteDir);
        EnsureDir(MaterialDir);

        CreateAssets();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildCamera(out Camera cam);
        BuildBackground();
        BuildLevel();
        GameObject player = BuildPlayer();
        BuildPuzzle();

        cam.GetComponent<CameraFollow>().target = player.transform;

        var gm = new GameObject("GameManager");
        gm.AddComponent<GameManager>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Сцену «Вежа Годинникаря» створено: " + ScenePath + ". Натисніть Play!");
    }

    // ---------------- ресурси ----------------

    static void EnsureDir(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    static void CreateAssets()
    {
        square = MakeSprite("Square", 4, 4, 4, (x, y) => Color.white);

        gearSprite = MakeSprite("Gear", 64, 64, 64, (x, y) =>
        {
            float dx = x - 31.5f, dy = y - 31.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float ang = Mathf.Atan2(dy, dx);
            float outer = 22f + (Mathf.Sin(ang * 8f) > 0f ? 7f : 0f);
            bool inside = r < outer && r > 9f;
            return inside ? Color.white : new Color(1f, 1f, 1f, 0f);
        });

        spikeSprite = MakeSprite("Spike", 32, 32, 32, (x, y) =>
        {
            float half = 16f * (1f - (y + 0.5f) / 32f);
            bool inside = Mathf.Abs(x + 0.5f - 16f) <= half;
            return inside ? Color.white : new Color(1f, 1f, 1f, 0f);
        });

        string matPath = MaterialDir + "/NoFriction.physicsMaterial2D";
        noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(matPath);
        if (noFriction == null)
        {
            noFriction = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(noFriction, matPath);
        }
    }

    static Sprite MakeSprite(string name, int w, int h, int ppu, Func<int, int, Color> pixel)
    {
        string path = SpriteDir + "/" + name + ".png";
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, pixel(x, y));
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ---------------- допоміжні побудови ----------------

    static GameObject Box(string name, Vector2 pos, Vector2 size, Color color, int order, bool solid = true, Transform parent = null)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = color;
        sr.sortingOrder = order;
        if (solid)
        {
            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = Vector2.one;
        }
        return go;
    }

    static void Kinematic(GameObject go)
    {
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    static void Platform(string name, float centerX, float topY, float width)
    {
        Box(name, new Vector2(centerX, topY - 0.25f), new Vector2(width, 0.5f), Copper, 0);
    }

    static void Gear(Vector2 pos)
    {
        var go = new GameObject("Gear");
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = gearSprite;
        sr.color = Gold;
        sr.sortingOrder = 3;
        var cc = go.AddComponent<CircleCollider2D>();
        cc.radius = 0.5f;
        cc.isTrigger = true;
        go.AddComponent<Collectible>();
    }

    static void Spikes(Vector2 centerBottom, int count)
    {
        var root = new GameObject("Spikes");
        root.transform.position = centerBottom;
        float w = 0.7f;
        float total = count * w;
        for (int i = 0; i < count; i++)
        {
            var s = new GameObject("Spike" + i);
            s.transform.SetParent(root.transform, false);
            s.transform.localPosition = new Vector3(-total / 2f + w * (i + 0.5f), 0.3f, 0f);
            s.transform.localScale = new Vector3(w, 0.6f, 1f);
            var sr = s.AddComponent<SpriteRenderer>();
            sr.sprite = spikeSprite;
            sr.color = Metal;
            sr.sortingOrder = 2;
        }
        var bc = root.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(total * 0.85f, 0.4f);
        bc.offset = new Vector2(0f, 0.2f);
        root.AddComponent<Hazard>();
    }

    // ---------------- сцена ----------------

    static void BuildCamera(out Camera cam)
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0.7f, 0f, -10f);
        cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.13f, 0.09f, 0.07f);
        go.AddComponent<AudioListener>();
        go.AddComponent<CameraFollow>();
    }

    static void BuildBackground()
    {
        var parent = new GameObject("Background");
        Vector2[] pos = { new Vector2(-4, 2), new Vector2(8, 3.5f), new Vector2(20, 2), new Vector2(28, 4) };
        float[] speed = { 8, -6, 10, -8 };
        float[] size = { 7, 9, 6, 8 };
        for (int i = 0; i < pos.Length; i++)
        {
            var g = new GameObject("BgGear" + i);
            g.transform.SetParent(parent.transform, false);
            g.transform.position = pos[i];
            g.transform.localScale = new Vector3(size[i], size[i], 1f);
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = gearSprite;
            sr.color = new Color(0.55f, 0.38f, 0.2f, 0.18f);
            sr.sortingOrder = -10;
            g.AddComponent<Spinner>().degreesPerSecond = speed[i];
        }
    }

    static void BuildLevel()
    {
        var level = new GameObject("Level");

        // Земля: ліва ділянка, прірва між x=8 та x=12, права ділянка
        Box("Ground_Left", new Vector2(-1f, -4f), new Vector2(18f, 1f), Ground, 0, true, level.transform);
        Box("Ground_Right", new Vector2(21f, -4f), new Vector2(18f, 1f), Ground, 0, true, level.transform);

        // Невидимі стіни по краях рівня
        var wl = Box("Wall_Left", new Vector2(-10.5f, 0f), new Vector2(1f, 20f), Color.clear, 0, true, level.transform);
        wl.GetComponent<SpriteRenderer>().enabled = false;
        var wr = Box("Wall_Right", new Vector2(30.5f, 0f), new Vector2(1f, 20f), Color.clear, 0, true, level.transform);
        wr.GetComponent<SpriteRenderer>().enabled = false;

        // Платформи (верхня грань вказана явно)
        Platform("Platform_1", 2f, -1.5f, 4f);
        Platform("Platform_2", 6f, 0.5f, 3f);
        Platform("Platform_3", 10f, -1.5f, 3f);   // над прірвою
        Platform("Platform_4", 15f, -1.5f, 3f);

        // Шестерні
        Gear(new Vector2(6f, 1.5f));
        Gear(new Vector2(10f, -0.5f));
        Gear(new Vector2(15f, -0.5f));

        // Шипи (навчальна перешкода на старті)
        Spikes(new Vector2(-3f, -3.5f), 3);
    }

    static GameObject BuildPlayer()
    {
        var player = Box("Player", new Vector2(-8f, -2.9f), new Vector2(0.8f, 1.1f), Blue, 5);
        player.tag = "Player";
        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        player.GetComponent<BoxCollider2D>().sharedMaterial = noFriction;

        // «Око», яке показує напрямок руху
        var eye = Box("Eye", Vector2.zero, new Vector2(0.28f, 0.2f), Color.white, 6, false, player.transform);
        eye.transform.localPosition = new Vector3(0.25f, 0.25f, 0f);

        var pc = player.AddComponent<PlayerController2D>();
        pc.eye = eye.transform;
        return player;
    }

    static void BuildPuzzle()
    {
        var puzzle = new GameObject("Puzzle");

        // Брама, що блокує шлях
        var gate = Box("Gate", new Vector2(20f, -0.5f), new Vector2(1f, 6f), Brass, 2, true, puzzle.transform);
        Kinematic(gate);
        var gateDoor = gate.AddComponent<Door>();
        gateDoor.openOffset = new Vector2(0f, 6.5f);

        // Важкий ящик і нажимна плита
        var crate = Box("Crate", new Vector2(14f, -3f), new Vector2(1f, 1f), Wood, 2, true, puzzle.transform);
        var crb = crate.AddComponent<Rigidbody2D>();
        crb.mass = 2f;
        crb.freezeRotation = true;
        crate.AddComponent<Crate>();

        var plate = Box("PressurePlate", new Vector2(17f, -3.35f), new Vector2(1.6f, 0.3f), new Color(0.8f, 0.3f, 0.25f), 1, false, puzzle.transform);
        var trigger = plate.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = Vector2.one;
        var pp = plate.AddComponent<PressurePlate>();
        pp.target = gateDoor;
        pp.requiredMass = 1.5f;
        pp.latch = true;

        // Вихід
        var exitDoorGo = Box("ExitDoor", new Vector2(27f, -2f), new Vector2(1f, 3f), Brass, 2, true, puzzle.transform);
        Kinematic(exitDoorGo);
        var exitDoor = exitDoorGo.AddComponent<Door>();
        exitDoor.openOffset = new Vector2(0f, 3.3f);
        exitDoor.isExit = true;

        var zone = new GameObject("ExitZone");
        zone.transform.SetParent(puzzle.transform, false);
        zone.transform.position = new Vector3(28.6f, -2.5f, 0f);
        var zc = zone.AddComponent<BoxCollider2D>();
        zc.isTrigger = true;
        zc.size = new Vector2(1.6f, 3f);
        zone.AddComponent<ExitZone>().exitDoor = exitDoor;

        // Важіль біля виходу
        var lever = new GameObject("Lever");
        lever.transform.SetParent(puzzle.transform, false);
        lever.transform.position = new Vector3(23.5f, -3f, 0f);
        var lc = lever.AddComponent<BoxCollider2D>();
        lc.isTrigger = true;
        lc.size = new Vector2(2.5f, 2f);

        Box("LeverBase", new Vector2(23.5f, -3.35f), new Vector2(0.8f, 0.3f), Metal, 1, false, lever.transform);
        var pivot = new GameObject("LeverPivot");
        pivot.transform.SetParent(lever.transform, false);
        pivot.transform.position = new Vector3(23.5f, -3.2f, 0f);
        pivot.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        var handle = Box("LeverHandle", Vector2.zero, new Vector2(0.15f, 0.9f), new Color(0.9f, 0.3f, 0.25f), 2, false, pivot.transform);
        handle.transform.localPosition = new Vector3(0f, 0.45f, 0f);

        var lv = lever.AddComponent<Lever>();
        lv.door = exitDoor;
        lv.pivot = pivot.transform;
    }
}
