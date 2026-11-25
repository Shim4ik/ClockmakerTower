using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;

/// <summary>
/// Записує показники ігрового процесу у збірці та зберігає їх у файли.
/// F5 — запустити дослід (прогрів 10 с + запис 60 с зі сталим маршрутом PerfBot), F3 — показати/сховати панель.
/// Параметр командного рядка -autobench: дослід запускається сам після старту, після завершення гра закривається.
/// Вимірюється: час кадру, виділення керованої пам'яті на кадр (GC Allocated In Frame), кількість збирань сміття,
/// пік керованої купи, пік робочого набору процесу, CPU/GPU-час кадру через FrameTimingManager (якщо підтримується).
/// Результати: CSV по кадрах та текстовий підсумок у Application.persistentDataPath.
/// </summary>
public sealed class PerfRecorder : MonoBehaviour
{
    public float warmupSeconds = 10f;
    public float recordSeconds = 60f;

#if CLOCKMAKER_RELEASE
    private const string VersionLabel = "Release";
#elif CLOCKMAKER_OPT_AI && CLOCKMAKER_OPT_HUD
    private const string VersionLabel = "B2";
#elif CLOCKMAKER_OPT_AI
    private const string VersionLabel = "B1";
#elif CLOCKMAKER_OPT_HUD
    private const string VersionLabel = "B_HUD";
#else
    private const string VersionLabel = "A";
#endif

    private enum Phase { Idle, Warmup, Recording, Done }

    private Phase phase = Phase.Idle;
    private float phaseStart;
    private bool showPanel = true;
    private bool autoBench;
    private bool autoStarted;

    private readonly List<float> frameMs = new List<float>(16384);
    private readonly List<long> allocBytes = new List<long>(16384);
    private readonly List<float> cpuMs = new List<float>(16384);
    private readonly List<float> gpuMs = new List<float>(16384);
    private readonly FrameTiming[] timings = new FrameTiming[1];

    private ProfilerRecorder gcRecorder;
    private int gcStart;
    private long heapMax;
    private string summary = "";
    private string message = "";
    private PerfBot bot;
    private PlayerController2D player;
    private GUIStyle style;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.GetComponent<PlayerController2D>();
        bot = gameObject.AddComponent<PerfBot>();
        bot.enabled = false;

        foreach (string arg in Environment.GetCommandLineArgs())
            if (arg == "-autobench") autoBench = true;

        try { gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame"); }
        catch (Exception) { }
    }

    private void OnDestroy()
    {
        if (gcRecorder.Valid) gcRecorder.Dispose();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3)) showPanel = !showPanel;
        if (Input.GetKeyDown(KeyCode.F5) && (phase == Phase.Idle || phase == Phase.Done)) StartRun();

        // Автоматичний режим: через 2 с після старту запускаємо нову гру та дослід
        if (autoBench && !autoStarted && Time.realtimeSinceStartup > 2f)
        {
            MainMenu menu = UnityEngine.Object.FindAnyObjectByType<MainMenu>();
            if (menu != null)
            {
                autoStarted = true;
                menu.AutoStart();
                StartRun();
            }
        }
    }

    private void StartRun()
    {
        if (UiState.MenuOpen)
        {
            message = "Закрийте меню (натисніть «Нова гра») і повторіть F5.";
            return;
        }
        if (player == null)
        {
            message = "Гравця не знайдено.";
            return;
        }
        message = "";
        summary = "";
        phase = Phase.Warmup;
        phaseStart = Time.realtimeSinceStartup;
        bot.Begin(player);
    }

    private void LateUpdate()
    {
        if (phase == Phase.Idle || phase == Phase.Done) return;

        float now = Time.realtimeSinceStartup;

        if (phase == Phase.Warmup)
        {
            if (now - phaseStart >= warmupSeconds)
            {
                frameMs.Clear(); allocBytes.Clear(); cpuMs.Clear(); gpuMs.Clear();
                gcStart = GC.CollectionCount(0);
                heapMax = 0;
                phase = Phase.Recording;
                phaseStart = now;
            }
            return;
        }

        // Recording
        frameMs.Add(Time.unscaledDeltaTime * 1000f);
        allocBytes.Add(gcRecorder.Valid ? gcRecorder.LastValue : -1);

        long heap = GC.GetTotalMemory(false);
        if (heap > heapMax) heapMax = heap;

        FrameTimingManager.CaptureFrameTimings();
        if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
        {
            if (timings[0].cpuFrameTime > 0) cpuMs.Add((float)timings[0].cpuFrameTime);
            if (timings[0].gpuFrameTime > 0) gpuMs.Add((float)timings[0].gpuFrameTime);
        }

        if (now - phaseStart >= recordSeconds) Finish();
    }

    // ---------- статистика ----------

    private static float Percentile(List<float> sorted, float p)
    {
        if (sorted.Count == 0) return 0f;
        int idx = Mathf.Clamp(Mathf.CeilToInt(p * sorted.Count) - 1, 0, sorted.Count - 1);
        return sorted[idx];
    }

    private static long PercentileL(List<long> sorted, float p)
    {
        if (sorted.Count == 0) return 0;
        int idx = Mathf.Clamp(Mathf.CeilToInt(p * sorted.Count) - 1, 0, sorted.Count - 1);
        return sorted[idx];
    }

    private static string PeakWorkingSet()
    {
        try
        {
            using (var proc = System.Diagnostics.Process.GetCurrentProcess())
                return (proc.PeakWorkingSet64 / (1024 * 1024)) + " МБ";
        }
        catch (Exception)
        {
            return "недоступно";
        }
    }

    private void Finish()
    {
        phase = Phase.Done;
        bot.End();

        var ft = new List<float>(frameMs); ft.Sort();
        var cpu = new List<float>(cpuMs); cpu.Sort();
        var gpu = new List<float>(gpuMs); gpu.Sort();

        var al = new List<long>();
        foreach (long a in allocBytes) if (a >= 0) al.Add(a);
        al.Sort();

        int frames = frameMs.Count;
        double sumMs = 0; foreach (float v in frameMs) sumMs += v;
        long sumAlloc = 0; int framesWithAlloc = 0;
        foreach (long a in al) { sumAlloc += a; if (a > 0) framesWithAlloc++; }
        int gcCount = GC.CollectionCount(0) - gcStart;

        var sb = new StringBuilder();
        sb.AppendLine("Версія: " + VersionLabel + (autoBench ? " (autobench)" : ""));
        sb.AppendLine("Дата: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("Unity: " + Application.unityVersion + "; Development Build: " + Debug.isDebugBuild);
        sb.AppendLine("Платформа: " + Application.platform + "; ОС: " + SystemInfo.operatingSystem);
        sb.AppendLine("Процесор: " + SystemInfo.processorType + " (потоків: " + SystemInfo.processorCount + ")");
        sb.AppendLine("Пам'ять ПК: " + SystemInfo.systemMemorySize + " МБ");
        sb.AppendLine("Відеокарта: " + SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsDeviceType + ")");
        sb.AppendLine("Роздільність: " + Screen.width + "x" + Screen.height + "; режим вікна: " + Screen.fullScreenMode + "; Quality: " + QualitySettings.names[QualitySettings.GetQualityLevel()]);
        sb.AppendLine("VSync: " + QualitySettings.vSyncCount + "; targetFrameRate: " + Application.targetFrameRate);
        sb.AppendLine("Тривалість запису: " + recordSeconds + " с (прогрів " + warmupSeconds + " с); кадрів: " + frames);
        sb.AppendLine("--- Час кадру, мс ---");
        sb.AppendLine("Середній: " + (frames > 0 ? sumMs / frames : 0).ToString("F2") + "; середній FPS: " + (sumMs > 0 ? frames * 1000.0 / sumMs : 0).ToString("F1"));
        sb.AppendLine("Медіана: " + Percentile(ft, 0.5f).ToString("F2") + "; p95: " + Percentile(ft, 0.95f).ToString("F2") + "; p99: " + Percentile(ft, 0.99f).ToString("F2") + "; максимум: " + (ft.Count > 0 ? ft[ft.Count - 1] : 0).ToString("F2"));
        sb.AppendLine("--- GC Allocated In Frame (ProfilerRecorder), байт на кадр ---");
        if (al.Count > 0)
        {
            sb.AppendLine("Медіана: " + PercentileL(al, 0.5f) + "; p95: " + PercentileL(al, 0.95f) + "; максимум: " + al[al.Count - 1]);
            sb.AppendLine("Сума за запис: " + sumAlloc + " Б; кадрів з виділенням: " + framesWithAlloc + " з " + al.Count);
        }
        else
        {
            sb.AppendLine("недоступно (лічильник не підтримується у цій збірці)");
        }
        sb.AppendLine("Збирань сміття (покоління 0): " + gcCount + "; пік керованої купи: " + (heapMax / 1024) + " КБ");
        sb.AppendLine("Пік робочого набору процесу: " + PeakWorkingSet());
        sb.AppendLine("--- FrameTimingManager ---");
        sb.AppendLine(cpu.Count > 0
            ? "CPU, мс: медіана " + Percentile(cpu, 0.5f).ToString("F2") + "; p95 " + Percentile(cpu, 0.95f).ToString("F2")
            : "CPU: недоступно (платформа може не підтримувати)");
        sb.AppendLine(gpu.Count > 0
            ? "GPU, мс: медіана " + Percentile(gpu, 0.5f).ToString("F2") + "; p95 " + Percentile(gpu, 0.95f).ToString("F2")
            : "GPU: недоступно (платформа/графічний API не повертає GPU-час)");
        summary = sb.ToString();

        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string name = "perf_" + VersionLabel + "_" + Screen.width + "x" + Screen.height + "_" + SystemInfo.graphicsDeviceType + "_" + stamp;
        string basePath = Path.Combine(Application.persistentDataPath, name);
        try
        {
            var csv = new StringBuilder();
            csv.AppendLine("frame;frame_ms;gc_alloc_bytes");
            for (int i = 0; i < frames; i++)
                csv.AppendLine(i + ";" + frameMs[i].ToString("F3") + ";" + allocBytes[i]);
            File.WriteAllText(basePath + ".csv", csv.ToString());
            File.WriteAllText(basePath + "_summary.txt", summary, Encoding.UTF8);
            message = "Збережено: " + basePath + "_summary.txt";
        }
        catch (Exception e)
        {
            message = "Не вдалося зберегти файли: " + e.Message;
        }
        Debug.Log(summary + "\n" + message);

        if (autoBench) Invoke(nameof(QuitApp), 1.5f);
    }

    private void QuitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- панель ----------

    private void OnGUI()
    {
        if (!showPanel || phase == Phase.Recording) return;   // під час запису нічого не малюємо
        if (autoBench && phase != Phase.Done) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            style.normal.textColor = Color.white;
        }

        string title;
        switch (phase)
        {
            case Phase.Warmup: title = "Прогрів... (" + VersionLabel + ")"; break;
            case Phase.Done: title = "Готово (" + VersionLabel + ")"; break;
            default: title = "Версія " + VersionLabel + ": F5 — запустити дослід, F3 — сховати панель"; break;
        }

        string text = title + (message.Length > 0 ? "\n" + message : "") + (summary.Length > 0 ? "\n" + summary : "");
        float w = Mathf.Min(760f, Screen.width - 20f);
        float h = phase == Phase.Done ? Mathf.Min(460f, Screen.height - 20f) : 80f;
        Rect r = new Rect(10f, Screen.height - h - 10f, w, h);
        GUI.Box(r, GUIContent.none);
        GUI.Label(new Rect(r.x + 8f, r.y + 4f, r.width - 16f, r.height - 8f), text, style);
    }
}
