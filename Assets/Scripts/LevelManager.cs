using UnityEngine;

/// <summary>
/// Керує рівнями гри: створює свіжу копію шаблону рівня, налаштовує камеру, веде прогрес (збереження в PlayerPrefs).
/// Шаблони рівнів — вимкнені об'єкти сцени; активний рівень — їх копія, тому перезапуск повертає рівень у початковий стан.
/// </summary>
public sealed class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    private const string SaveKey = "Clockmaker_NextLevel";

    [SerializeField] private GameObject[] levelTemplates;

    private GameObject current;
    private int currentIndex = -1;

    public int CurrentIndex { get { return currentIndex; } }
    public int LevelCount { get { return levelTemplates != null ? levelTemplates.Length : 0; } }
    public bool IsLastLevel { get { return currentIndex >= LevelCount - 1; } }

    /// <summary>Індекс рівня, з якого можна продовжити гру (0 — збереження немає).</summary>
    public static int SavedLevel { get { return PlayerPrefs.GetInt(SaveKey, 0); } }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        LoadLevel(0);   // фон за стартовим меню
    }

    public void StartNewGame()
    {
        if (GameManager.Instance != null) GameManager.Instance.ResetRun();
        LoadLevel(0);
    }

    public void ContinueGame()
    {
        if (GameManager.Instance != null) GameManager.Instance.ResetRun();
        LoadLevel(Mathf.Clamp(SavedLevel, 0, Mathf.Max(0, LevelCount - 1)));
    }

    public void RestartLevel()
    {
        LoadLevel(currentIndex < 0 ? 0 : currentIndex);
    }

    public void NextLevel()
    {
        LoadLevel(Mathf.Min(currentIndex + 1, LevelCount - 1));
    }

    public void SaveProgress(int nextIndex)
    {
        PlayerPrefs.SetInt(SaveKey, nextIndex);
        PlayerPrefs.Save();
    }

    public void ClearProgress()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }

    private void LoadLevel(int index)
    {
        if (levelTemplates == null || levelTemplates.Length == 0) return;
        index = Mathf.Clamp(index, 0, levelTemplates.Length - 1);

        if (current != null)
        {
            current.SetActive(false);   // одразу знімає шестерні зі списків (OnDisable)
            Destroy(current);
            current = null;
        }

        current = Instantiate(levelTemplates[index]);
        current.name = "Level_" + (index + 1);
        current.SetActive(true);
        currentIndex = index;

        LevelData data = current.GetComponent<LevelData>();
        ApplyCameraBounds(data);
        if (GameManager.Instance != null) GameManager.Instance.BeginLevel(data, index);
    }

    private void ApplyCameraBounds(LevelData d)
    {
        CameraFollow follow = CameraFollow.Instance;
        if (follow == null || d == null) return;

        Camera cam = follow.GetComponent<Camera>();
        float halfWidth = cam.orthographicSize * cam.aspect;
        float minX = d.leftX + halfWidth;
        float maxX = Mathf.Max(minX, d.rightX - halfWidth);
        follow.SetBounds(minX, maxX, d.camMinY, d.camMaxY);
    }
}
