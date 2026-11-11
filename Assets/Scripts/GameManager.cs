using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Завдання 3. Система виконання завдань + стан гри (шестерні, смерті, перемога) та простий інтерфейс (IMGUI).
/// Завдання рівня:
///   1) зібрати всі шестерні;
///   2) відкрити браму (покласти важкий ящик на плиту);
///   3) потягнути важіль виходу (клавіша E);
///   4) дійти до виходу.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public float killY = -8f;

    int gears;
    int totalGears;
    int deaths;
    bool gateOpened;
    bool exitOpened;
    bool won;

    Transform player;
    PlayerController2D playerController;
    Vector3 spawnPoint;
    float lastDeathTime = -10f;

    string hint = "";
    float hintUntil;
    string toast = "";
    float toastUntil;

    GUIStyle labelStyle, centerStyle, bigStyle;

    public bool AllGearsCollected { get { return gears >= totalGears; } }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerController = p.GetComponent<PlayerController2D>();
            spawnPoint = player.position;
        }
        totalGears = Collectible.All.Count;
        Toast("A/D — рух, Пробіл — стрибок, E — взаємодія, R — перезапуск, M — звук", 6f);
    }

    void Update()
    {
        if (player != null && !won && player.position.y < killY)
            KillPlayer();

        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ---------- події ----------

    public void CollectGear()
    {
        gears++;
        if (AllGearsCollected) Toast("Усі шестерні зібрано! Тепер потрібно дістатися до важеля.");
        else Toast("Шестерню знайдено: " + gears + " / " + totalGears);
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
            gateOpened = true;
            Toast("Брама відчинилась!");
        }
    }

    public void KillPlayer()
    {
        if (won || Time.time - lastDeathTime < 0.3f) return;
        lastDeathTime = Time.time;
        deaths++;
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

    public void Win()
    {
        if (won) return;
        won = true;
        if (playerController != null) playerController.StopMoving();
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
    }

    string Task(string text, bool done)
    {
        return (done ? "[x] " : "[  ] ") + text;
    }

    void OnGUI()
    {
        InitStyles();

        // Панель завдань
        GUI.Box(new Rect(10, 10, 470, 150), GUIContent.none);
        GUI.Label(new Rect(20, 14, 450, 28), "Шестерні: " + gears + " / " + totalGears + "     Смерті: " + deaths, labelStyle);
        GUI.Label(new Rect(20, 44, 450, 28), Task("Зібрати всі шестерні", AllGearsCollected && totalGears > 0), labelStyle);
        GUI.Label(new Rect(20, 70, 450, 28), Task("Відкрити браму (ящик на плиту)", gateOpened), labelStyle);
        GUI.Label(new Rect(20, 96, 450, 28), Task("Потягнути важіль виходу (E)", exitOpened), labelStyle);
        GUI.Label(new Rect(20, 122, 450, 28), Task("Вибратися з вежі", won), labelStyle);

        // Повідомлення зверху
        if (Time.time < toastUntil)
            GUI.Label(new Rect(0, 20, Screen.width, 40), toast, centerStyle);

        // Підказка знизу
        if (Time.time < hintUntil)
            GUI.Label(new Rect(0, Screen.height - 70, Screen.width, 40), hint, centerStyle);

        // Перемога
        if (won)
        {
            GUI.Box(new Rect(Screen.width / 2f - 320, Screen.height / 2f - 90, 640, 180), GUIContent.none);
            GUI.Label(new Rect(Screen.width / 2f - 320, Screen.height / 2f - 70, 640, 60), "Час запущено! Тео вибрався з вежі!", bigStyle);
            GUI.Label(new Rect(Screen.width / 2f - 320, Screen.height / 2f + 10, 640, 40), "Смертей: " + deaths + ".  Натисніть R, щоб зіграти ще раз.", centerStyle);
        }
    }
}
