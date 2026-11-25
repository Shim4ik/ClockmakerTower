using UnityEngine;

/// <summary>
/// Спільний стан інтерфейсу: коли відкрите будь-яке меню, решта підписів (IMGUI) ховається,
/// щоб елементи інтерфейсу не накладались один на одного.
/// </summary>
public static class UiState
{
    public static bool MenuOpen;
    public static bool MenuShown;   // стартове меню вже показувалось у цьому запуску

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        MenuOpen = false;
        MenuShown = false;
    }
}
