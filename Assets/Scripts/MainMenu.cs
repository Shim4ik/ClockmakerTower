using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Стартове меню: «Нова гра», «Продовжити», «Налаштування звуку», «Вийти». Поки меню відкрите, гра на паузі.
/// Показується один раз за запуск. Обробники кнопок підписуються в Start і знімаються в OnDestroy.
/// </summary>
public sealed class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private SettingsMenu settings;

    public bool IsOpen { get { return menuPanel != null && menuPanel.activeSelf; } }

    private void Start()
    {
        playButton.onClick.AddListener(Play);
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(Continue);
            continueButton.gameObject.SetActive(LevelManager.SavedLevel > 0);   // є збережений прогрес
        }
        settingsButton.onClick.AddListener(OpenSettings);
        quitButton.onClick.AddListener(Quit);

        if (UiState.MenuShown)
        {
            Hide();
        }
        else
        {
            UiState.MenuShown = true;
            Show();
        }
    }

    private void Show()
    {
        menuPanel.SetActive(true);
        Time.timeScale = 0f;
        UiState.MenuOpen = true;
        Select(playButton);
    }

    private void Hide()
    {
        menuPanel.SetActive(false);
        Time.timeScale = 1f;
        UiState.MenuOpen = false;
        Select(null);
    }

    /// <summary>Повернення з налаштувань у головне меню.</summary>
    public void ReturnToMenu()
    {
        menuPanel.SetActive(true);
        Time.timeScale = 0f;
        UiState.MenuOpen = true;
        Select(playButton);
    }

    /// <summary>Запуск нової гри без участі гравця (автоматичний дослід, -autobench).</summary>
    public void AutoStart()
    {
        Play();
    }

    private void Play()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.StartNewGame();
        Hide();
    }

    private void Continue()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.ContinueGame();
        Hide();
    }

    private void OpenSettings()
    {
        if (settings != null) settings.SetOpen(true);
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void Select(Button button)
    {
        EventSystem es = EventSystem.current;
        if (es != null) es.SetSelectedGameObject(button != null ? button.gameObject : null);
    }

    private void OnDestroy()
    {
        if (playButton != null) playButton.onClick.RemoveListener(Play);
        if (continueButton != null) continueButton.onClick.RemoveListener(Continue);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
        if (quitButton != null) quitButton.onClick.RemoveListener(Quit);
    }
}
