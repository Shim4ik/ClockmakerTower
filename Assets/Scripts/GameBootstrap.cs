using System;
using UnityEngine;

/// <summary>
/// Початкові налаштування збірки (до завантаження сцени): у релізі вертикальна синхронізація ввімкнена,
/// щоб гра не працювала на тисячах кадрів на секунду. Параметри командного рядка:
///   -novsync   — вимкнути VSync;
///   -autobench — автоматичний дослід продуктивності (також вимикає VSync).
/// У редакторі нічого не змінюється.
/// </summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
#if !UNITY_EDITOR
        bool noVsync = false;
        foreach (string arg in Environment.GetCommandLineArgs())
        {
            if (arg == "-novsync" || arg == "-autobench") noVsync = true;
        }
        QualitySettings.vSyncCount = noVsync ? 0 : 1;
        Application.targetFrameRate = -1;
#endif
    }
}
