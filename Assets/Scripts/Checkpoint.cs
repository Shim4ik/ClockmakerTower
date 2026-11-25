using UnityEngine;

/// <summary>
/// Контрольна точка: при дотику зберігає місце появи гравця після смерті.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class Checkpoint : MonoBehaviour
{
    public SpriteRenderer flag;
    private bool reached;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (reached || !other.CompareTag("Player")) return;
        reached = true;

        if (flag != null) flag.color = new Color(0.35f, 0.9f, 0.4f);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetSpawn(new Vector3(transform.position.x, -2.9f, 0f));
            GameManager.Instance.Toast("Контрольну точку збережено");
        }
        AudioManager.Lamp();
        EffectsManager.Gear(transform.position);
    }
}
