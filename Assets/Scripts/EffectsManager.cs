using UnityEngine;

/// <summary>
/// Завдання 2. Візуальні ефекти (Particle System): префаби ефектів створюються та запускаються у потрібних подіях.
/// </summary>
public class EffectsManager : MonoBehaviour
{
    public static EffectsManager Instance { get; private set; }

    public GameObject gearFx;    // іскри при зборі шестерні
    public GameObject dustFx;    // пил при стрибку / приземленні
    public GameObject steamFx;   // пара при відкритті дверей
    public GameObject deathFx;   // «вибух» при смерті

    void Awake()
    {
        Instance = this;
    }

    static void Spawn(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;
        Instantiate(prefab, position, prefab.transform.rotation);
    }

    public static void Gear(Vector3 p) { if (Instance != null) Spawn(Instance.gearFx, p); }
    public static void Dust(Vector3 p) { if (Instance != null) Spawn(Instance.dustFx, p); }
    public static void Steam(Vector3 p) { if (Instance != null) Spawn(Instance.steamFx, p); }
    public static void Death(Vector3 p) { if (Instance != null) Spawn(Instance.deathFx, p); }
}
