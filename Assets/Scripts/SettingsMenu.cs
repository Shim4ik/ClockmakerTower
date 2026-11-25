using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Панель налаштувань звуку: відкривається з головного меню або клавішею Tab під час гри (Esc / «Назад» — закрити).
/// Поки панель відкрита, гра на паузі (Time.timeScale = 0; аудіо продовжує грати), а клавіатурна навігація UI
/// не конфліктує з керуванням персонажем. Звукова логіка знаходиться в CourseAudioPanel.
/// </summary>
public sealed class SettingsMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button firstSelected;
    [SerializeField] private Button closeButton;
    [SerializeField] private MainMenu mainMenu;

    public bool IsOpen { get { return panel != null && panel.activeSelf; } }

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    private void Update()
    {
        bool mainOpen = mainMenu != null && mainMenu.IsOpen;

        // Tab відкриває/закриває налаштування лише під час гри (не поверх головного меню)
        if (!mainOpen && Input.GetKeyDown(KeyCode.Tab))
            SetOpen(!IsOpen);
        else if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            SetOpen(false);
    }

    private void Close() { SetOpen(false); }

    public void SetOpen(bool open)
    {
        if (panel == null) return;
        panel.SetActive(open);

        EventSystem es = EventSystem.current;
        if (open)
        {
            Time.timeScale = 0f;
            UiState.MenuOpen = true;
            if (es != null) es.SetSelectedGameObject(firstSelected != null ? firstSelected.gameObject : null);
        }
        else if (mainMenu != null && mainMenu.IsOpen)
        {
            mainMenu.ReturnToMenu();   // повертаємось у головне меню (пауза лишається)
        }
        else
        {
            Time.timeScale = 1f;
            UiState.MenuOpen = false;
            if (es != null) es.SetSelectedGameObject(null);
        }
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
        UiState.MenuOpen = false;
    }
}
