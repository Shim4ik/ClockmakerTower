using UnityEngine;

/// <summary>
/// Рухома платформа: їздить між початковою точкою та точкою зі зсувом offset, зупиняючись на кінцях.
/// Переносить гравця (та ящики), що стоять на ній.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public sealed class MovingPlatform : MonoBehaviour
{
    public Vector2 offset = new Vector2(3f, 0f);
    public float speed = 2f;
    public float pause = 0.4f;

    private Rigidbody2D rb;
    private Vector2 pointA;
    private Vector2 pointB;
    private Vector2 target;
    private float waitUntil;
    private Vector2 lastDelta;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        pointA = rb.position;
        pointB = pointA + offset;
        target = pointB;
    }

    private void FixedUpdate()
    {
        lastDelta = Vector2.zero;
        if (Time.time < waitUntil) return;

        Vector2 pos = rb.position;
        Vector2 next = Vector2.MoveTowards(pos, target, speed * Time.fixedDeltaTime);
        lastDelta = next - pos;
        rb.MovePosition(next);

        if ((next - target).sqrMagnitude < 0.0001f)
        {
            target = (target == pointB) ? pointA : pointB;
            waitUntil = Time.time + pause;
        }
    }

    // Переносимо тіла, що стоять зверху (нормаль контакту спрямована вниз — від тіла до платформи)
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (lastDelta == Vector2.zero) return;
        Rigidbody2D body = collision.rigidbody;
        if (body == null || body.bodyType != RigidbodyType2D.Dynamic) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y < -0.5f)
            {
                body.position += lastDelta;
                return;
            }
        }
    }
}
