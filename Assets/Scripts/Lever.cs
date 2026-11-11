using UnityEngine;

/// <summary>
/// Завдання 2. Взаємодія з об'єктами: важіль активується клавішею E.
/// Вихід відкривається, лише якщо зібрано всі шестерні.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Lever : MonoBehaviour
{
    public Door door;
    public Transform pivot;

    bool playerInRange;
    bool pulled;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }

    void Update()
    {
        if (!playerInRange || pulled) return;

        GameManager.Instance.SetHint("Натисніть E, щоб потягнути важіль");

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!GameManager.Instance.AllGearsCollected)
            {
                GameManager.Instance.Toast("Механізм не запуститься: зберіть усі шестерні!");
                return;
            }
            Pull();
        }
    }

    void Pull()
    {
        pulled = true;
        AudioManager.Lever();
        if (pivot != null) EffectsManager.Gear(pivot.position);
        if (pivot != null) pivot.localRotation = Quaternion.Euler(0f, 0f, -45f);
        if (door != null) door.Open();
    }
}
