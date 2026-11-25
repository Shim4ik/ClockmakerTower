using System;
using UnityEngine;

public enum EnemyState { Idle = 0, Patrol = 1, Chase = 2, Attack = 3, Retreat = 4 }

/// <summary>
/// Штучний інтелект ворога на основі скінченного автомата (FSM).
/// Стани: Idle (спокій), Patrol (патрулювання), Chase (переслідування), Attack (атака), Retreat (втеча).
/// Гравця виявляє за допомогою Raycast (з перевіркою видимості крізь перешкоди).
/// Атака завдає шкоди у момент Animation Event «OnAttackHit».
/// Якщо гравець стрибне ворогові на голову — ворог тікає; для боса потрібно кілька таких стрибків.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    [Header("Патрулювання")]
    public float leftX = 0f;
    public float rightX = 5f;
    public float patrolSpeed = 2f;
    public float idleTime = 1f;
    [Tooltip("На скільки ворог може вийти за межі патрульної зони під час переслідування")]
    public float boundsMargin = 1f;

    [Header("Виявлення гравця")]
    public float detectionRange = 6f;
    [Tooltip("У цьому радіусі ворог помічає гравця й позаду себе")]
    public float closeRadius = 2f;
    public float loseSightTime = 2f;

    [Header("Переслідування")]
    public float chaseSpeed = 3.5f;

    [Header("Атака")]
    public float attackRange = 1.3f;
    public float attackDuration = 0.7f;
    public float attackCooldown = 1.2f;
    public int damage = 1;
    public Vector2 knockback = new Vector2(8f, 6f);
    [Tooltip("Резервний момент удару, якщо Animation Event не спрацював")]
    public float hitFallbackTime = 0.6f;

    [Header("Втеча")]
    public float retreatSpeed = 5f;
    public float retreatTime = 2.5f;

    [Header("Посилання")]
    public Animator animator;
    public SpriteRenderer sprite;
    public Transform eye;

    [Header("Бос / особливі вороги")]
    [Tooltip("Скільки стрибків на голову потрібно, щоб перемогти ворога (0 — ворога не можна перемогти, лише оглушити)")]
    public int stompsToDefeat = 0;
    [Tooltip("Рівень можна пройти, лише перемігши цього ворога")]
    public bool mustDefeat = false;
    public float stompHeight = 0.7f;
    public Color patrolColor = new Color(0.85f, 0.30f, 0.25f);

    /// <summary>Кількість живих ворогів, яких обов'язково треба перемогти.</summary>
    public static int MustDefeatAlive { get; private set; }

    public EnemyState State { get; private set; }

    Rigidbody2D rb;
    Transform player;
    PlayerHealth playerHealth;
    float stateTimer;
    float moveX;
    int dir = 1;
    float lastSeenTime;
    float nextAttackTime;
    bool hitDone;
    bool counted;
    int stompsTaken;
    GUIStyle labelStyle;

#if CLOCKMAKER_OPT_AI
    private static readonly RaycastHit2D[] HitBuffer = new RaycastHit2D[8];
    private static readonly ContactFilter2D HitFilter = MakeFilter();

    private static ContactFilter2D MakeFilter()
    {
        ContactFilter2D f = new ContactFilter2D();
        f.NoFilter();
        f.useTriggers = false;   // тригери не перекривають огляд
        return f;
    }
#endif

    static readonly Color ColChase = new Color(1f, 0.55f, 0.15f);
    static readonly Color ColAttack = new Color(1f, 0.95f, 0.35f);
    static readonly Color ColRetreat = new Color(0.55f, 0.65f, 0.90f);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        if (mustDefeat && !counted) { counted = true; MustDefeatAlive++; }
    }

    void OnDisable()
    {
        if (counted) { counted = false; MustDefeatAlive--; }
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerHealth = p.GetComponent<PlayerHealth>();
        }
        SetState(EnemyState.Patrol);
    }

    // ---------------- FSM ----------------

    void SetState(EnemyState s)
    {
        EnemyState prev = State;
        State = s;
        stateTimer = 0f;

        if (s == EnemyState.Attack)
        {
            hitDone = false;
            moveX = 0f;
        }
        if (s == EnemyState.Chase)
        {
            lastSeenTime = Time.time;
            if (prev == EnemyState.Idle || prev == EnemyState.Patrol)
                AudioManager.Alert();
        }
        if (animator != null) animator.SetInteger("State", (int)s);
    }

    void Update()
    {
        if (player == null) return;

        stateTimer += Time.deltaTime;
        bool sees = CanSeePlayer();
        float dx = player.position.x - transform.position.x;
        float dy = Mathf.Abs(player.position.y - transform.position.y);
        float x = transform.position.x;

        switch (State)
        {
            case EnemyState.Idle:
                moveX = 0f;
                if (sees) SetState(EnemyState.Chase);
                else if (stateTimer >= idleTime) SetState(EnemyState.Patrol);
                break;

            case EnemyState.Patrol:
                moveX = patrolSpeed * dir;
                if (x >= rightX && dir > 0) { dir = -1; SetState(EnemyState.Idle); }
                else if (x <= leftX && dir < 0) { dir = 1; SetState(EnemyState.Idle); }
                if (sees) SetState(EnemyState.Chase);
                break;

            case EnemyState.Chase:
                dir = dx >= 0f ? 1 : -1;
                moveX = chaseSpeed * dir;
                if (sees) lastSeenTime = Time.time;

                if (Mathf.Abs(dx) <= attackRange && dy < 1.2f && Time.time >= nextAttackTime)
                    SetState(EnemyState.Attack);
                else if (Time.time - lastSeenTime > loseSightTime)
                {
                    dir = x < (leftX + rightX) * 0.5f ? 1 : -1;   // повертаємось до патрульної зони
                    SetState(EnemyState.Patrol);
                }
                break;

            case EnemyState.Attack:
                moveX = 0f;
                dir = dx >= 0f ? 1 : -1;
                if (!hitDone && stateTimer >= hitFallbackTime) OnAttackHit();
                if (stateTimer >= attackDuration)
                {
                    nextAttackTime = Time.time + attackCooldown;
                    SetState(sees ? EnemyState.Chase : EnemyState.Patrol);
                }
                break;

            case EnemyState.Retreat:
                dir = dx >= 0f ? -1 : 1;
                moveX = retreatSpeed * dir;
                if (stateTimer >= retreatTime)
                {
                    dir = x < (leftX + rightX) * 0.5f ? 1 : -1;
                    SetState(EnemyState.Patrol);
                }
                break;
        }

        UpdateVisuals();
    }

    void FixedUpdate()
    {
        float vx = moveX;
        float x = rb.position.x;
        float min = leftX - boundsMargin;
        float max = rightX + boundsMargin;
        // Не виходимо за межі зони (щоб не впасти з платформи)
        if ((x <= min && vx < 0f) || (x >= max && vx > 0f)) vx = 0f;
        rb.SetVelocity(new Vector2(vx, rb.GetVelocity().y));
    }

    void UpdateVisuals()
    {
        if (eye != null)
            eye.localPosition = new Vector3(0.25f * dir, eye.localPosition.y, eye.localPosition.z);

        if (sprite == null) return;
        switch (State)
        {
            case EnemyState.Chase: sprite.color = ColChase; break;
            case EnemyState.Attack:
                // миготіння під час замаху — попередження для гравця
                sprite.color = ((int)(Time.time * 14f) % 2 == 0) ? ColAttack : ColChase;
                break;
            case EnemyState.Retreat: sprite.color = ColRetreat; break;
            default: sprite.color = patrolColor; break;
        }
    }

    // ---------------- виявлення (Raycast) ----------------

    bool CanSeePlayer()
    {
        Vector2 origin = rb.position + Vector2.up * 0.3f;
        Vector2 to = (Vector2)player.position - origin;
        float dist = to.magnitude;
        if (dist > detectionRange) return false;

        // Ворог бачить уперед; позаду — лише на малій відстані
        bool inFront = Mathf.Sign(to.x) == Mathf.Sign(dir);
        if (!inFront && dist > closeRadius) return false;

#if CLOCKMAKER_OPT_AI
        // Версія B: без виділень пам'яті — спільний буфер і ручний пошук найближчого колайдера
        int count = Physics2D.Raycast(origin, to.normalized, HitFilter, HitBuffer, dist);
        float best = float.MaxValue;
        Collider2D nearest = null;
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D h = HitBuffer[i];
            if (h.collider == null || h.collider.attachedRigidbody == rb) continue;   // сам ворог
            if (h.distance < best) { best = h.distance; nearest = h.collider; }
        }
        return nearest != null && nearest.CompareTag("Player");
#else
        // Версія A: RaycastAll створює новий масив щокадру, плюс сортування
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, to.normalized, dist);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit2D h in hits)
        {
            if (h.collider == null || h.collider.isTrigger) continue;
            if (h.collider.attachedRigidbody == rb) continue;   // сам ворог
            return h.collider.CompareTag("Player");             // перше, що перекриває промінь
        }
        return false;
#endif
    }

    // ---------------- атака ----------------

    /// <summary>Викликається Animation Event у момент удару (або резервним таймером).</summary>
    public void OnAttackHit()
    {
        if (State != EnemyState.Attack || hitDone) return;
        hitDone = true;
        if (player == null || playerHealth == null) return;

        float dx = player.position.x - transform.position.x;
        float dy = Mathf.Abs(player.position.y - transform.position.y);
        if (Mathf.Abs(dx) <= attackRange * 1.3f && dy < 1.2f)
        {
            Vector2 kb = new Vector2(Mathf.Sign(dx) * knockback.x, knockback.y);
            if (playerHealth.Damage(damage, kb))
            {
                AudioManager.Hit();
                EffectsManager.Hit(player.position);
            }
        }
    }

    // ---------------- стрибок на голову ----------------

    void OnCollisionEnter2D(Collision2D c) { TryStomp(c); }
    void OnCollisionStay2D(Collision2D c) { TryStomp(c); }

    void TryStomp(Collision2D c)
    {
        if (State == EnemyState.Retreat) return;
        PlayerController2D pc = c.collider.GetComponent<PlayerController2D>();
        if (pc == null) return;

        float dy = pc.transform.position.y - transform.position.y;
        if (dy > stompHeight && pc.VerticalSpeed <= 0.5f)
        {
            pc.Bounce(11f);
            AudioManager.Stomp();
            EffectsManager.Dust(transform.position + Vector3.up * 0.5f);

            if (stompsToDefeat > 0)
            {
                stompsTaken++;
                if (stompsTaken >= stompsToDefeat)
                {
                    Defeat();
                    return;
                }
                if (GameManager.Instance != null)
                    GameManager.Instance.Toast("Влучно! Лишилось стрибків на голову: " + (stompsToDefeat - stompsTaken));
            }
            SetState(EnemyState.Retreat);
        }
    }

    void Defeat()
    {
        EffectsManager.Death(transform.position);
        AudioManager.Death();
        if (GameManager.Instance != null) GameManager.Instance.Toast("Сторожа переможено!", 3f);
        gameObject.SetActive(false);   // OnDisable зменшить лічильник MustDefeatAlive
    }

    // ---------------- допоміжне ----------------

    string StateLabel()
    {
        switch (State)
        {
            case EnemyState.Idle: return "Спокій";
            case EnemyState.Patrol: return "Патрулювання";
            case EnemyState.Chase: return "Переслідування!";
            case EnemyState.Attack: return "Атака!";
            default: return "Втеча";
        }
    }

    void OnGUI()
    {
        if (UiState.MenuOpen || Camera.main == null) return;
        Vector3 sp = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.0f);
        if (sp.z < 0f) return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            labelStyle.normal.textColor = Color.white;
        }
        GUI.Label(new Rect(sp.x - 90f, Screen.height - sp.y - 12f, 180f, 24f), StateLabel(), labelStyle);
    }

    void OnDrawGizmosSelected()
    {
        // Патрульна зона та радіус виявлення (видно у вікні Scene)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(leftX, transform.position.y - 0.6f, 0f), new Vector3(rightX, transform.position.y - 0.6f, 0f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
