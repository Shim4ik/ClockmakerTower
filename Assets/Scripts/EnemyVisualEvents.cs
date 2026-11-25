using UnityEngine;

/// <summary>
/// Приймає Animation Events з кліпу атаки (вони викликаються на об'єкті з Animator)
/// і передає їх у EnemyAI.
/// </summary>
public class EnemyVisualEvents : MonoBehaviour
{
    public EnemyAI ai;

    // Викликається подією в кліпі Enemy_Attack у момент удару
    public void OnAttackHit()
    {
        if (ai != null) ai.OnAttackHit();
    }
}
