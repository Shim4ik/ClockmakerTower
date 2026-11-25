using TMPro;
using UnityEngine;

/// <summary>
/// HUD рахунку (TextMeshPro). Лише відображає стан: власником кількості шестерень є GameManager.
/// Підписка виконується в OnEnable і знімається в OnDisable, тому вимкнення/увімкнення HUD
/// не дублює підписку. Текст оновлюється лише при зміні значень.
/// </summary>
public sealed class SessionHud : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    private int lastCurrent = -1;
    private int lastTotal = -1;

    private void OnEnable()
    {
        GameManager.GearsChanged += OnGearsChanged;
        if (GameManager.Instance != null)
            OnGearsChanged(GameManager.Instance.Gears, GameManager.Instance.TotalGears);
    }

    private void OnDisable()
    {
        GameManager.GearsChanged -= OnGearsChanged;
    }

    private void OnGearsChanged(int current, int total)
    {
        if (label == null) return;
        if (current == lastCurrent && total == lastTotal) return;   // нічого не змінилось
        lastCurrent = current;
        lastTotal = total;
        label.text = "Шестерні: " + current + " / " + total;
    }
}
