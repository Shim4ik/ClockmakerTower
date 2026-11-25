using UnityEngine;

/// <summary>
/// Відтворюваний сценарій для вимірювань: гравець бігає вперед-назад по лівій ділянці рівня
/// (цикл 3 с: 1,5 с вправо, 1,5 с вліво) і стрибає кожні 1,5 с. На маршруті є шипи, ворог-сторож,
/// звуки, ефекти частинок і HUD. Один і той самий сценарій використовується для версій A та B.
/// </summary>
public sealed class PerfBot : MonoBehaviour
{
    private PlayerController2D controller;
    private float startTime;
    private int lastJumpIndex = -1;

    public void Begin(PlayerController2D target)
    {
        controller = target;
        startTime = Time.time;
        lastJumpIndex = -1;
        controller.scriptedControl = true;
        enabled = true;
    }

    public void End()
    {
        if (controller != null)
        {
            controller.scriptedControl = false;
            controller.scriptedX = 0f;
        }
        enabled = false;
    }

    private void Update()
    {
        if (controller == null) return;

        float t = Time.time - startTime;
        float cycle = t % 3f;
        controller.scriptedX = cycle < 1.5f ? 1f : -1f;

        int jumpIndex = Mathf.FloorToInt((t + 1.1f) / 1.5f);   // стрибки на 0,4 с, 1,9 с, 3,4 с ...
        if (jumpIndex != lastJumpIndex)
        {
            lastJumpIndex = jumpIndex;
            controller.ScriptedJump();
        }
    }
}
