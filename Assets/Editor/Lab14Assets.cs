using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

/// <summary>
/// Практична №14: інтерфейс (TextMeshPro, Button, Slider), звук (Audio Mixer, джерела Music/SFX, просторове джерело).
/// Частина PrototypeSceneBuilder (partial).
/// </summary>
public static partial class PrototypeSceneBuilder
{
    const string SoundDir = "Assets/Sound";
    const string MusicDir = "Assets/Sound/Music";
    const string SfxDir = "Assets/Sound/SFX";
    const string FontDir = "Assets/Fonts";
    const string MixerPath = "Assets/Sound/GameAudio.mixer";

    static AudioClip musicClip, clickClip, worldClip;
    static TMP_FontAsset uiFont;

    // ======================= TMP Essential Resources =======================

    /// <summary>
    /// TextMeshPro не працює без TMP Essential Resources (файл TMP Settings). Якщо їх немає —
    /// намагаємось запустити імпорт і просимо повторити побудову сцени після його завершення.
    /// </summary>
    static bool EnsureTmpEssentials()
    {
        if (TMP_Settings.instance != null) return true;

        try
        {
            Type importer = Type.GetType("TMPro.TMP_PackageResourceImporter, Unity.TextMeshPro.Editor");
            if (importer != null)
            {
                var method = importer.GetMethod("ImportResources",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (method != null && method.GetParameters().Length == 3)
                    method.Invoke(null, new object[] { true, false, false });
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Автоматичний імпорт TMP Essential Resources не вдався: " + e.Message);
        }

        const string key = "Clockmaker_TmpWarned";
        if (!SessionState.GetBool(key, false))
        {
            SessionState.SetBool(key, true);
            EditorUtility.DisplayDialog("Clockmaker",
                "Потрібні TMP Essential Resources.\n\n" +
                "1) Window → TextMeshPro → Import TMP Essential Resources → Import.\n" +
                "2) Після імпорту виберіть Clockmaker → Build Final Scene.",
                "OK");
        }
        Debug.LogWarning("Побудову сцени відкладено: імпортуйте TMP Essential Resources (Window → TextMeshPro), потім Clockmaker → Build Final Scene.");
        return false;
    }

    // ======================= РЕСУРСИ =======================

    static void CreateLab14Assets()
    {
        EnsureDir(SoundDir);
        EnsureDir(MusicDir);
        EnsureDir(SfxDir);
        EnsureDir(FontDir);
        CreateSoundFiles();
        CreateUiFont();
    }

    static void WriteWav(string path, float seconds, Func<float, float> sample)
    {
        const int sampleRate = 22050;
        int n = Mathf.RoundToInt(seconds * sampleRate);
        using (var fs = new FileStream(path, FileMode.Create))
        using (var w = new BinaryWriter(fs))
        {
            int dataLen = n * 2;
            w.Write(new[] { 'R', 'I', 'F', 'F' });
            w.Write(36 + dataLen);
            w.Write(new[] { 'W', 'A', 'V', 'E' });
            w.Write(new[] { 'f', 'm', 't', ' ' });
            w.Write(16);
            w.Write((short)1);
            w.Write((short)1);
            w.Write(sampleRate);
            w.Write(sampleRate * 2);
            w.Write((short)2);
            w.Write((short)16);
            w.Write(new[] { 'd', 'a', 't', 'a' });
            w.Write(dataLen);
            for (int i = 0; i < n; i++)
            {
                float v = Mathf.Clamp(sample(i / (float)sampleRate), -1f, 1f);
                w.Write((short)(v * 30000f));
            }
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
    }

    static void ApplyAudioImport(string path, AudioCompressionFormat format, AudioClipLoadType loadType, bool forceMono, float quality)
    {
        var importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null) return;
        var s = importer.defaultSampleSettings;
        s.loadType = loadType;
        s.compressionFormat = format;
        s.quality = quality;
        s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        importer.defaultSampleSettings = s;
        importer.forceToMono = forceMono;
        importer.loadInBackground = false;
        importer.SaveAndReimport();
    }

    static void CreateSoundFiles()
    {
        const float TwoPi = 2f * Mathf.PI;

        // ---- Музична петля: 16 с, прогресія Am – F – C – G (двічі), арпеджіо в стилі «музичної скриньки» ----
        string musicPath = MusicDir + "/mus_tower_theme.wav";
        float[][] chords =
        {
            new[] { 220.00f, 261.63f, 329.63f, 440.00f },   // Am
            new[] { 174.61f, 220.00f, 261.63f, 349.23f },   // F
            new[] { 261.63f, 329.63f, 392.00f, 523.25f },   // C
            new[] { 196.00f, 246.94f, 293.66f, 392.00f }    // G
        };
        float[] bass = { 110.00f, 87.31f, 130.81f, 98.00f };
        int[] pattern = { 0, 1, 2, 3, 2, 1, 2, 3 };
        WriteWav(musicPath, 16f, t =>
        {
            int bar = (int)(t / 2f);
            int chord = bar % 4;
            float tb = t - bar * 2f;
            int step = Mathf.Min((int)(tb / 0.25f), 7);
            float ts = tb - step * 0.25f;
            float f = chords[chord][pattern[step]];
            float env = Mathf.Exp(-ts * 9f) * (1f - Mathf.Exp(-ts * 200f)) * Mathf.Clamp01((0.25f - ts) / 0.01f);
            float lead = (Mathf.Sin(TwoPi * f * ts) + 0.3f * Mathf.Sin(TwoPi * 2f * f * ts)) * env * 0.28f;
            float low = Mathf.Sin(TwoPi * bass[chord] * tb) * Mathf.Exp(-tb * 0.8f) * 0.22f;
            return lead + low;
        });
        ApplyAudioImport(musicPath, AudioCompressionFormat.Vorbis, AudioClipLoadType.Streaming, false, 0.6f);
        musicClip = AssetDatabase.LoadAssetAtPath<AudioClip>(musicPath);

        // ---- Клік кнопки: короткий ефект ----
        string clickPath = SfxDir + "/sfx_click.wav";
        WriteWav(clickPath, 0.08f, t => Mathf.Sin(TwoPi * 1000f * t) * Mathf.Exp(-t * 50f) * 0.6f);
        ApplyAudioImport(clickPath, AudioCompressionFormat.PCM, AudioClipLoadType.DecompressOnLoad, true, 1f);
        clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>(clickPath);

        // ---- Просторовий ефект: гул механізму, 2 с, зациклюється (ціле число періодів) ----
        string worldPath = SfxDir + "/sfx_machine_hum.wav";
        WriteWav(worldPath, 2f, t =>
        {
            float pulse = 0.7f + 0.3f * Mathf.Sin(TwoPi * 2f * t);
            float hum = 0.30f * Mathf.Sin(TwoPi * 110f * t) + 0.15f * Mathf.Sin(TwoPi * 220f * t) + 0.10f * Mathf.Sin(TwoPi * 330f * t);
            return hum * pulse;
        });
        ApplyAudioImport(worldPath, AudioCompressionFormat.ADPCM, AudioClipLoadType.CompressedInMemory, true, 1f);
        worldClip = AssetDatabase.LoadAssetAtPath<AudioClip>(worldPath);
    }

    static void CreateUiFont()
    {
        string path = FontDir + "/UiFont SDF.asset";
        uiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (uiFont != null) return;

        // Шрифт Liberation Sans (SIL OFL) входить до проєкту: містить кирилицю, а «Include Font Data» увімкнено за замовчуванням
        string ttf = FontDir + "/LiberationSans-Regular.ttf";
        AssetDatabase.ImportAsset(ttf, ImportAssetOptions.ForceSynchronousImport);
        Font font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
        if (font == null)
        {
            Debug.LogWarning("Не знайдено " + ttf + ": TMP використає шрифт за замовчуванням (кирилиця може не відображатись).");
            return;
        }

        // Динамічний шрифт: кириличні символи додаються в атлас за потреби
        TMP_FontAsset created = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        if (created == null)
        {
            Debug.LogWarning("Не вдалося створити TMP Font Asset зі шрифту " + ttf + ".");
            return;
        }

        created.name = "UiFont SDF";
        AssetDatabase.CreateAsset(created, path);
        if (created.atlasTextures != null && created.atlasTextures.Length > 0 && created.atlasTextures[0] != null)
        {
            created.atlasTextures[0].name = "UiFont SDF Atlas";
            AssetDatabase.AddObjectToAsset(created.atlasTextures[0], created);
        }
        if (created.material != null)
        {
            created.material.name = "UiFont SDF Material";
            AssetDatabase.AddObjectToAsset(created.material, created);
        }
        EditorUtility.SetDirty(created);
        AssetDatabase.SaveAssets();
        uiFont = created;
    }

    // ======================= UI =======================

    static TMP_DefaultControls.Resources TmpRes()
    {
        return new TMP_DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
        };
    }

    static DefaultControls.Resources UiRes()
    {
        return new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
        };
    }

    static void SetRect(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static TMP_Text MakeText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions align, Color color,
        Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = TMP_DefaultControls.CreateText(TmpRes());
        go.name = name;
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TMP_Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        if (uiFont != null) t.font = uiFont;
        SetRect((RectTransform)go.transform, anchor, pivot, pos, size);
        return t;
    }

    static Button MakeButton(string name, Transform parent, string label, Vector2 pos, Vector2 size, float fontSize)
    {
        GameObject go = TMP_DefaultControls.CreateButton(TmpRes());
        go.name = name;
        go.transform.SetParent(parent, false);
        SetRect((RectTransform)go.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        var t = go.GetComponentInChildren<TMP_Text>();
        t.text = label;
        t.fontSize = fontSize;
        if (uiFont != null) t.font = uiFont;
        return go.GetComponent<Button>();
    }

    static Slider MakeSlider(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject go = DefaultControls.CreateSlider(UiRes());
        go.name = name;
        go.transform.SetParent(parent, false);
        SetRect((RectTransform)go.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        var s = go.GetComponent<Slider>();
        s.minValue = 0f;
        s.maxValue = 1f;
        s.wholeNumbers = false;
        s.value = 0.8f;
        // On Value Changed залишаємо порожнім: підписку виконує CourseAudioPanel.Start
        return s;
    }

    static void SetObjectRef(UnityEngine.Object target, string property, UnityEngine.Object value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(property);
        if (p == null) { Debug.LogWarning("Немає поля " + property + " у " + target.GetType().Name); return; }
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void BuildAudioAndUi()
    {
        // ---------- Canvas ----------
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // ---------- HUD: рахунок ----------
        var score = MakeText("ScoreText", canvasGo.transform, "Шестерні: 0 / 3", 44f, TextAlignmentOptions.Center,
            new Color(1f, 0.85f, 0.3f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(700f, 70f));
        var hud = score.gameObject.AddComponent<SessionHud>();
        SetObjectRef(hud, "label", score);

        MakeText("HintText", canvasGo.transform, "Tab — налаштування звуку", 28f, TextAlignmentOptions.Right,
            new Color(0.9f, 0.9f, 0.9f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 20f), new Vector2(700f, 40f));

        // ---------- Стартове меню ----------
        var menuPanel = new GameObject("MainMenuPanel", typeof(RectTransform), typeof(Image));
        menuPanel.transform.SetParent(canvasGo.transform, false);
        var mrt = (RectTransform)menuPanel.transform;
        mrt.anchorMin = Vector2.zero;
        mrt.anchorMax = Vector2.one;
        mrt.offsetMin = Vector2.zero;
        mrt.offsetMax = Vector2.zero;
        menuPanel.GetComponent<Image>().color = new Color(0.06f, 0.045f, 0.04f, 0.96f);

        MakeText("GameTitle", menuPanel.transform, "Вежа Годинникаря", 110f, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.3f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1500f, 150f));
        MakeText("GameSubtitle", menuPanel.transform, "2D-платформер з фізичними головоломками", 40f, TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.85f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(1500f, 60f));
        Button playButton = MakeButton("PlayButton", menuPanel.transform, "Грати", new Vector2(0f, 30f), new Vector2(520f, 90f), 42f);
        Button settingsButton = MakeButton("SettingsButton", menuPanel.transform, "Налаштування звуку", new Vector2(0f, -80f), new Vector2(520f, 90f), 42f);
        Button quitButton = MakeButton("QuitButton", menuPanel.transform, "Вийти", new Vector2(0f, -190f), new Vector2(520f, 90f), 42f);
        MakeText("MenuHint", menuPanel.transform, "Керування: A/D — рух, Пробіл — стрибок, E — взаємодія, Tab — налаштування звуку", 30f, TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(1700f, 50f));

        // ---------- Панель налаштувань (окремий контейнер) ----------
        var panel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGo.transform, false);
        SetRect((RectTransform)panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 460f));
        panel.GetComponent<Image>().color = new Color(0.08f, 0.06f, 0.05f, 0.92f);

        MakeText("Title", panel.transform, "Налаштування звуку", 40f, TextAlignmentOptions.Center, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(700f, 60f));

        MakeText("MusicLabel", panel.transform, "Музика", 32f, TextAlignmentOptions.Left, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-250f, 70f), new Vector2(200f, 50f));
        Slider musicSlider = MakeSlider("MusicSlider", panel.transform, new Vector2(100f, 70f), new Vector2(400f, 36f));

        MakeText("SfxLabel", panel.transform, "Ефекти", 32f, TextAlignmentOptions.Left, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-250f, -10f), new Vector2(200f, 50f));
        Slider sfxSlider = MakeSlider("SfxSlider", panel.transform, new Vector2(100f, -10f), new Vector2(400f, 36f));

        Button testButton = MakeButton("TestButton", panel.transform, "Перевірити звук", new Vector2(0f, -105f), new Vector2(420f, 70f), 32f);
        GameObject buttonGo = testButton.gameObject;
        Button closeButton = MakeButton("CloseButton", panel.transform, "Назад", new Vector2(0f, -195f), new Vector2(300f, 60f), 30f);
        // Button On Click залишаємо порожнім: підписку виконує CourseAudioPanel.Start

        var menu = canvasGo.AddComponent<SettingsMenu>();
        SetObjectRef(menu, "panel", panel);
        SetObjectRef(menu, "firstSelected", testButton);
        SetObjectRef(menu, "closeButton", closeButton);

        var mainMenu = canvasGo.AddComponent<MainMenu>();
        SetObjectRef(mainMenu, "menuPanel", menuPanel);
        SetObjectRef(mainMenu, "playButton", playButton);
        SetObjectRef(mainMenu, "settingsButton", settingsButton);
        SetObjectRef(mainMenu, "quitButton", quitButton);
        SetObjectRef(mainMenu, "settings", menu);
        SetObjectRef(menu, "mainMenu", mainMenu);
        panel.SetActive(false);   // контейнер налаштувань закритий за замовчуванням

        // ---------- EventSystem (один на сцену) ----------
        var esGo = new GameObject("EventSystem");
        var es = esGo.AddComponent<EventSystem>();
        es.firstSelectedGameObject = playButton.gameObject;
        Type inputModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModule != null)
        {
            var module = esGo.AddComponent(inputModule);
            var assign = inputModule.GetMethod("AssignDefaultActions");
            if (assign != null) assign.Invoke(module, null);
        }
        else
        {
            esGo.AddComponent<StandaloneInputModule>();
            Debug.LogWarning("Пакет Input System не знайдено: використано StandaloneInputModule.");
        }

        // ---------- AudioRoot: MusicSource, SfxSource, CourseAudioPanel ----------
        var audioRoot = new GameObject("AudioRoot");

        var musicGo = new GameObject("MusicSource");
        musicGo.transform.SetParent(audioRoot.transform, false);
        var musicSource = musicGo.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.volume = 1f;
        musicSource.pitch = 1f;
        musicSource.loop = true;

        var sfxGo = new GameObject("SfxSource");
        sfxGo.transform.SetParent(audioRoot.transform, false);
        var sfxSource = sfxGo.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.volume = 1f;
        sfxSource.pitch = 1f;
        sfxSource.loop = false;

        var panelComp = audioRoot.AddComponent<CourseAudioPanel>();
        SetObjectRef(panelComp, "musicSource", musicSource);
        SetObjectRef(panelComp, "sfxSource", sfxSource);
        SetObjectRef(panelComp, "musicClip", musicClip);
        SetObjectRef(panelComp, "clickClip", clickClip);
        SetObjectRef(panelComp, "musicSlider", musicSlider);
        SetObjectRef(panelComp, "sfxSlider", sfxSlider);
        SetObjectRef(panelComp, "testButton", testButton);

        // ---------- WorldSound: просторове (3D) джерело ----------
        // Слухач стоїть на камері (z = -10), тому джерело теж розміщено на z = -10:
        // інакше 10 одиниць відстані по глибині «з'їли» б майже весь діапазон 2–15.
        var world = new GameObject("WorldSound");
        world.transform.position = new Vector3(13f, -1f, -10f);
        var ws = world.AddComponent<AudioSource>();
        ws.clip = worldClip;
        ws.loop = true;
        ws.playOnAwake = true;
        ws.spatialBlend = 1f;
        ws.minDistance = 2f;
        ws.maxDistance = 15f;
        ws.rolloffMode = AudioRolloffMode.Logarithmic;
        ws.volume = 1f;

        // Видимий «механізм» як позначка джерела звуку
        var machine = new GameObject("WorldSoundMachine");
        machine.transform.position = new Vector3(13f, -1f, 0f);
        machine.transform.localScale = new Vector3(2f, 2f, 1f);
        var msr = machine.AddComponent<SpriteRenderer>();
        msr.sprite = gearSprite;
        msr.color = new Color(1f, 0.6f, 0.2f, 0.9f);
        msr.sortingOrder = -5;
        machine.AddComponent<Spinner>().degreesPerSecond = 40f;

        // Якщо мікшер уже створений — одразу підключаємо
        if (AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath) != null)
            WireMixerInScene(false);
        else
            Debug.Log("Audio Mixer ще не створено. Створіть Assets/Sound/GameAudio.mixer (див. README), потім виконайте Clockmaker → Wire Audio Mixer.");
    }

    // ======================= ПІДКЛЮЧЕННЯ МІКШЕРА =======================

    [MenuItem("Clockmaker/Wire Audio Mixer")]
    public static void WireAudioMixerMenu()
    {
        WireMixerInScene(true);
    }

    static void WireMixerInScene(bool showDialogs)
    {
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (mixer == null)
        {
            if (showDialogs)
                EditorUtility.DisplayDialog("Clockmaker", "Не знайдено " + MixerPath + ".\nСтворіть Audio Mixer з назвою GameAudio у папці Assets/Sound.", "OK");
            return;
        }

        AudioMixerGroup[] music = mixer.FindMatchingGroups("Master/Music");
        AudioMixerGroup[] sfx = mixer.FindMatchingGroups("Master/SFX");
        if (music.Length == 0 || sfx.Length == 0)
        {
            string msg = "У мікшері GameAudio мають бути групи Music та SFX під Master.";
            Debug.LogError(msg);
            if (showDialogs) EditorUtility.DisplayDialog("Clockmaker", msg, "OK");
            return;
        }

        var panel = UnityEngine.Object.FindAnyObjectByType<CourseAudioPanel>();
        if (panel != null)
        {
            SetObjectRef(panel, "mixer", mixer);
            var so = new SerializedObject(panel);
            var ms = so.FindProperty("musicSource").objectReferenceValue as AudioSource;
            var ss = so.FindProperty("sfxSource").objectReferenceValue as AudioSource;
            if (ms != null) { ms.outputAudioMixerGroup = music[0]; EditorUtility.SetDirty(ms); }
            if (ss != null) { ss.outputAudioMixerGroup = sfx[0]; EditorUtility.SetDirty(ss); }
        }

        GameObject world = GameObject.Find("WorldSound");
        if (world != null)
        {
            var ws = world.GetComponent<AudioSource>();
            ws.outputAudioMixerGroup = sfx[0];
            EditorUtility.SetDirty(ws);
        }

        var manager = UnityEngine.Object.FindAnyObjectByType<AudioManager>();
        if (manager != null)
        {
            manager.sfxGroup = sfx[0];
            manager.musicGroup = music[0];
            EditorUtility.SetDirty(manager);
        }

        float tmp;
        bool musicExposed = mixer.GetFloat("MusicVolume", out tmp);
        bool sfxExposed = mixer.GetFloat("SfxVolume", out tmp);
        if (!musicExposed || !sfxExposed)
            Debug.LogWarning("Не виставлено (Expose to script) параметри мікшера: " +
                             (musicExposed ? "" : "MusicVolume ") + (sfxExposed ? "" : "SfxVolume") +
                             ". Назви мають збігатися з кодом.");

        Debug.Log("Audio Mixer підключено до сцени.");
        if (showDialogs)
        {
            // Збереження лише при ручному запуску з меню (під час побудови сцену зберігає Build())
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            EditorUtility.DisplayDialog("Clockmaker", "Audio Mixer підключено до сцени й збережено.", "OK");
        }
    }
}
