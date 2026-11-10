using UnityEngine;

/// <summary>
/// Паралакс: шар рухається разом із камерою з коефіцієнтом factor
/// (1 — «приклеєний» до камери, 0 — стоїть у світі). Створює відчуття глибини.
/// </summary>
public class ParallaxLayer : MonoBehaviour
{
    [Range(0f, 1f)] public float factor = 0.5f;

    Transform cam;
    Vector3 startPos;
    Vector3 camStart;

    void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
        startPos = transform.position;
        if (cam != null) camStart = cam.position;
    }

    void LateUpdate()
    {
        if (cam == null) return;
        Vector3 d = cam.position - camStart;
        transform.position = new Vector3(startPos.x + d.x * factor, startPos.y + d.y * factor, startPos.z);
    }
}
