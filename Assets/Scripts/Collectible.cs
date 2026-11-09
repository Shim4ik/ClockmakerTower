using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Завдання 2. Збір предметів: шестерня-деталь головного механізму (Trigger).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Collectible : MonoBehaviour
{
    public float spinSpeed = 90f;
    public float bobAmplitude = 0.15f;
    public float bobSpeed = 2f;

    /// <summary>Усі шестерні на сцені (самореєстрація, щоб не шукати їх через Find...).</summary>
    public static readonly List<Collectible> All = new List<Collectible>();

    Vector3 startPos;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        transform.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
        transform.position = startPos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobAmplitude);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance != null) GameManager.Instance.CollectGear();
        Destroy(gameObject);
    }
}
