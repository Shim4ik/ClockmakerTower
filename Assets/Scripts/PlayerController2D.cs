using UnityEngine;

/// <summary>
/// Рух гравця: біг ліворуч/праворуч та стрибок (2D, Rigidbody2D).
/// Лабораторна №12: додано звуки (стрибок, приземлення, кроки залежно від поверхні) та ефект пилу.
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

    [Header("Звук кроків")]
    public float stepInterval = 0.3f;

    [Header("Візуал")]
    public Transform eye;

    [HideInInspector] public bool canControl = true;

    public bool IsGrounded { get { return Time.time - lastGroundedTime <= 0.1f; } }
    public float HorizontalSpeed { get { return Mathf.Abs(rb.GetVelocity().x); } }
    public float VerticalSpeed { get { return rb.GetVelocity().y; } }
    public Vector3 FeetPosition { get { return transform.position + Vector3.down * 0.55f; } }

    Rigidbody2D rb;
    float inputX;
    float lastGroundedTime = -10f;
    float lastJumpPressedTime = -10f;
    float facing = 1f;
    float lastStepTime;
    float airVelocityY;
    bool wasGrounded;
    Collider2D groundCollider;

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

        // Приземлення: звук + пил
        bool grounded = IsGrounded;
        if (!grounded) airVelocityY = rb.GetVelocity().y;
        if (grounded && !wasGrounded && airVelocityY < -5f)
        {
            AudioManager.Land();
            EffectsManager.Dust(FeetPosition);
            airVelocityY = 0f;
        }
        wasGrounded = grounded;

        // Кроки: звук залежить від поверхні (камінь / дерево)
        if (grounded && Mathf.Abs(inputX) > 0.1f && HorizontalSpeed > 1f && Time.time - lastStepTime >= stepInterval)
        {
            lastStepTime = Time.time;
            AudioManager.Step(OnWood());
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
            AudioManager.Jump();
            EffectsManager.Dust(FeetPosition);
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
                groundCollider = col.collider;
                return;
            }
        }
    }

    bool OnWood()
    {
        return groundCollider != null &&
               (groundCollider.name.StartsWith("Platform") || groundCollider.name.StartsWith("Crate"));
    }

    public void Respawn(Vector3 position)
    {
        transform.position = position;
        rb.SetVelocity(Vector2.zero);
        airVelocityY = 0f;
    }

    public void StopMoving()
    {
        canControl = false;
        inputX = 0f;
        rb.SetVelocity(Vector2.zero);
    }
}
