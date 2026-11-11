using UnityEngine;

/// <summary>
/// Завдання 3. Звукові ефекти: звуки дій, оточення (фоновий гул вежі з цоканням годинника)
/// та спеціальні звуки (механізми). Клавіша M — вимкнути/увімкнути звук.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Звуки дій")]
    public AudioClip jump;
    public AudioClip land;
    public AudioClip stepStone;
    public AudioClip stepWood;

    [Header("Спеціальні звуки")]
    public AudioClip gear;
    public AudioClip lever;
    public AudioClip door;
    public AudioClip death;
    public AudioClip lamp;

    [Header("Оточення")]
    public AudioClip ambient;
    [Range(0f, 1f)] public float ambientVolume = 0.35f;

    AudioSource sfx;
    AudioSource amb;

    void Awake()
    {
        Instance = this;

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.spatialBlend = 0f;   // 2D-звук

        amb = gameObject.AddComponent<AudioSource>();
        amb.playOnAwake = false;
        amb.loop = true;
        amb.spatialBlend = 0f;
        amb.volume = ambientVolume;
        amb.clip = ambient;
        if (ambient != null) amb.Play();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            AudioListener.volume = AudioListener.volume > 0f ? 0f : 1f;
    }

    static void Play(AudioClip clip, float volume = 1f, float pitchVariation = 0f)
    {
        if (Instance == null || clip == null) return;
        Instance.sfx.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        Instance.sfx.PlayOneShot(clip, volume);
    }

    public static void Jump() { if (Instance != null) Play(Instance.jump, 0.7f, 0.05f); }
    public static void Land() { if (Instance != null) Play(Instance.land, 0.8f, 0.08f); }
    public static void Step(bool wood)
    {
        if (Instance == null) return;
        Play(wood ? Instance.stepWood : Instance.stepStone, 0.5f, 0.12f);
    }
    public static void Gear() { if (Instance != null) Play(Instance.gear, 0.8f); }
    public static void Lever() { if (Instance != null) Play(Instance.lever, 0.9f); }
    public static void Door() { if (Instance != null) Play(Instance.door, 0.9f); }
    public static void Death() { if (Instance != null) Play(Instance.death, 0.9f); }
    public static void Lamp() { if (Instance != null) Play(Instance.lamp, 0.7f); }
}
