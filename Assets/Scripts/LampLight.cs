using UnityEngine;

/// <summary>
/// Ліхтар: вночі світиться (гало + лампа). Гравець може вимкнути/увімкнути його клавішею E.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LampLight : MonoBehaviour
{
    public SpriteRenderer glow;
    public SpriteRenderer bulb;
    public Color glowColor = new Color(1f, 0.75f, 0.35f);
    public Color bulbOff = new Color(0.30f, 0.25f, 0.15f);
    public Color bulbOn = new Color(1f, 0.90f, 0.55f);
    [Range(0f, 1f)] public float nightThreshold = 0.2f;
    public float glowAlpha = 0.6f;

    bool manualOn = true;
    bool playerNear;
    float current;

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
        if (playerNear)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetHint(manualOn ? "E — вимкнути лампу" : "E — увімкнути лампу");
            if (Input.GetKeyDown(KeyCode.E))
            {
                manualOn = !manualOn;
                AudioManager.Lamp();
            }
        }

        float target = manualOn
            ? Mathf.Clamp01((DayNightCycle.Night - nightThreshold) / (1f - nightThreshold))
            : 0f;
        current = Mathf.MoveTowards(current, target, 2f * Time.deltaTime);

        if (glow != null)
        {
            Color g = glowColor;
            g.a = current * glowAlpha;
            glow.color = g;
        }
        if (bulb != null) bulb.color = Color.Lerp(bulbOff, bulbOn, current);
    }
}
