using UnityEngine;

/// <summary>
/// Завдання 4. Динамічне освітлення: цикл «день — вечір — ніч — світанок».
/// У 2D без URP це реалізовано кольором фону/задника та напівпрозорим «нічним» накладенням на камері.
/// Клавіша N — перемкнути час доби на чверть доби вперед.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    public static float Night { get; private set; }   // 0 = день, 1 = глибока ніч

    [Tooltip("Тривалість повної доби, секунд")]
    public float cycleSeconds = 90f;
    [Range(0f, 1f)] public float timeOfDay = 0.40f;   // 0 = північ, 0.5 = полудень
    public bool autoAdvance = true;
    public float maxDarkness = 0.6f;

    public Camera cam;
    public SpriteRenderer nightOverlay;
    public SpriteRenderer backdrop;

    public Color dayBackdrop = new Color(0.90f, 0.78f, 0.60f);
    public Color nightBackdrop = new Color(0.10f, 0.12f, 0.28f);
    public Color overlayColor = new Color(0.02f, 0.03f, 0.12f);

    GUIStyle style;

    void Update()
    {
        if (autoAdvance) timeOfDay = (timeOfDay + Time.deltaTime / cycleSeconds) % 1f;
        if (Input.GetKeyDown(KeyCode.N)) timeOfDay = (timeOfDay + 0.25f) % 1f;
        Apply();
    }

    void Apply()
    {
        // 0 опівночі, 1 опівдні
        float daylight = Mathf.Clamp01(0.5f - 0.5f * Mathf.Cos(timeOfDay * 2f * Mathf.PI));
        daylight = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(daylight * 1.5f));
        Night = 1f - daylight;

        Color back = Color.Lerp(nightBackdrop, dayBackdrop, daylight);
        if (cam != null) cam.backgroundColor = back * 0.5f;
        if (backdrop != null) backdrop.color = back;
        if (nightOverlay != null)
        {
            Color c = overlayColor;
            c.a = Night * maxDarkness;
            nightOverlay.color = c;
        }
    }

    string Phase()
    {
        if (timeOfDay < 0.20f || timeOfDay >= 0.80f) return "Ніч";
        if (timeOfDay < 0.30f) return "Світанок";
        if (timeOfDay < 0.70f) return "День";
        return "Вечір";
    }

    void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleRight };
            style.normal.textColor = Color.white;
        }
        GUI.Label(new Rect(Screen.width - 410, 10, 400, 30), "Час доби: " + Phase() + "   (N — змінити)", style);
    }
}
