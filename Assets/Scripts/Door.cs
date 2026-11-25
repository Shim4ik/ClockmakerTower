using UnityEngine;

/// <summary>
/// Двері / брама, що плавно відкриваються (зсуваються вгору). Рухається кінематичний Rigidbody2D.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Door : MonoBehaviour
{
    public Vector2 openOffset = new Vector2(0f, 6f);
    public float openSpeed = 4f;
    [Tooltip("Чи є ці двері виходом з рівня")]
    public bool isExit = false;

    public bool IsOpen { get; private set; }

    Rigidbody2D rb;
    Vector2 closedPos;
    Vector2 openPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        closedPos = rb.position;
        openPos = closedPos + openOffset;
    }

    public void Open(bool focusCamera = true)
    {
        if (IsOpen) return;
        IsOpen = true;

        Collider2D col = GetComponent<Collider2D>();
        Vector3 basePos = col != null ? new Vector3(transform.position.x, col.bounds.min.y, 0f) : transform.position;
        EffectsManager.Steam(basePos);
        AudioManager.Door();

        if (focusCamera && CameraFollow.Instance != null)
            CameraFollow.Instance.FocusOn(transform, 1.6f);
        if (GameManager.Instance != null)
            GameManager.Instance.OnDoorOpened(isExit);
    }

    public void Close()
    {
        IsOpen = false;
    }

    void FixedUpdate()
    {
        Vector2 target = IsOpen ? openPos : closedPos;
        rb.MovePosition(Vector2.MoveTowards(rb.position, target, openSpeed * Time.fixedDeltaTime));
    }
}
