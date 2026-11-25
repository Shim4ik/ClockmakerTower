using UnityEngine;

/// <summary>
/// Завдання 1. Передає стан гравця в Animator Controller (параметри Speed, VelocityY, Grounded).
/// Стани: Idle, Run, Jump, Fall.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    public PlayerController2D controller;

    Animator anim;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int VelocityYHash = Animator.StringToHash("VelocityY");
    static readonly int GroundedHash = Animator.StringToHash("Grounded");

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (controller == null) controller = GetComponentInParent<PlayerController2D>();
    }

    void Update()
    {
        if (controller == null) return;
        anim.SetFloat(SpeedHash, controller.HorizontalSpeed);
        anim.SetFloat(VelocityYHash, controller.VerticalSpeed);
        anim.SetBool(GroundedHash, controller.IsGrounded);
    }
}
