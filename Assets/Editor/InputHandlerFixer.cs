using UnityEditor;
using UnityEngine;

/// <summary>
/// У Unity 6 нові проєкти можуть мати вимкнений старий Input Manager (Input.GetAxis тощо).
/// Цей скрипт один раз вмикає режим «Both», щоб прототип працював.
/// </summary>
public static class InputHandlerFixer
{
    [InitializeOnLoadMethod]
    static void Fix()
    {
        EditorApplication.delayCall += () =>
        {
            try
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
                if (assets == null || assets.Length == 0) return;
                var so = new SerializedObject(assets[0]);
                var prop = so.FindProperty("activeInputHandler");
                if (prop == null) return;
                bool inputSystemInstalled = System.Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem") != null;
                // 1 = лише нова Input System; 0 = лише старий Input Manager (потрібен «Both», якщо пакет Input System встановлено)
                if (prop.intValue == 1 || (prop.intValue == 0 && inputSystemInstalled))
                {
                    prop.intValue = 2;   // 2 = Both
                    so.ApplyModifiedPropertiesWithoutUndo();
                    AssetDatabase.SaveAssets();
                    EditorUtility.DisplayDialog("Clockmaker",
                        "Увімкнено режим Input «Both». Перезапустіть редактор Unity, щоб зміна подіяла.", "OK");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Не вдалося перевірити Active Input Handling: " + e.Message);
            }
        };
    }
}
