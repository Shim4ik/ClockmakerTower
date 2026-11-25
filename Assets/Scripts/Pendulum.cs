using UnityEngine;

/// <summary>
/// Маятник-пастка: обертає об'єкт-вісь за синусоїдою. На вантажі (дочірній об'єкт) стоїть Hazard.
/// Через нього можна пробігти у вільний момент або перестрибнути, коли вантаж внизу.
/// </summary>
public sealed class Pendulum : MonoBehaviour
{
    public float amplitudeDeg = 50f;
    public float period = 3f;
    public float phase = 0f;

    private void Update()
    {
        float angle = amplitudeDeg * Mathf.Sin(2f * Mathf.PI * Time.time / period + phase);
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
