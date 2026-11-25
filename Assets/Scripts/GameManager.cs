using UnityEngine;

public enum GameState { Playing, LevelComplete, Victory }

/// <summary>
/// Стан гри: поточний рівень, шестерні, здоров'я, смерті, час, перемога та простий інтерфейс (IMGUI).
/// Завдання рівня формуються за його особливостями (NPC, шестерні, бос, брами, важіль, вихід).
/// Перехід між рівнями виконує LevelManager.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>Повідомляє HUD про зміну рахунку: (зібрано, усього).</summary>
    public static event System.Action<int, int> GearsChanged;

    public float killY = -8f;

    public GameState State { get; private set; }

    int gears;
    int totalGears;
    int gatesOpened;
    bool exitOpened;
    bool talkedToNpc;

    int deaths;        // за всю гру
    int levelDeaths;   // на поточному рівні
    float runTime;     // сума часу пройдених рівнів
    float levelStart;
    float levelFinalTime;

    LevelData level;
    int levelIndex;

    PlayerHealth health;
    Transform player;
    PlayerController2D playerController;
    Vector3 spawnPoint;
    float lastDeathTime = -10f;

    string hint = "";
    float hintUntil;
    string toast = "";
    float toastUntil;

    GUIStyle labelStyle, centerStyle, bigStyle, smallStyle;

    public bool AllGearsCollected { get { return gears >= totalGears; } }
    public int Gears { get { return gears; } }
    public int TotalGears { get { return totalGears; } }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        EnsurePlayer();
    }

    void EnsurePlayer()
    {
        if (player != null) return;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        player = p.transform;
        playerController = p.GetComponent<PlayerController2D>();
        health = p.GetComponent<PlayerHealth>();
    }

    // ---------- рівні ----------

    public void ResetRun()
    {
        deaths = 0;
        runTime = 0f;
    }

    public void BeginLevel(LevelData data, int index)
    {
        EnsurePlayer();
        level = data;
        levelIndex = index;
        spawnPoint = (data != null && data.spawn != null) ? data.spawn.position : Vector3.zero;

        gears = 0;
        gatesOpened = 0;
        exitOpened = false;
        talkedToNpc = false;
        levelDeaths = 0;
        State = GameState.Playing;
        levelStart = Time.time;
        lastDeathTime = -10f;

        totalGears = Collectible.All.Count;
        if (GearsChanged != null) GearsChanged(gears, totalGears);

        if (health != null) health.ResetHealth();
        if (playerController != null)
        {
            playerController.canControl = true;
            playerController.Respawn(spawnPoint);
        }
        if (CameraFollow.Instance != null && player != null) CameraFollow.Instance.Snap(player.position);

        if (data != null)
        {
            int count = LevelManager.Instance != null ? LevelManager.Instance.LevelCount : 1;
            Toast("Рівень " + (index + 1) + " / " + count + " — " + data.levelName + ". " + data.intro, 5f);
        }
    }

    public void SetSpawn(Vector3 position)
    {
        spawnPoint = position;
    }

    public bool CanOpenExit(out string reason)
    {
        reason = "";
        if (!AllGearsCollected)
        {
            reason = "Механізм не запуститься: зберіть усі шестерні!";
            return false;
        }
        if (EnemyAI.MustDefeatAlive > 0)
        {
            reason = "Спершу переможіть головного сторожа: стрибайте йому на голову!";
            return false;
        }
        return true;
    }

    void Update()
    {
        if (player == null) EnsurePlayer();

        if (State != GameState.Playing)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.E))
                Advance();
            return;
        }

        if (player != null && player.position.y < killY)
            KillPlayer();

        if (Input.GetKeyDown(KeyCode.R) && LevelManager.Instance != null)
            LevelManager.Instance.RestartLevel();
    }

    void Advance()
    {
        if (LevelManager.Instance == null) return;
        if (State == GameState.LevelComplete) LevelManager.Instance.NextLevel();
        else if (State == GameState.Victory) LevelManager.Instance.StartNewGame();
    }

    // ---------- події ----------

    public void CollectGear()
    {
        gears++;
        if (GearsChanged != null) GearsChanged(gears, totalGears);
        if (AllGearsCollected) Toast("Усі шестерні зібрано! Тепер потрібно дістатися до важеля.");
        else Toast("Шестерню знайдено: " + gears + " / " + totalGears);
    }

    public void OnNpcTalked()
    {
        talkedToNpc = true;
    }

    public void OnDoorOpened(bool isExit)
    {
        if (isExit)
        {
            exitOpened = true;
            Toast("Вихід відкрито!");
        }
        else
        {
            gatesOpened++;
            Toast("Брама відчинилась!");
        }
    }

    public void KillPlayer()
    {
        if (State != GameState.Playing || Time.time - lastDeathTime < 0.3f) return;
        lastDeathTime = Time.time;
        deaths++;
        levelDeaths++;
        if (health != null) health.ResetHealth();
        if (player != null)
        {
            Vector3 pos = player.position;
            pos.y = Mathf.Max(pos.y, -3f);
            EffectsManager.Death(pos);
        }
        AudioManager.Death();
        if (playerController != null) playerController.Respawn(spawnPoint);
        Toast("Спробуйте ще раз!", 1.5f);
    }

    /// <summary>Виклик із зони виходу: рівень пройдено.</summary>
    public void Win()
    {
        CompleteLevel();
    }

    void CompleteLevel()
    {
        if (State != GameState.Playing) return;

        levelFinalTime = Time.time - levelStart;
        runTime += levelFinalTime;

        bool last = LevelManager.Instance == null || LevelManager.Instance.IsLastLevel;
        State = last ? GameState.Victory : GameState.LevelComplete;

        if (LevelManager.Instance != null)
        {
            if (last) LevelManager.Instance.ClearProgress();
            else LevelManager.Instance.SaveProgress(levelIndex + 1);
        }

        if (playerController != null) playerController.StopMoving();
        AudioManager.Gear();
        EffectsManager.Gear(player != null ? player.position : Vector3.zero);
    }

    public void SetHint(string text)
    {
        hint = text;
        hintUntil = Time.time + 0.1f;
    }

    public void Toast(string text, float seconds = 2.5f)
    {
        toast = text;
        toastUntil = Time.time + seconds;
    }

    // ---------- інтерфейс ----------

    void InitStyles()
    {
        if (labelStyle != null) return;

        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 18 };
        labelStyle.normal.textColor = Color.white;

        centerStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
        centerStyle.normal.textColor = Color.white;

        bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 32, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        bigStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);

        smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        smallStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
    }

    string Task(string text, bool done)
    {
        return (done ? "[x] " : "[  ] ") + text;
    }

    float LevelTime { get { return State == GameState.Playing ? Time.time - levelStart : levelFinalTime; } }

    static string FormatTime(float seconds)
    {
        int s = Mathf.Max(0, (int)seconds);
        return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
    }

    // ---------- рядки HUD (IMGUI) ----------
    // Версія A: рядки створюються конкатенацією щокадру (нові виділення пам'яті).
    // Версія B (CLOCKMAKER_OPT_HUD): рядки кешуються й перебудовуються лише при зміні стану.

    const int HudLines = 7;

#if CLOCKMAKER_OPT_HUD
    private readonly string[] hudCache = new string[HudLines];
    private long hudKey = long.MinValue;
#endif

    bool BossAlive { get { return level != null && level.hasBoss && EnemyAI.MustDefeatAlive > 0; } }

    private string BuildHudLine(int i)
    {
        int hp = health != null ? health.Health : 0;
        int maxHp = health != null ? health.maxHealth : 0;

        if (i == 0)
        {
            int count = LevelManager.Instance != null ? LevelManager.Instance.LevelCount : 1;
            return level != null ? "Рівень " + (levelIndex + 1) + " / " + count + " — " + level.levelName : "";
        }
        if (i == 1)
            return "Здоров'я: " + hp + " / " + maxHp + "   Смерті: " + deaths + "   Час: " + FormatTime(LevelTime);
        if (level == null) return "";

        // завдання рівня (до 5)
        int n = i - 2;
        int idx = 0;
        if (level.hasNpc) { if (idx++ == n) return Task("Поговорити з годинникарем (E)", talkedToNpc); }
        if (idx++ == n) return Task("Зібрати всі шестерні", AllGearsCollected && totalGears > 0);
        if (level.hasBoss) { if (idx++ == n) return Task("Перемогти головного сторожа (стрибок на голову)", !BossAlive); }
        if (level.gateCount > 0) { if (idx++ == n) return Task(level.gateTask, gatesOpened >= level.gateCount); }
        if (idx++ == n) return Task("Потягнути важіль виходу (E)", exitOpened);
        if (idx++ == n) return Task("Дістатися до виходу", State != GameState.Playing);
        return "";
    }

    private string HudLine(int i)
    {
#if CLOCKMAKER_OPT_HUD
        int hp = health != null ? health.Health : 0;
        int flags = (talkedToNpc ? 1 : 0) | ((AllGearsCollected && totalGears > 0) ? 2 : 0) | (gatesOpened << 2) | (exitOpened ? 16 : 0)
                  | (BossAlive ? 32 : 0) | (State != GameState.Playing ? 64 : 0);
        long key = ((((long)levelIndex * 1000 + deaths) * 10 + hp) * 100000 + (int)LevelTime) * 128 + flags;
        if (key != hudKey)
        {
            hudKey = key;
            for (int k = 0; k < hudCache.Length; k++) hudCache[k] = BuildHudLine(k);
        }
        return hudCache[i];
#else
        return BuildHudLine(i);
#endif
    }

    void OnGUI()
    {
        if (UiState.MenuOpen) return;   // не накладаємось на меню
        InitStyles();

        // Панель завдань
        GUI.Box(new Rect(10, 10, 560, 206), GUIContent.none);
        for (int i = 0; i < HudLines; i++)
        {
            string text = HudLine(i);
            if (text.Length > 0)
                GUI.Label(new Rect(20, 14 + i * 26, 540, 28), text, labelStyle);
        }

        // Повідомлення зверху
        if (Time.time < toastUntil)
            GUI.Label(new Rect(0, Mathf.Max(80f, Screen.height * 0.09f), Screen.width, 40), toast, centerStyle);

        // Підказка знизу
        if (Time.time < hintUntil)
            GUI.Label(new Rect(0, Screen.height - 70, Screen.width, 40), hint, centerStyle);

        // Екрани завершення рівня та перемоги
        if (State != GameState.Playing) DrawEndPanel();
    }

    void DrawEndPanel()
    {
        float w = 720f, h = 300f;
        float x = (Screen.width - w) / 2f;
        float y = (Screen.height - h) / 2f;
        GUI.Box(new Rect(x, y, w, h), GUIContent.none);

        if (State == GameState.Victory)
        {
            GUI.Label(new Rect(x, y + 20, w, 60), "Час запущено! Тео вибрався з вежі!", bigStyle);
            GUI.Label(new Rect(x, y + 95, w, 36), "Загальний час: " + FormatTime(runTime) + "     Смертей: " + deaths, smallStyle);
            GUI.Label(new Rect(x, y + 135, w, 36), "Ви пройшли всі " + (LevelManager.Instance != null ? LevelManager.Instance.LevelCount : 1) + " рівнів вежі.", smallStyle);
            if (GUI.Button(new Rect(x + w / 2f - 150f, y + 210, 300, 56), "Грати ще раз (Enter)")) Advance();
        }
        else
        {
            GUI.Label(new Rect(x, y + 20, w, 60), "Рівень пройдено!", bigStyle);
            GUI.Label(new Rect(x, y + 95, w, 36), "Час рівня: " + FormatTime(levelFinalTime) + "     Смертей на рівні: " + levelDeaths, smallStyle);
            GUI.Label(new Rect(x, y + 135, w, 36), "Шестерні: " + gears + " / " + totalGears + "     Прогрес збережено", smallStyle);
            if (GUI.Button(new Rect(x + w / 2f - 150f, y + 210, 300, 56), "Далі (Enter)")) Advance();
        }
    }
}
