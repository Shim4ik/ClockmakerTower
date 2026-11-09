using UnityEngine;

/// <summary>
/// Завдання 4. Камера: плавно слідує за гравцем у межах рівня
/// та тимчасово фокусується на важливих подіях (відкриття дверей).
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    public Transform target;
    public Vector3 offset = new Vector3(0f, 1.2f, -10f);
    public float smoothness = 6f;

    [Header("Межі камери (центр)")]
    public float minX = 0.7f;
    public float maxX = 19.3f;
    public float minY = -1f;
    public float maxY = 2f;

    Transform focusTarget;
    float focusUntil;

    void Awake()
    {
        Instance = this;
    }

    public void FocusOn(Transform t, float duration)
    {
        focusTarget = t;
        focusUntil = Time.time + duration;
    }

    void LateUpdate()
    {
        Transform t = (focusTarget != null && Time.time < focusUntil) ? focusTarget : target;
        if (t == null) return;

        Vector3 desired = new Vector3(
            Mathf.Clamp(t.position.x + offset.x, minX, maxX),
            Mathf.Clamp(t.position.y + offset.y, minY, maxY),
            offset.z);

        float k = 1f - Mathf.Exp(-smoothness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, k);
    }
}
