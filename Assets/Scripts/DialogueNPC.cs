using UnityEngine;

/// <summary>
/// Завдання 3. NPC-годинникар: діалог з гравцем (клавіша E), видає завдання,
/// а після виконання допомагає — відновлює здоров'я.
/// Діалог залежить від прогресу гравця (кількості зібраних шестерень).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DialogueNPC : MonoBehaviour
{
    public string npcName = "Годинникар Мартин";
    public Transform visual;

    bool playerNear;
    bool talking;
    bool rewarded;
    bool metBefore;
    int lineIndex;
    string[] lines;

    PlayerController2D playerController;
    PlayerHealth playerHealth;
    Vector3 visualBase;
    GUIStyle nameStyle, textStyle, hintStyle, tagStyle;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            playerController = p.GetComponent<PlayerController2D>();
            playerHealth = p.GetComponent<PlayerHealth>();
        }
        if (visual != null) visualBase = visual.localPosition;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerNear = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerNear = false;
    }

    void Update()
    {
        // Легке погойдування, щоб NPC виглядав «живим»
        if (visual != null)
            visual.localPosition = visualBase + Vector3.up * (Mathf.Sin(Time.time * 2f) * 0.04f);

        if (!talking)
        {
            if (playerNear)
            {
                GameManager.Instance.SetHint("E — поговорити з годинникарем");
                if (Input.GetKeyDown(KeyCode.E)) StartDialogue();
            }
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            lineIndex++;
            if (lineIndex >= lines.Length) EndDialogue();
        }
    }

    string[] BuildLines()
    {
        GameManager gm = GameManager.Instance;

        if (gm.AllGearsCollected && gm.TotalGears > 0)
        {
            if (!rewarded)
                return new[]
                {
                    "Ти зібрав усі шестерні! Чудова робота, Тео!",
                    "Тримай — я відновлю твої сили.",
                    "Тепер іди до важеля біля виходу — і час знову піде."
                };
            return new[] { "Поспішай до важеля біля виходу. Удачі, Тео!" };
        }

        if (!metBefore)
            return new[]
            {
                "Тео! Нарешті ти прокинувся. Я — старий годинникар Мартин.",
                "Вежа зупинила час. Щоб вибратися, потрібно зібрати три шестерні головного механізму.",
                "Стережись механічних сторожів: вони нападають, якщо помітять тебе.",
                "Їх можна оглушити, якщо стрибнути їм на голову. Повертайся, коли збереш шестерні!"
            };

        int left = gm.TotalGears - gm.Gears;
        return new[] { "Тобі лишилося знайти шестерень: " + left + ". Не здавайся!" };
    }

    void StartDialogue()
    {
        lines = BuildLines();
        lineIndex = 0;
        talking = true;
        if (playerController != null) playerController.StopMoving();
        AudioManager.Lamp();
    }

    void EndDialogue()
    {
        talking = false;
        metBefore = true;
        if (playerController != null) playerController.canControl = true;
        GameManager.Instance.OnNpcTalked();

        if (GameManager.Instance.AllGearsCollected && !rewarded)
        {
            rewarded = true;
            if (playerHealth != null) playerHealth.Heal();
            GameManager.Instance.Toast("Годинникар відновив ваше здоров'я!");
            EffectsManager.Gear(transform.position + Vector3.up * 0.8f);
            AudioManager.Gear();
        }
    }

    void OnGUI()
    {
        if (UiState.MenuOpen) return;
        if (nameStyle == null)
        {
            nameStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            nameStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true };
            textStyle.normal.textColor = Color.white;
            hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleRight };
            hintStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            tagStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            tagStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
        }

        // Ім'я над NPC
        if (Camera.main != null)
        {
            Vector3 sp = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.3f);
            if (sp.z > 0f)
                GUI.Label(new Rect(sp.x - 100f, Screen.height - sp.y - 12f, 200f, 24f), npcName, tagStyle);
        }

        if (!talking) return;

        float w = Mathf.Min(900f, Screen.width - 40f);
        float x = (Screen.width - w) / 2f;
        float y = Screen.height - 190f;
        GUI.Box(new Rect(x, y, w, 160f), GUIContent.none);
        GUI.Label(new Rect(x + 20f, y + 10f, w - 40f, 30f), npcName, nameStyle);
        GUI.Label(new Rect(x + 20f, y + 45f, w - 40f, 80f), lines[lineIndex], textStyle);
        string next = lineIndex < lines.Length - 1 ? "[E] — далі" : "[E] — закрити";
        GUI.Label(new Rect(x + 20f, y + 125f, w - 40f, 28f), next, hintStyle);
    }
}
