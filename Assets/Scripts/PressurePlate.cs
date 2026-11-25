using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Нажимна плита: спрацьовує, коли сумарна маса об'єктів на ній перевищує поріг (механіка «вага»).
/// Гравець (маса 1) сам плиту не натискає — потрібен важкий ящик (маса 2).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PressurePlate : MonoBehaviour
{
    public Door target;
    public float requiredMass = 1.5f;
    [Tooltip("Якщо увімкнено — двері лишаються відкритими після першого натискання")]
    public bool latch = true;

    readonly HashSet<Rigidbody2D> bodies = new HashSet<Rigidbody2D>();
    SpriteRenderer sr;
    Color idleColor;
    bool pressed;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) idleColor = sr.color;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D body = other.attachedRigidbody;
        if (body != null && body.bodyType == RigidbodyType2D.Dynamic)
        {
            bodies.Add(body);
            Evaluate();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Rigidbody2D body = other.attachedRigidbody;
        if (body != null && bodies.Remove(body))
            Evaluate();
    }

    void Evaluate()
    {
        float mass = 0f;
        foreach (Rigidbody2D b in bodies) mass += b.mass;

        pressed = mass >= requiredMass;
        if (sr != null) sr.color = pressed ? new Color(0.35f, 0.9f, 0.4f) : idleColor;

        if (target == null) return;
        if (pressed) target.Open();
        else if (!latch) target.Close();
    }
}
