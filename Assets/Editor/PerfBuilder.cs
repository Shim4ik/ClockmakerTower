using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Практична №15: збірки Windows Player для порівняння A/B.
/// A — вихідна версія; B1 — оптимізація AI (CLOCKMAKER_OPT_AI); B2 — B1 + оптимізація HUD (CLOCKMAKER_OPT_HUD).
/// Збірки A/B1/B2 — Development Build з Autoconnect Profiler; Final — без Development Build.
/// Символи компіляції передаються лише для збірки (extraScriptingDefines), налаштування проєкту не змінюються.
/// </summary>
public static class PerfBuilder
{
    const string SceneToBuild = "Assets/Scenes/Final_UI.unity";

    [MenuItem("Clockmaker/Perf/Build A (Development)")]
    static void BuildA() { Build("A", new string[0], true); }

    [MenuItem("Clockmaker/Perf/Build B1 - AI (Development)")]
    static void BuildB1() { Build("B1", new[] { "CLOCKMAKER_OPT_AI" }, true); }

    [MenuItem("Clockmaker/Perf/Build B2 - AI + HUD (Development)")]
    static void BuildB2() { Build("B2", new[] { "CLOCKMAKER_OPT_AI", "CLOCKMAKER_OPT_HUD" }, true); }

    [MenuItem("Clockmaker/Perf/Build Final (без Development Build, B2)")]
    static void BuildFinal() { Build("Final", new[] { "CLOCKMAKER_OPT_AI", "CLOCKMAKER_OPT_HUD" }, false); }

    static void Build(string name, string[] defines, bool development)
    {
        if (!File.Exists(SceneToBuild))
        {
            EditorUtility.DisplayDialog("Clockmaker", "Не знайдено сцену " + SceneToBuild + ".\nСпочатку виберіть Clockmaker → Build Final Scene.", "OK");
            return;
        }

        PlayerSettings.enableFrameTimingStats = true;   // для CPU/GPU-часу кадру у збірці

        string folder = "Builds/" + name;
        var options = new BuildPlayerOptions
        {
            scenes = new[] { SceneToBuild },
            locationPathName = folder + "/ClockmakerTower.exe",
            target = BuildTarget.StandaloneWindows64,
            options = development ? (BuildOptions.Development | BuildOptions.ConnectWithProfiler) : BuildOptions.None,
            extraScriptingDefines = defines
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary s = report.summary;

        string info = "Збірка: " + name + "\nРезультат: " + s.result + "\nРозмір (totalSize): " + (s.totalSize / 1024.0 / 1024.0).ToString("F2") + " МБ" +
                      "\nЧас збірки: " + s.totalTime + "\nDevelopment Build: " + development + "\nСимволи: " + string.Join(";", defines);
        Debug.Log(info);
        if (s.result == BuildResult.Succeeded)
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(folder + "/build_info.txt", info);
            EditorUtility.RevealInFinder(folder + "/ClockmakerTower.exe");
        }
        else
        {
            EditorUtility.DisplayDialog("Clockmaker", "Збірка не вдалась. Подробиці в Console.", "OK");
        }
    }
}
