using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Робота «Реліз проєкту»: збірка Windows 64-bit без Development Build та Script Debugging.
/// Збірка кладеться у Builds/Release (поза Assets); поруч копіюються README, ліцензії, чек-лист і скрипт тестів з папки ReleaseKit.
/// Для релізу використовується найкраща за результатами роботи 15 версія (символи CLOCKMAKER_OPT_AI, CLOCKMAKER_OPT_HUD)
/// та символ CLOCKMAKER_RELEASE.
/// </summary>
public static class ReleaseBuilder
{
    const string SceneToBuild = "Assets/Scenes/Final_UI.unity";
    const string OutputFolder = "Builds/Release";
    const string ExeName = "ClockmakerTower.exe";
    const string KitFolder = "ReleaseKit";

    public const string Company = "ClockmakerStudio";
    public const string Product = "Clockmaker Tower";
    public const string Version = "1.0.0";

    [MenuItem("Clockmaker/Release/Build Windows Release (без Development Build)")]
    static void BuildRelease()
    {
        if (!File.Exists(SceneToBuild))
        {
            EditorUtility.DisplayDialog("Clockmaker", "Не знайдено сцену " + SceneToBuild + ".\\nСпочатку виберіть Clockmaker → Build Full Game (5 рівнів).", "OK");
            return;
        }

        ApplyReleaseSettings();

        try
        {
            if (Directory.Exists(OutputFolder)) Directory.Delete(OutputFolder, true);
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Clockmaker", "Не вдалося очистити " + OutputFolder + ": " + e.Message + "\\nЗакрийте гру, якщо вона запущена.", "OK");
            return;
        }

        string[] defines = { "CLOCKMAKER_OPT_AI", "CLOCKMAKER_OPT_HUD", "CLOCKMAKER_RELEASE" };
        var options = new BuildPlayerOptions
        {
            scenes = new[] { SceneToBuild },
            locationPathName = OutputFolder + "/" + ExeName,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,          // без Development Build та Script Debugging
            extraScriptingDefines = defines
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary s = report.summary;

        if (s.result != BuildResult.Succeeded)
        {
            Debug.LogError("Збірка не вдалась: " + s.result);
            EditorUtility.DisplayDialog("Clockmaker", "Збірка не вдалась. Подробиці в Console.", "OK");
            return;
        }

        CopyKit();
        WriteBuildInfo(s, defines);
        Debug.Log("Релізну збірку створено: " + Path.GetFullPath(OutputFolder));
        EditorUtility.RevealInFinder(OutputFolder + "/" + ExeName);
    }

    [MenuItem("Clockmaker/Release/Open Release Folder")]
    static void OpenFolder()
    {
        if (Directory.Exists(OutputFolder)) EditorUtility.RevealInFinder(OutputFolder + "/" + ExeName);
        else EditorUtility.DisplayDialog("Clockmaker", "Папки " + OutputFolder + " ще немає. Спочатку зберіть реліз.", "OK");
    }

    static void ApplyReleaseSettings()
    {
        PlayerSettings.companyName = Company;
        PlayerSettings.productName = Product;
        PlayerSettings.bundleVersion = Version;

        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.enableFrameTimingStats = true;

        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.connectProfiler = false;

        AssetDatabase.SaveAssets();
    }

    static void CopyKit()
    {
        if (!Directory.Exists(KitFolder))
        {
            Debug.LogWarning("Папку " + KitFolder + " не знайдено: README та ліцензії не скопійовано.");
            return;
        }
        foreach (string file in Directory.GetFiles(KitFolder))
            File.Copy(file, Path.Combine(OutputFolder, Path.GetFileName(file)), true);
    }

    static long FolderSize(string folder)
    {
        long total = 0;
        foreach (string f in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
            total += new FileInfo(f).Length;
        return total;
    }

    static void WriteBuildInfo(BuildSummary s, string[] defines)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Продукт: " + Product + " " + Version);
        sb.AppendLine("Компанія: " + Company);
        sb.AppendLine("Unity: " + Application.unityVersion);
        sb.AppendLine("Цільова платформа: Windows 64-bit (StandaloneWindows64)");
        sb.AppendLine("Development Build: " + EditorUserBuildSettings.development + "; Script Debugging: " + EditorUserBuildSettings.allowDebugging);
        sb.AppendLine("Перша (і єдина) сцена: " + SceneToBuild);
        sb.AppendLine("Символи компіляції: " + string.Join(";", defines));

        try
        {
            sb.AppendLine("Scripting Backend: " + PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone));
            var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);
            sb.AppendLine("Графічні API (за пріоритетом): " + string.Join(", ", Array.ConvertAll(apis, a => a.ToString())));
        }
        catch (Exception e)
        {
            sb.AppendLine("Додаткові параметри недоступні: " + e.Message);
        }

        sb.AppendLine("Вікно за замовчуванням: " + PlayerSettings.defaultScreenWidth + "x" + PlayerSettings.defaultScreenHeight + ", " + PlayerSettings.fullScreenMode + ", змінюване: " + PlayerSettings.resizableWindow);
        sb.AppendLine("Результат збірки: " + s.result + "; час збірки: " + s.totalTime);
        sb.AppendLine("Розмір (totalSize за звітом Unity): " + (s.totalSize / 1024.0 / 1024.0).ToString("F2") + " МБ");
        sb.AppendLine("Розмір теки збірки: " + (FolderSize(OutputFolder) / 1024.0 / 1024.0).ToString("F2") + " МБ");
        sb.AppendLine("Дата збірки: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        File.WriteAllText(Path.Combine(OutputFolder, "build_info.txt"), sb.ToString(), Encoding.UTF8);
    }
}
