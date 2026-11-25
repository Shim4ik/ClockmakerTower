using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Побудова повної гри (5 рівнів): меню Clockmaker → Build Full Game.
/// Використовує ті самі допоміжні методи, що й попередні лабораторні, але замість одного рівня створює
/// п'ять шаблонів рівнів і LevelManager. Сцена зберігається як Assets/Scenes/Final_UI.unity.
/// </summary>
public static partial class PrototypeSceneBuilder
{
    [MenuItem("Clockmaker/Build Full Game (5 рівнів)")]
    public static void BuildFull()
    {
        if (!EnsureTmpEssentials()) return;

        EnsureDir("Assets/Scenes");
        EnsureDir(SpriteDir);
        EnsureDir(MaterialDir);

        CreateAssets();
        CreateLab12Assets();
        CreateLab13Assets();
        CreateLab14Assets();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Camera cam;
        SpriteRenderer overlay;
        BuildCamera(out cam, out overlay);
        SpriteRenderer backdrop = BuildBackdrop();
        BuildBackgroundGears();
        BuildExtraBackgroundGears();
        GameObject player = BuildPlayer();
        BuildManagers();
        BuildAudioAndUi();
        AddContinueButton();
        BuildAllLevels();   // п'ять рівнів-шаблонів + LevelManager

        cam.GetComponent<CameraFollow>().target = player.transform;

        var dn = new GameObject("DayNightCycle");
        var cycle = dn.AddComponent<DayNightCycle>();
        cycle.cam = cam;
        cycle.nightOverlay = overlay;
        cycle.backdrop = backdrop;

        new GameObject("GameManager").AddComponent<GameManager>();
        new GameObject("PerfRecorder").AddComponent<PerfRecorder>();

        EditorSceneManager.SaveScene(scene, ScenePath);

        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        foreach (var s in list) if (s.path == ScenePath) found = true;
        if (!found) list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Повну гру (5 рівнів) створено: " + ScenePath + ". Натисніть Play!");
    }

    /// <summary>Додаткові шестерні на задньому плані для довших рівнів.</summary>
    static void BuildExtraBackgroundGears()
    {
        var parent = new GameObject("BackgroundGears_Extra");
        Vector2[] pos = { new Vector2(40, 3), new Vector2(50, 2.5f), new Vector2(60, 4) };
        float[] speed = { 7, -9, 6 };
        float[] size = { 7, 9, 6 };
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

    /// <summary>У стартовому меню: «Нова гра» + кнопка «Продовжити» (видима, лише якщо є збережений прогрес).</summary>
    static void AddContinueButton()
    {
        var mainMenu = UnityEngine.Object.FindAnyObjectByType<MainMenu>();
        GameObject panel = GameObject.Find("MainMenuPanel");
        GameObject play = GameObject.Find("PlayButton");
        GameObject settings = GameObject.Find("SettingsButton");
        GameObject quit = GameObject.Find("QuitButton");
        if (mainMenu == null || panel == null || play == null)
        {
            Debug.LogWarning("Не вдалося додати кнопку «Продовжити»: стартове меню не знайдено.");
            return;
        }

        ((RectTransform)play.transform).anchoredPosition = new Vector2(0f, 70f);
        var playText = play.GetComponentInChildren<TMP_Text>();
        if (playText != null) playText.text = "Нова гра";
        if (settings != null) ((RectTransform)settings.transform).anchoredPosition = new Vector2(0f, -150f);
        if (quit != null) ((RectTransform)quit.transform).anchoredPosition = new Vector2(0f, -260f);

        Button cont = MakeButton("ContinueButton", panel.transform, "Продовжити", new Vector2(0f, -40f), new Vector2(520f, 90f), 42f);
        SetObjectRef(mainMenu, "continueButton", cont);
    }
}
