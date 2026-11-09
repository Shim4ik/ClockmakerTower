using UnityEngine;

/// <summary>Повільно обертає об'єкт (декоративні шестерні на фоні).</summary>
public class Spinner : MonoBehaviour
{
    public float degreesPerSecond = 10f;

    void Update()
    {
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}
