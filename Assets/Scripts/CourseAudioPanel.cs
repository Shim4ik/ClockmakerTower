using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public sealed class CourseAudioPanel : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button testButton;

    private bool connected;

    private void Start()
    {
        if (mixer == null || musicSource == null || sfxSource == null ||
            musicClip == null || clickClip == null || musicSlider == null ||
            sfxSlider == null || testButton == null)
        {
            Debug.LogError("Assign every CourseAudioPanel reference.", this);
            enabled = false;
            return;
        }

        musicSlider.onValueChanged.AddListener(SetMusic);
        sfxSlider.onValueChanged.AddListener(SetSfx);
        testButton.onClick.AddListener(PlayClick);
        connected = true;

        SetMusic(musicSlider.value);
        SetSfx(sfxSlider.value);

        musicSource.clip = musicClip;
        musicSource.loop = true;
        musicSource.Play();
    }

    private void SetMusic(float value) => SetLevel("MusicVolume", value);
    private void SetSfx(float value) => SetLevel("SfxVolume", value);

    private void SetLevel(string parameter, float value)
    {
        float linear = Mathf.Clamp01(value);
        float decibels = 20f * Mathf.Log10(Mathf.Max(linear, 0.0001f));
        if (!mixer.SetFloat(parameter, decibels))
            Debug.LogError("Expose mixer parameter: " + parameter, this);
    }

    private void PlayClick()
    {
        if (sfxSource.isActiveAndEnabled)
            sfxSource.PlayOneShot(clickClip);
    }

    private void OnDestroy()
    {
        if (!connected) return;
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(SetMusic);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(SetSfx);
        if (testButton != null)
            testButton.onClick.RemoveListener(PlayClick);
    }
}
