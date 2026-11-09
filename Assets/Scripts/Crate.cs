using UnityEngine;

/// <summary>
/// Важкий ящик, який можна штовхати. Якщо ящик упав у прірву — повертається на початкове місце.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Crate : MonoBehaviour
{
    public float killY = -8f;

    Rigidbody2D rb;
    Vector3 startPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;
    }

    void Update()
    {
        if (transform.position.y < killY)
        {
            transform.position = startPos;
            rb.SetVelocity(Vector2.zero);
        }
    }
}
