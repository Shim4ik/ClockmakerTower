using UnityEngine;

/// <summary>
/// Завдання 1. Рух гравця: біг ліворуч/праворуч та стрибок (2D, Rigidbody2D).
/// Керування: A/D або стрілки — рух, Пробіл — стрибок.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Рух")]
    public float moveSpeed = 7f;

    [Header("Стрибок")]
    public float jumpVelocity = 13f;
    [Tooltip("Час після сходження з краю платформи, коли стрибок ще дозволений")]
    public float coyoteTime = 0.1f;
    [Tooltip("Час, протягом якого натиснутий стрибок «запам'ятовується» перед приземленням")]
    public float jumpBufferTime = 0.12f;

    [Header("Візуал")]
    public Transform eye;

    [HideInInspector] public bool canControl = true;

    Rigidbody2D rb;
    float inputX;
    float lastGroundedTime = -10f;
    float lastJumpPressedTime = -10f;
    float facing = 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        inputX = canControl ? Input.GetAxisRaw("Horizontal") : 0f;

        if (canControl && Input.GetButtonDown("Jump"))
            lastJumpPressedTime = Time.time;

        // Змінна висота стрибка: відпустили пробіл — зменшуємо швидкість підйому
        if (Input.GetButtonUp("Jump") && rb.GetVelocity().y > 0f)
            rb.SetVelocity(new Vector2(rb.GetVelocity().x, rb.GetVelocity().y * 0.5f));

        if (eye != null)
        {
            if (inputX > 0.01f) facing = 1f;
            else if (inputX < -0.01f) facing = -1f;
            eye.localPosition = new Vector3(0.25f * facing, eye.localPosition.y, eye.localPosition.z);
        }
    }

    void FixedUpdate()
    {
        rb.SetVelocity(new Vector2(inputX * moveSpeed, rb.GetVelocity().y));

        bool canJump = Time.time - lastGroundedTime <= coyoteTime;
        bool wantsJump = Time.time - lastJumpPressedTime <= jumpBufferTime;
        if (canJump && wantsJump)
        {
            rb.SetVelocity(new Vector2(rb.GetVelocity().x, jumpVelocity));
            lastGroundedTime = -10f;
            lastJumpPressedTime = -10f;
        }
    }

    void OnCollisionEnter2D(Collision2D col) { CheckGround(col); }
    void OnCollisionStay2D(Collision2D col) { CheckGround(col); }

    // Гравець «стоїть на землі», якщо нормаль контакту спрямована вгору
    void CheckGround(Collision2D col)
    {
        for (int i = 0; i < col.contactCount; i++)
        {
            if (col.GetContact(i).normal.y > 0.7f)
            {
                lastGroundedTime = Time.time;
                return;
            }
        }
    }

    public void Respawn(Vector3 position)
    {
        transform.position = position;
        rb.SetVelocity(Vector2.zero);
    }

    public void StopMoving()
    {
        canControl = false;
        inputX = 0f;
        rb.SetVelocity(Vector2.zero);
    }
}
