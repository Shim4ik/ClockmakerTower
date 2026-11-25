using UnityEngine;

/// <summary>
/// Здоров'я гравця: отримання шкоди від ворогів, відкидання, короткочасна невразливість з миготінням.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 3;
    public float invulnerableTime = 1f;

    public int Health { get; private set; }

    PlayerController2D controller;
    SpriteRenderer[] renderers;
    float invulnerableUntil;

    void Awake()
    {
        Health = maxHealth;
        controller = GetComponent<PlayerController2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    /// <summary>Повертає true, якщо шкоду завдано.</summary>
    public bool Damage(int amount, Vector2 knockbackVelocity)
    {
        if (Time.time < invulnerableUntil) return false;

        Health -= amount;
        invulnerableUntil = Time.time + invulnerableTime;
        if (controller != null) controller.Knockback(knockbackVelocity);

        if (Health <= 0)
        {
            Health = maxHealth;
            if (GameManager.Instance != null) GameManager.Instance.KillPlayer();
        }
        return true;
    }

    public void Heal()
    {
        Health = maxHealth;
    }

    public void ResetHealth()
    {
        Health = maxHealth;
        invulnerableUntil = 0f;
    }

    void Update()
    {
        // Миготіння під час невразливості
        bool blinking = Time.time < invulnerableUntil;
        bool visible = !blinking || ((int)(Time.time * 10f) % 2 == 0);
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].enabled = visible;
    }
}
