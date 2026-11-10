using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Автоматично будує сцену «Вежа Годинникаря» з оточенням (лабораторна №11):
/// текстури (цегла, дошки, метал), матеріали, фон із паралаксом, ліхтарі та цикл дня й ночі.
/// Запускається сам при першому відкритті проєкту або вручну: меню Clockmaker → Build Environment Scene.
/// </summary>
public static class PrototypeSceneBuilder
{
    const string ScenePath = "Assets/Scenes/Environment.unity";
    const string SpriteDir = "Assets/Sprites";
    const string MaterialDir = "Assets/Materials";

    static Sprite square, gearSprite, spikeSprite, brickTex, plankTex, metalTex, gradientTex, glowTex;
    static PhysicsMaterial2D noFriction;
    static Material brickMat, woodMat, metalMat;

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

    [MenuItem("Clockmaker/Build Environment Scene")]
    public static void Build()
    {
        EnsureDir("Assets/Scenes");
        EnsureDir(SpriteDir);
        EnsureDir(MaterialDir);

        CreateAssets();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Camera cam;
        SpriteRenderer overlay;
        BuildCamera(out cam, out overlay);
        SpriteRenderer backdrop = BuildBackdrop();
        BuildBackgroundGears();
        BuildLevel();
        GameObject player = BuildPlayer();
        BuildPuzzle();
        BuildLamps();

        cam.GetComponent<CameraFollow>().target = player.transform;

        var dn = new GameObject("DayNightCycle");
        var cycle = dn.AddComponent<DayNightCycle>();
        cycle.cam = cam;
        cycle.nightOverlay = overlay;
        cycle.backdrop = backdrop;

        new GameObject("GameManager").AddComponent<GameManager>();

        EditorSceneManager.SaveScene(scene, ScenePath);

        // Додаємо сцену до Build Settings, не видаляючи наявні (наприклад, Prototype.unity)
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        foreach (var s in list) if (s.path == ScenePath) found = true;
        if (!found) list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Сцену з оточенням створено: " + ScenePath + ". Натисніть Play!");
    }

    // ---------------- ресурси ----------------

    static void EnsureDir(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }

    static float Hash(float a, float b)
    {
        float s = Mathf.Sin(a * 12.9898f + b * 78.233f) * 43758.5453f;
        return s - Mathf.Floor(s);
    }

    static void CreateAssets()
    {
        square = MakeSprite("Square", 4, 4, 4, (x, y) => Color.white, false);

        gearSprite = MakeSprite("Gear", 64, 64, 64, (x, y) =>
        {
            float dx = x - 31.5f, dy = y - 31.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float ang = Mathf.Atan2(dy, dx);
            float outer = 22f + (Mathf.Sin(ang * 8f) > 0f ? 7f : 0f);
            bool inside = r < outer && r > 9f;
            return inside ? Color.white : new Color(1f, 1f, 1f, 0f);
        }, false);

        spikeSprite = MakeSprite("Spike", 32, 32, 32, (x, y) =>
        {
            float half = 16f * (1f - (y + 0.5f) / 32f);
            bool inside = Mathf.Abs(x + 0.5f - 16f) <= half;
            return inside ? Color.white : new Color(1f, 1f, 1f, 0f);
        }, false);

        // ----- текстури оточення (повторювані, 32x32 пікселі = 1 одиниця) -----
        brickTex = MakeSprite("Tex_Brick", 32, 32, 32, (x, y) =>
        {
            int row = y / 8;
            int off = (row % 2) * 8;
            int bx = (x + off) % 16;
            int by = y % 8;
            if (by == 0 || bx == 0) return new Color(0.22f, 0.15f, 0.11f);
            float k = 0.85f + 0.3f * Hash((x + off) / 16, row);
            return new Color(0.55f * k, 0.30f * k, 0.22f * k);
        }, true);

        plankTex = MakeSprite("Tex_Planks", 32, 32, 32, (x, y) =>
        {
            int row = y / 8;
            if (y % 8 == 0) return new Color(0.28f, 0.18f, 0.09f);
            if ((x == 3 || x == 28) && y % 8 == 4) return new Color(0.20f, 0.13f, 0.07f);
            float n = Hash(x * 0.37f, row) * 0.5f + Hash(x, y) * 0.5f;
            float k = 0.86f + 0.22f * n;
            return new Color(0.62f * k, 0.42f * k, 0.22f * k);
        }, true);

        metalTex = MakeSprite("Tex_Metal", 32, 32, 32, (x, y) =>
        {
            if (x == 0 || y == 0 || x == 31 || y == 31) return new Color(0.25f, 0.24f, 0.22f);
            bool rx = (x >= 3 && x <= 5) || (x >= 26 && x <= 28);
            bool ry = (y >= 3 && y <= 5) || (y >= 26 && y <= 28);
            if (rx && ry) return new Color(0.78f, 0.74f, 0.62f);
            float k = 0.9f + 0.15f * Hash(x * 0.1f, y);
            return new Color(0.52f * k, 0.50f * k, 0.46f * k);
        }, true);

        gradientTex = MakeSprite("Tex_Gradient", 64, 64, 64, (x, y) =>
        {
            float g = Mathf.Lerp(1f, 0.6f, y / 63f);
            return new Color(g, g, g, 1f);
        }, false);

        glowTex = MakeSprite("Tex_Glow", 64, 64, 64, (x, y) =>
        {
            float dx = x - 31.5f, dy = y - 31.5f;
            float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) / 32f);
            return new Color(1f, 1f, 1f, a * a);
        }, false);

        // ----- матеріали -----
        brickMat = MakeMaterial("Mat_Brick", brickTex);
        woodMat = MakeMaterial("Mat_Wood", plankTex);
        metalMat = MakeMaterial("Mat_Metal", metalTex);

        string matPath = MaterialDir + "/NoFriction.physicsMaterial2D";
        noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(matPath);
        if (noFriction == null)
        {
            noFriction = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(noFriction, matPath);
        }
    }

    static Material MakeMaterial(string name, Sprite sprite)
    {
        string path = MaterialDir + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) return null;
        mat = new Material(sh) { name = name };
        if (sprite != null) mat.mainTexture = sprite.texture;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Sprite MakeSprite(string name, int w, int h, int ppu, Func<int, int, Color> pixel, bool tile)
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
        importer.filterMode = tile ? FilterMode.Point : FilterMode.Bilinear;
        importer.wrapMode = tile ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;   // потрібно для режиму Tiled
        importer.SetTextureSettings(settings);

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

    /// <summary>Об'єкт із повторюваною текстурою (Draw Mode = Tiled) та матеріалом.</summary>
    static GameObject Tiled(string name, Vector2 pos, Vector2 size, Sprite tex, Material mat, int order, bool solid = true, Transform parent = null)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = tex;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = size;
        sr.sortingOrder = order;
        if (mat != null) sr.sharedMaterial = mat;
        if (solid)
        {
            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = size;
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
        Tiled(name, new Vector2(centerX, topY - 0.25f), new Vector2(width, 0.5f), plankTex, woodMat, 0);
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

    static void BuildCamera(out Camera cam, out SpriteRenderer overlay)
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

        // Нічне затемнення: напівпрозорий прямокутник на камері
        var ov = new GameObject("NightOverlay");
        ov.transform.SetParent(go.transform, false);
        ov.transform.localPosition = new Vector3(0f, 0f, 10f);
        ov.transform.localScale = new Vector3(70f, 40f, 1f);
        overlay = ov.AddComponent<SpriteRenderer>();
        overlay.sprite = square;
        overlay.color = new Color(0.02f, 0.03f, 0.12f, 0f);
        overlay.sortingOrder = 40;
    }

    static SpriteRenderer BuildBackdrop()
    {
        var root = new GameObject("Backdrop");

        // Небо / задня стіна вежі: градієнт, що слідує за камерою
        var grad = new GameObject("Gradient");
        grad.transform.SetParent(root.transform, false);
        grad.transform.position = new Vector3(0.7f, 0f, 0f);
        grad.transform.localScale = new Vector3(70f, 40f, 1f);
        var gsr = grad.AddComponent<SpriteRenderer>();
        gsr.sprite = gradientTex;
        gsr.sortingOrder = -20;
        grad.AddComponent<ParallaxLayer>().factor = 1f;

        // Колони з освітленими вікнами (дальній план, паралакс 0.5)
        var cols = new GameObject("Columns");
        cols.transform.SetParent(root.transform, false);
        cols.AddComponent<ParallaxLayer>().factor = 0.5f;
        for (float x = -16f; x <= 38f; x += 6f)
        {
            Box("Column", new Vector2(x, 1f), new Vector2(2.4f, 22f), new Color(0.10f, 0.07f, 0.08f), -9, false, cols.transform);
            Box("Window", new Vector2(x, 2.2f), new Vector2(0.8f, 1.6f), new Color(1f, 0.72f, 0.35f, 0.85f), -8, false, cols.transform);
        }
        return gsr;
    }

    static void BuildBackgroundGears()
    {
        var parent = new GameObject("BackgroundGears");
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
            sr.color = new Color(0.55f, 0.38f, 0.2f, 0.25f);
            sr.sortingOrder = -7;
            g.AddComponent<Spinner>().degreesPerSecond = speed[i];
        }
    }

    static void BuildLevel()
    {
        var level = new GameObject("Level");

        // Земля з цегляною текстурою: ліва ділянка, прірва між x=8 та x=12, права ділянка
        Tiled("Ground_Left", new Vector2(-1f, -4f), new Vector2(18f, 1f), brickTex, brickMat, 0, true, level.transform);
        Tiled("Ground_Right", new Vector2(21f, -4f), new Vector2(18f, 1f), brickTex, brickMat, 0, true, level.transform);

        // Невидимі стіни по краях рівня
        var wl = Box("Wall_Left", new Vector2(-10.5f, 0f), new Vector2(1f, 20f), Color.clear, 0, true, level.transform);
        wl.GetComponent<SpriteRenderer>().enabled = false;
        var wr = Box("Wall_Right", new Vector2(30.5f, 0f), new Vector2(1f, 20f), Color.clear, 0, true, level.transform);
        wr.GetComponent<SpriteRenderer>().enabled = false;

        // Дерев'яні платформи
        Platform("Platform_1", 2f, -1.5f, 4f);
        Platform("Platform_2", 6f, 0.5f, 3f);
        Platform("Platform_3", 10f, -1.5f, 3f);   // над прірвою
        Platform("Platform_4", 15f, -1.5f, 3f);

        Gear(new Vector2(6f, 1.5f));
        Gear(new Vector2(10f, -0.5f));
        Gear(new Vector2(15f, -0.5f));

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

        var eye = Box("Eye", Vector2.zero, new Vector2(0.28f, 0.2f), Color.white, 6, false, player.transform);
        eye.transform.localPosition = new Vector3(0.25f, 0.25f, 0f);

        var pc = player.AddComponent<PlayerController2D>();
        pc.eye = eye.transform;
        return player;
    }

    static void BuildPuzzle()
    {
        var puzzle = new GameObject("Puzzle");

        // Металева брама
        var gate = Tiled("Gate", new Vector2(20f, -0.5f), new Vector2(1f, 6f), metalTex, metalMat, 2, true, puzzle.transform);
        Kinematic(gate);
        var gateDoor = gate.AddComponent<Door>();
        gateDoor.openOffset = new Vector2(0f, 6.5f);

        // Дерев'яний ящик (маса 2)
        var crate = Tiled("Crate", new Vector2(14f, -3f), new Vector2(1f, 1f), plankTex, woodMat, 2, true, puzzle.transform);
        var crb = crate.AddComponent<Rigidbody2D>();
        crb.mass = 2f;
        crb.freezeRotation = true;
        crate.AddComponent<Crate>();

        // Нажимна плита
        var plate = Box("PressurePlate", new Vector2(17f, -3.35f), new Vector2(1.6f, 0.3f), new Color(0.8f, 0.3f, 0.25f), 1, false, puzzle.transform);
        var trigger = plate.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = Vector2.one;
        var pp = plate.AddComponent<PressurePlate>();
        pp.target = gateDoor;
        pp.requiredMass = 1.5f;
        pp.latch = true;

        // Вихід
        var exitDoorGo = Tiled("ExitDoor", new Vector2(27f, -2f), new Vector2(1f, 3f), metalTex, metalMat, 2, true, puzzle.transform);
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

        // Важіль
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

    static void BuildLamps()
    {
        var parent = new GameObject("Lamps");
        float[] xs = { -6f, 18.5f, 25.5f };
        foreach (float x in xs)
        {
            var root = new GameObject("Lamp");
            root.transform.SetParent(parent.transform, false);
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
    }
}
