using UnityEngine;

/// <summary>
/// Зона виходу за дверима: якщо двері відкриті — рівень пройдено.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ExitZone : MonoBehaviour
{
    public Door exitDoor;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (exitDoor != null && exitDoor.IsOpen)
            GameManager.Instance.Win();
    }
}
