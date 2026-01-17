using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LevelProgressManager : MonoBehaviour
{
    public static LevelProgressManager Instance { get; private set; }
    
    [Header("All Levels")]
    [SerializeField] private LevelData[] allLevels;
    
    [Header("Current Session")]
    private LevelData currentLevel;
    private int currentLevelIndex = 0;
    
    private const string SAVE_KEY = "LaserGameProgress";
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        LoadProgress();
        UpdateLevelUnlocks();
    }
    
    #region Level Selection
    
    public void LoadLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= allLevels.Length)
        {
            Debug.LogError($"Level index {levelIndex} out of range!");
            return;
        }
        
        LevelData levelData = allLevels[levelIndex];
        
        if (!levelData.isUnlocked)
        {
            Debug.LogWarning($"Level {levelData.levelName} is locked!");
            return;
        }
        
        currentLevel = levelData;
        currentLevelIndex = levelIndex;
        
        // Load the scene
        if (!string.IsNullOrEmpty(levelData.sceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(levelData.sceneName);
        }
        else if (levelData.sceneBuildIndex >= 0)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(levelData.sceneBuildIndex);
        }
    }
    
    public void LoadLevel(LevelData levelData)
    {
        int index = System.Array.IndexOf(allLevels, levelData);
        if (index >= 0)
        {
            LoadLevel(index);
        }
    }
    
    public void LoadNextLevel()
    {
        int nextIndex = currentLevelIndex + 1;
        
        if (nextIndex < allLevels.Length)
        {
            LoadLevel(nextIndex);
        }
        else
        {
            Debug.Log("No more levels! Returning to menu...");
            LoadMainMenu();
        }
    }
    
    public void ReloadCurrentLevel()
    {
        if (currentLevel != null)
        {
            LoadLevel(currentLevelIndex);
        }
    }
    
    public void LoadMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
    
    #endregion
    
    #region Progress Management
    
    public void CompleteLevel(LevelData levelData, float completionTime, int stars)
    {
        if (levelData == null) return;
        
        bool isNewBest = false;
        
        // Update best time and stars
        if (!levelData.isCompleted || completionTime < levelData.bestTime)
        {
            levelData.bestTime = completionTime;
            isNewBest = true;
        }
        
        if (stars > levelData.bestStars)
        {
            levelData.bestStars = stars;
        }
        
        levelData.isCompleted = true;
        
        // Unlock next levels
        UpdateLevelUnlocks();
        
        // Save progress
        SaveProgress();
        
        Debug.Log($"Level completed! {levelData.levelName} - Time: {completionTime:F2}s - Stars: {stars}/3" + 
                  (isNewBest ? " (NEW BEST!)" : ""));
    }
    
    private void UpdateLevelUnlocks()
    {
        // First level is always unlocked
        if (allLevels.Length > 0)
        {
            allLevels[0].isUnlocked = true;
        }
        
        int totalStars = GetTotalStars();
        
        // Check unlock requirements for each level
        for (int i = 1; i < allLevels.Length; i++)
        {
            LevelData level = allLevels[i];
            
            bool meetsRequirements = true;
            
            // Check if required level is completed
            if (level.requiredLevel > 0)
            {
                if (level.requiredLevel - 1 < allLevels.Length)
                {
                    if (!allLevels[level.requiredLevel - 1].isCompleted)
                    {
                        meetsRequirements = false;
                    }
                }
            }
            else
            {
                // Default: previous level must be completed
                if (!allLevels[i - 1].isCompleted)
                {
                    meetsRequirements = false;
                }
            }
            
            // Check star requirement
            if (totalStars < level.requiredStars)
            {
                meetsRequirements = false;
            }
            
            level.isUnlocked = meetsRequirements;
        }
    }
    
    public int GetTotalStars()
    {
        int total = 0;
        foreach (LevelData level in allLevels)
        {
            total += level.bestStars;
        }
        return total;
    }
    
    public int GetCompletedLevelsCount()
    {
        return allLevels.Count(l => l.isCompleted);
    }
    
    #endregion
    
    #region Save/Load System
    
    [System.Serializable]
    private class SaveData
    {
        public List<LevelSaveData> levels = new List<LevelSaveData>();
    }
    
    [System.Serializable]
    private class LevelSaveData
    {
        public int levelNumber;
        public bool isCompleted;
        public float bestTime;
        public int bestStars;
    }
    
    public void SaveProgress()
    {
        SaveData data = new SaveData();
        
        foreach (LevelData level in allLevels)
        {
            data.levels.Add(new LevelSaveData
            {
                levelNumber = level.levelNumber,
                isCompleted = level.isCompleted,
                bestTime = level.bestTime,
                bestStars = level.bestStars
            });
            
            // Also save individual PlayerPrefs for compatibility with MainMenuUI
            PlayerPrefs.SetInt($"Level_{level.levelNumber}_Completed", level.isCompleted ? 1 : 0);
            PlayerPrefs.SetInt($"Level_{level.levelNumber}_Stars", level.bestStars);
            PlayerPrefs.SetFloat($"Level_{level.levelNumber}_BestTime", level.bestTime);
        }
        
        // Save total stars
        PlayerPrefs.SetInt("TotalStars", GetTotalStars());
        
        string json = JsonUtility.ToJson(data, true);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
        
        Debug.Log("Progress saved!");
    }
    
    public void LoadProgress()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY))
        {
            Debug.Log("No save data found. Starting fresh.");
            return;
        }
        
        string json = PlayerPrefs.GetString(SAVE_KEY);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        
        foreach (LevelSaveData saveData in data.levels)
        {
            LevelData level = allLevels.FirstOrDefault(l => l.levelNumber == saveData.levelNumber);
            if (level != null)
            {
                level.isCompleted = saveData.isCompleted;
                level.bestTime = saveData.bestTime;
                level.bestStars = saveData.bestStars;
            }
        }
        
        Debug.Log("Progress loaded!");
    }
    
    public void ResetProgress()
    {
        foreach (LevelData level in allLevels)
        {
            level.isCompleted = false;
            level.bestTime = 999f;
            level.bestStars = 0;
            level.isUnlocked = false;
        }
        
        UpdateLevelUnlocks();
        SaveProgress();
        
        Debug.Log("Progress reset!");
    }
    
    #endregion
    
    #region Getters
    
    public LevelData GetCurrentLevelData()
    {
        return currentLevel;
    }
    
    public LevelData[] GetAllLevels()
    {
        return allLevels;
    }
    
    public LevelData GetLevel(int levelNumber)
    {
        return allLevels.FirstOrDefault(l => l.levelNumber == levelNumber);
    }
    
    #endregion
}