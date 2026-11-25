using UnityEngine;

/// <summary>
/// Опис рівня (на кореневому об'єкті рівня): назва, вступ, точка появи, межі рівня, особливості для списку завдань.
/// </summary>
public sealed class LevelData : MonoBehaviour
{
    public string levelName = "Рівень";
    [TextArea] public string intro = "";
    public Transform spawn;

    [Header("Межі рівня (світові координати X) та діапазон камери по Y")]
    public float leftX = -10f;
    public float rightX = 30f;
    public float camMinY = -1f;
    public float camMaxY = 2f;

    [Header("Особливості (для списку завдань)")]
    public int gateCount;
    public string gateTask = "Відкрити браму";
    public bool hasNpc;
    public bool hasBoss;
}
