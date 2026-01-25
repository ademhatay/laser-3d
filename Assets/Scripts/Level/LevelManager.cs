using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    public delegate void LevelDataChangedHandler();
    public event LevelDataChangedHandler OnLevelDataUpdated;
    
    [Header("Current Level Data")]
    [SerializeField] private LevelData currentLevelData;
    
    [Header("Level Information")]
    [SerializeField] private string levelName = "Level 1";
    [SerializeField] private int levelNumber = 1;
    
    [Header("Completion Requirements")]
    [SerializeField] private LaserColorType[] requiredColors = new LaserColorType[0];
    [SerializeField] private bool requireAllTargets = false;
    [SerializeField] private bool requireAllCollectables = false;
    
    [Header("References")]
    [SerializeField] private InteractableDoor exitDoor;
    
    private List<LaserTarget> allTargets = new List<LaserTarget>();
    private Dictionary<LaserColorType, List<LaserTarget>> targetsByColor = new Dictionary<LaserColorType, List<LaserTarget>>();
    
    private bool levelCompleteTriggered = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Load level data from LevelProgressManager if available
        if (currentLevelData == null)
        {
            currentLevelData = LevelProgressManager.Instance?.GetCurrentLevelData();
        }
        
        // Apply level data settings
        if (currentLevelData != null)
        {
            levelName = currentLevelData.levelName;
            levelNumber = currentLevelData.levelNumber;
            requiredColors = currentLevelData.requiredColors;
            requireAllCollectables = currentLevelData.requireAllCollectables;
        }
        
        FindAllTargets();
        
        LaserTarget.OnTargetActivated += CheckLevelCompletion;
        LaserTarget.OnTargetDeactivated += CheckLevelCompletion;
        
        if (exitDoor == null)
        {
            exitDoor = FindFirstObjectByType<InteractableDoor>();
        }
        
        // Notify UI of level data
        OnLevelDataUpdated?.Invoke();
        
        Debug.Log($"[LevelManager] Level başlatıldı: {levelName} (#{levelNumber})");
        CheckLevelCompletion();
    }
    
    void OnDestroy()
    {
        LaserTarget.OnTargetActivated -= CheckLevelCompletion;
        LaserTarget.OnTargetDeactivated -= CheckLevelCompletion;
    }
    
    private void FindAllTargets()
    {
        allTargets.Clear();
        targetsByColor.Clear();
        
        LaserTarget[] targets = FindObjectsByType<LaserTarget>(FindObjectsSortMode.None);
        allTargets.AddRange(targets);
        
        foreach (LaserTarget target in allTargets)
        {
            LaserColorType color = target.RequiredColorType;
            
            if (!targetsByColor.ContainsKey(color))
            {
                targetsByColor[color] = new List<LaserTarget>();
            }
            
            targetsByColor[color].Add(target);
        }
        
        Debug.Log($"[LevelManager] {levelName} - {allTargets.Count} hedef bulundu");
    }
    
    private void CheckLevelCompletion()
    {
        bool levelComplete = CheckRequirements();
        
        if (levelComplete && !levelCompleteTriggered)
        {
            levelCompleteTriggered = true;
            UnlockAndOpenDoor();
            OnLevelCompleted();
        }
        else if (!levelComplete && levelCompleteTriggered)
        {
            levelCompleteTriggered = false;
            LockDoor();
        }
        
        // UI'yi güncelle
        OnLevelDataUpdated?.Invoke();
    }
    
    private bool CheckRequirements()
    {
        // Toplanabilir kontrolü
        if (requireAllCollectables && GameManager.Instance != null)
        {
            if (GameManager.Instance.CollectablesCollected < GameManager.Instance.TotalCollectables)
            {
                return false;
            }
        }
        
        // Renk gereksinimleri yoksa
        if (requiredColors == null || requiredColors.Length == 0)
        {
            if (requireAllTargets)
            {
                return CheckAllTargetsCompleted();
            }
            else
            {
                return true;
            }
        }
        
        // Her gerekli rengi kontrol et
        foreach (LaserColorType requiredColor in requiredColors)
        {
            if (!CheckColorCompleted(requiredColor))
            {
                return false;
            }
        }
        
        Debug.Log($"[LevelManager] Tüm gereksinimler tamamlandı!");
        return true;
    }
    
    private bool CheckColorCompleted(LaserColorType color)
    {
        if (!targetsByColor.ContainsKey(color))
        {
            Debug.LogWarning($"[LevelManager] {color} rengi için hedef yok!");
            return false;
        }
        
        List<LaserTarget> colorTargets = targetsByColor[color];
        
        foreach (LaserTarget target in colorTargets)
        {
            if (!target.IsActivated)
            {
                return false;
            }
        }
        
        return true;
    }
    
    private bool CheckAllTargetsCompleted()
    {
        foreach (LaserTarget target in allTargets)
        {
            if (!target.IsActivated)
            {
                return false;
            }
        }
        return true;
    }
    
    private void UnlockAndOpenDoor()
    {
        if (exitDoor != null)
        {
            exitDoor.Unlock();
            
            if (!exitDoor.IsOpen)
            {
                exitDoor.Open();
                Debug.Log($"[LevelManager] Level {levelName} tamamlandı! Kapı açıldı.");
            }
        }
    }
    
    private void LockDoor()
    {
        if (exitDoor != null && !exitDoor.IsLocked)
        {
            exitDoor.Lock();
            
            if (exitDoor.IsOpen)
            {
                exitDoor.Close();
            }
        }
    }
    
    private void OnLevelCompleted()
    {
        float completionTime = GameManager.Instance != null ? GameManager.Instance.CurrentTime : 60f;
        int stars = 1; // Default 1 star
        
        // Try to get stars from level data
        if (currentLevelData != null)
        {
            stars = currentLevelData.CalculateStars(completionTime);
            LevelProgressManager.Instance?.CompleteLevel(currentLevelData, completionTime, stars);
        }
        
        // ALWAYS save to PlayerPrefs directly as backup
        int previousStars = PlayerPrefs.GetInt($"Level_{levelNumber}_Stars", 0);
        int newStars = Mathf.Max(stars, previousStars);
        
        PlayerPrefs.SetInt($"Level_{levelNumber}_Completed", 1);
        PlayerPrefs.SetInt($"Level_{levelNumber}_Stars", newStars);
        
        float existingBestTime = PlayerPrefs.GetFloat($"Level_{levelNumber}_BestTime", 999f);
        if (completionTime < existingBestTime)
        {
            PlayerPrefs.SetFloat($"Level_{levelNumber}_BestTime", completionTime);
        }
        
        // Calculate and save TotalStars
        int totalStars = CalculateTotalStars();
        PlayerPrefs.SetInt("TotalStars", totalStars);
        
        PlayerPrefs.Save();
        
        Debug.Log($"[LevelManager] Level {levelNumber} tamamlandı ve kaydedildi! Time: {completionTime:F1}s, Stars: {stars}, TotalStars: {totalStars}");
    }
    
    private int CalculateTotalStars()
    {
        int total = 0;
        // Load all level stars from PlayerPrefs
        for (int i = 1; i <= 10; i++) // Check up to 10 levels
        {
            int stars = PlayerPrefs.GetInt($"Level_{i}_Stars", 0);
            total += stars;
        }
        return total;
    }
    
    // Public methods
    public int GetTargetCountForColor(LaserColorType color)
    {
        if (targetsByColor.ContainsKey(color))
        {
            return targetsByColor[color].Count;
        }
        return 0;
    }
    
    public int GetCompletedTargetCountForColor(LaserColorType color)
    {
        if (targetsByColor.ContainsKey(color))
        {
            return targetsByColor[color].Count(t => t.IsActivated);
        }
        return 0;
    }
    
    // UI için getter metodlar
    public string GetLevelName()
    {
        return levelName;
    }
    
    public int GetLevelNumber()
    {
        return levelNumber;
    }
    
    public int GetTotalTargetCount()
    {
        return allTargets.Count;
    }
    
    public int GetCompletedTargetCount()
    {
        return allTargets.Count(t => t.IsActivated);
    }
}