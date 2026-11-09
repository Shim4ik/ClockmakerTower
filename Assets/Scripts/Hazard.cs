using UnityEngine;

/// <summary>
/// Небезпека (шипи, гаряча пара тощо). Торкання — гравець повертається на старт.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Hazard : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance != null) GameManager.Instance.KillPlayer();
    }
}
