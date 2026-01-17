using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class LevelSelectUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform levelButtonContainer;
    [SerializeField] private GameObject levelButtonPrefab;
    [SerializeField] private Button backButton;
    [SerializeField] private TextMeshProUGUI titleText;
    
    [Header("Level Info Panel")]
    [SerializeField] private GameObject levelInfoPanel;
    [SerializeField] private TextMeshProUGUI levelNameText;
    [SerializeField] private TextMeshProUGUI levelDescriptionText;
    [SerializeField] private TextMeshProUGUI levelStatsText;
    [SerializeField] private Image[] starImages;
    [SerializeField] private Button playLevelButton;
    
    [Header("Settings")]
    [SerializeField] private string levelsResourcePath = "Levels";
    
    private LevelData[] allLevels;
    private LevelData selectedLevel;
    private List<GameObject> spawnedButtons = new List<GameObject>();
    
    void Start()
    {
        LoadAllLevels();
        CreateLevelButtons();
        
        if (backButton != null)
            backButton.onClick.AddListener(GoBack);
        
        if (playLevelButton != null)
            playLevelButton.onClick.AddListener(PlaySelectedLevel);
        
        if (levelInfoPanel != null)
            levelInfoPanel.SetActive(false);
    }
    
    private void LoadAllLevels()
    {
        // Load all LevelData assets from Resources/Levels
        allLevels = Resources.LoadAll<LevelData>(levelsResourcePath);
        
        // Sort by level number
        System.Array.Sort(allLevels, (a, b) => a.levelNumber.CompareTo(b.levelNumber));
        
        Debug.Log($"[LevelSelectUI] {allLevels.Length} level yüklendi.");
        
        // Load progress from PlayerPrefs
        LoadLevelProgress();
    }
    
    private void LoadLevelProgress()
    {
        foreach (LevelData level in allLevels)
        {
            // First level is always unlocked
            if (level.levelNumber == 1)
            {
                level.isUnlocked = true;
            }
            else
            {
                // Check if required level is completed
                int requiredCompleted = PlayerPrefs.GetInt($"Level_{level.requiredLevel}_Completed", 0);
                int totalStars = PlayerPrefs.GetInt("TotalStars", 0);
                
                level.isUnlocked = requiredCompleted == 1 && totalStars >= level.requiredStars;
            }
            
            // Load best stats
            level.isCompleted = PlayerPrefs.GetInt($"Level_{level.levelNumber}_Completed", 0) == 1;
            level.bestStars = PlayerPrefs.GetInt($"Level_{level.levelNumber}_Stars", 0);
            level.bestTime = PlayerPrefs.GetFloat($"Level_{level.levelNumber}_BestTime", 999f);
        }
    }
    
    private void CreateLevelButtons()
    {
        // Clear existing buttons
        foreach (var btn in spawnedButtons)
        {
            if (btn != null) Destroy(btn);
        }
        spawnedButtons.Clear();
        
        foreach (LevelData level in allLevels)
        {
            GameObject buttonObj;
            
            if (levelButtonPrefab != null && levelButtonContainer != null)
            {
                buttonObj = Instantiate(levelButtonPrefab, levelButtonContainer);
            }
            else
            {
                // Create button dynamically if no prefab
                buttonObj = new GameObject($"LevelButton_{level.levelNumber}");
                buttonObj.transform.SetParent(levelButtonContainer);
                
                var rectTransform = buttonObj.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(120, 120);
                
                var image = buttonObj.AddComponent<Image>();
                image.color = level.isUnlocked ? level.themeColor : Color.gray;
                
                var button = buttonObj.AddComponent<Button>();
                
                // Add text
                var textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform);
                var text = textObj.AddComponent<TextMeshProUGUI>();
                text.text = level.isUnlocked ? level.levelNumber.ToString() : "🔒";
                text.fontSize = 36;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;
                
                var textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
            }
            
            // Setup button
            var levelButton = buttonObj.GetComponent<Button>();
            if (levelButton != null)
            {
                LevelData capturedLevel = level;
                levelButton.onClick.AddListener(() => SelectLevel(capturedLevel));
                levelButton.interactable = level.isUnlocked;
            }
            
            // Update button visuals
            UpdateButtonVisuals(buttonObj, level);
            
            spawnedButtons.Add(buttonObj);
        }
    }
    
    private void UpdateButtonVisuals(GameObject buttonObj, LevelData level)
    {
        // Find text component
        var text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            if (level.isUnlocked)
            {
                text.text = level.levelNumber.ToString();
                if (level.isCompleted)
                {
                    text.text += "\n" + GetStarString(level.bestStars);
                }
            }
            else
            {
                text.text = "🔒";
            }
        }
        
        // Update image color
        var image = buttonObj.GetComponent<Image>();
        if (image != null)
        {
            if (!level.isUnlocked)
                image.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            else if (level.isCompleted)
                image.color = new Color(0.2f, 0.8f, 0.2f, 1f);
            else
                image.color = level.themeColor;
        }
    }
    
    private string GetStarString(int stars)
    {
        string result = "";
        for (int i = 0; i < 3; i++)
        {
            result += i < stars ? "★" : "☆";
        }
        return result;
    }
    
    public void SelectLevel(LevelData level)
    {
        if (!level.isUnlocked)
        {
            Debug.Log($"Level {level.levelNumber} kilitli!");
            return;
        }
        
        selectedLevel = level;
        ShowLevelInfo(level);
    }
    
    private void ShowLevelInfo(LevelData level)
    {
        if (levelInfoPanel != null)
            levelInfoPanel.SetActive(true);
        
        if (levelNameText != null)
            levelNameText.text = level.levelName;
        
        if (levelDescriptionText != null)
            levelDescriptionText.text = level.levelDescription;
        
        if (levelStatsText != null)
        {
            string stats = $"Aynalar: {level.totalMirrors}\n";
            stats += $"Hedefler: {level.totalTargets}\n";
            if (level.totalCollectables > 0)
                stats += $"Toplanabilir: {level.totalCollectables}\n";
            if (level.timeLimit > 0)
                stats += $"Süre Limiti: {level.timeLimit}s\n";
            if (level.isCompleted)
                stats += $"\nEn İyi Süre: {level.bestTime:F1}s";
            
            levelStatsText.text = stats;
        }
        
        // Update star display
        if (starImages != null)
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    starImages[i].color = i < level.bestStars ? Color.yellow : Color.gray;
                }
            }
        }
    }
    
    public void PlaySelectedLevel()
    {
        if (selectedLevel == null)
        {
            Debug.LogWarning("Seçili level yok!");
            return;
        }
        
        // Store selected level for LevelProgressManager
        PlayerPrefs.SetInt("CurrentLevelNumber", selectedLevel.levelNumber);
        PlayerPrefs.Save();
        
        Debug.Log($"Loading level: {selectedLevel.sceneName}");
        
        // Load by scene name or build index
        if (!string.IsNullOrEmpty(selectedLevel.sceneName))
        {
            SceneManager.LoadScene(selectedLevel.sceneName);
        }
        else if (selectedLevel.sceneBuildIndex >= 0)
        {
            SceneManager.LoadScene(selectedLevel.sceneBuildIndex);
        }
        else
        {
            Debug.LogError($"Level {selectedLevel.levelNumber} için sahne bulunamadı!");
        }
    }
    
    public void GoBack()
    {
        if (levelInfoPanel != null && levelInfoPanel.activeSelf)
        {
            levelInfoPanel.SetActive(false);
            selectedLevel = null;
        }
        else
        {
            // Return to main menu
            SceneManager.LoadScene("MainMenu");
        }
    }
}
