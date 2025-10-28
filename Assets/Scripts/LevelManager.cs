using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Progress Bar")]
    [SerializeField] private LevelProgressBar progressBar;   // Drag your Slider (with LevelProgressBar) here
    [SerializeField] private int totalLevels = 10;

    public int CurrentLevel { get; private set; } = 1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        // If you forgot to assign in Inspector, try to find it:
#if UNITY_2022_2_OR_NEWER
        if (progressBar == null)
            progressBar = FindFirstObjectByType<LevelProgressBar>(FindObjectsInactive.Include);
#else
        if (progressBar == null)
            progressBar = FindObjectOfType<LevelProgressBar>();
#endif

        // Initialize UI to level 1
        UpdateUI();
        SetupLevel(CurrentLevel);
    }

    /// <summary>
    /// Load a specific level (1..totalLevels).
    /// </summary>
    public void LoadLevel(int levelIndex)
    {
        CurrentLevel = Mathf.Clamp(levelIndex, 1, totalLevels);
        Debug.Log("Loading level: " + CurrentLevel);

        SetupLevel(CurrentLevel);
        UpdateUI();
    }

    /// <summary>
    /// Advance to the next level.
    /// </summary>
    public void NextLevel()
    {
        if (CurrentLevel < totalLevels)
        {
            CurrentLevel++;
            Debug.Log("Next level -> " + CurrentLevel);
            SetupLevel(CurrentLevel);
            UpdateUI();
        }
        else
        {
            Debug.Log("All levels complete!");
            // TODO: show win screen or loop/reset as needed
        }
    }

    /// <summary>
    /// Restart the current level.
    /// </summary>
    public void RestartLevel()
    {
        Debug.Log("Restart level -> " + CurrentLevel);
        SetupLevel(CurrentLevel);
        UpdateUI();
    }

    // --- Helpers ---

    private void UpdateUI()
    {
        if (progressBar != null)
            progressBar.SetLevel(CurrentLevel);
    }

    /// <summary>
    /// Put your per-level setup here (spawn requests, set goals, etc).
    /// </summary>
    private void SetupLevel(int level)
    {
        // TODO: your existing logic that configures the request for this level
        // e.g., RequestManager.Instance.BuildRequestFor(level);
    }
}
