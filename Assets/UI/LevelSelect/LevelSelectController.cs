using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Linq;

public class LevelSelectController : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;
    
    [Header("Settings")]
    [SerializeField] private string levelsResourcePath = "Levels";
    
    private VisualElement root;
    private VisualElement levelGrid;
    private ScrollView levelScrollView;
    private VisualElement levelInfoPanel;
    private Button backButton;
    private Button playLevelButton;
    private Label levelNameLabel;
    private Label levelDescriptionLabel;
    private Label statsLabel;
    private Label star1, star2, star3;
    
    private LevelData[] allLevels;
    private LevelData selectedLevel;
    
    void Start()
    {
        // Ensure cursor is visible
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        Time.timeScale = 1f;
        
        // Get UI Document if not assigned
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
        
        if (uiDocument == null)
        {
            Debug.LogError("[LevelSelectController] UIDocument component not found!");
            return;
        }
        
        // Get root visual element
        root = uiDocument.rootVisualElement;
        
        // Get UI elements
        levelGrid = root.Q<VisualElement>("levelGrid");
        levelScrollView = root.Q<ScrollView>("levelScrollView");
        levelInfoPanel = root.Q<VisualElement>("levelInfoPanel");
        backButton = root.Q<Button>("backBtn");
        playLevelButton = root.Q<Button>("playLevelBtn");
        levelNameLabel = root.Q<Label>("levelNameLabel");
        levelDescriptionLabel = root.Q<Label>("levelDescriptionLabel");
        statsLabel = root.Q<Label>("statsLabel");
        star1 = root.Q<Label>("star1");
        star2 = root.Q<Label>("star2");
        star3 = root.Q<Label>("star3");
        
        // Setup button listeners
        if (backButton != null)
            backButton.clicked += OnBackClicked;
        
        if (playLevelButton != null)
            playLevelButton.clicked += OnPlayLevelClicked;
        
        // Load and display levels
        LoadAllLevels();
        CreateLevelButtons();
    }
    
    private void LoadAllLevels()
    {
        // Load all LevelData assets from Resources
        allLevels = Resources.LoadAll<LevelData>(levelsResourcePath);
        
        // Sort by level number
        allLevels = allLevels.OrderBy(l => l.levelNumber).ToArray();
        
        Debug.Log($"[LevelSelectController] Loaded {allLevels.Length} levels");
        
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
                // Determine which level needs to be completed
                int requiredLevelNum = level.requiredLevel;
                
                // If requiredLevel is 0 or invalid, use previous level (levelNumber - 1)
                if (requiredLevelNum <= 0 || requiredLevelNum >= level.levelNumber)
                {
                    requiredLevelNum = level.levelNumber - 1;
                }
                
                // Check if required level is completed
                int requiredCompleted = PlayerPrefs.GetInt($"Level_{requiredLevelNum}_Completed", 0);
                int totalStars = PlayerPrefs.GetInt("TotalStars", 0);
                
                level.isUnlocked = requiredCompleted == 1 && totalStars >= level.requiredStars;
                
                Debug.Log($"[LevelSelectController] Level {level.levelNumber}: requiredLevel={requiredLevelNum}, completed={requiredCompleted}, totalStars={totalStars}, requiredStars={level.requiredStars}, unlocked={level.isUnlocked}");
            }
            
            // Load best stats
            level.isCompleted = PlayerPrefs.GetInt($"Level_{level.levelNumber}_Completed", 0) == 1;
            level.bestStars = PlayerPrefs.GetInt($"Level_{level.levelNumber}_Stars", 0);
            level.bestTime = PlayerPrefs.GetFloat($"Level_{level.levelNumber}_BestTime", 999f);
        }
    }
    
    private void CreateLevelButtons()
    {
        if (levelGrid == null)
        {
            Debug.LogError("[LevelSelectController] Level grid not found!");
            return;
        }
        
        // Clear existing buttons
        levelGrid.Clear();
        
        foreach (LevelData level in allLevels)
        {
            // Create button container
            VisualElement levelButton = new VisualElement();
            levelButton.name = $"LevelButton_{level.levelNumber}";
            
            // Add classes based on state
            levelButton.AddToClassList("level-button");
            if (!level.isUnlocked)
            {
                levelButton.AddToClassList("level-button-locked");
            }
            else if (level.isCompleted)
            {
                levelButton.AddToClassList("level-button-completed");
            }
            
            // Create level number label
            Label levelNumberLabel = new Label();
            if (level.isUnlocked)
            {
                levelNumberLabel.text = level.levelNumber.ToString();
                levelNumberLabel.AddToClassList("level-number");
            }
            else
            {
                levelNumberLabel.text = "🔒";
                levelNumberLabel.AddToClassList("level-locked-icon");
            }
            
            levelButton.Add(levelNumberLabel);
            
            // Add stars if completed
            if (level.isUnlocked && level.isCompleted && level.bestStars > 0)
            {
                Label starsLabel = new Label();
                starsLabel.text = GetStarString(level.bestStars);
                starsLabel.AddToClassList("level-stars");
                levelButton.Add(starsLabel);
            }
            
            // Make button clickable if unlocked
            if (level.isUnlocked)
            {
                levelButton.RegisterCallback<ClickEvent>(evt => OnLevelClicked(level));
            }
            
            // Add to grid
            levelGrid.Add(levelButton);
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
    
    private void OnLevelClicked(LevelData level)
    {
        if (!level.isUnlocked)
        {
            Debug.LogWarning($"Level {level.levelNumber} is locked!");
            return;
        }
        
        selectedLevel = level;
        ShowLevelInfo(level);
    }
    
    private void ShowLevelInfo(LevelData level)
    {
        if (levelInfoPanel == null) return;
        
        // Show panel
        levelInfoPanel.style.display = DisplayStyle.Flex;
        
        // Update labels
        if (levelNameLabel != null)
            levelNameLabel.text = level.levelName;
        
        if (levelDescriptionLabel != null)
            levelDescriptionLabel.text = level.levelDescription;
        
        // Update stars
        if (star1 != null) star1.RemoveFromClassList("star-filled");
        if (star2 != null) star2.RemoveFromClassList("star-filled");
        if (star3 != null) star3.RemoveFromClassList("star-filled");
        
        if (level.bestStars >= 1 && star1 != null) star1.AddToClassList("star-filled");
        if (level.bestStars >= 2 && star2 != null) star2.AddToClassList("star-filled");
        if (level.bestStars >= 3 && star3 != null) star3.AddToClassList("star-filled");
        
        // Update stats
        if (statsLabel != null)
        {
            string stats = $"Mirrors: {level.totalMirrors}\n";
            stats += $"Targets: {level.totalTargets}\n";
            if (level.totalCollectables > 0)
                stats += $"Collectables: {level.totalCollectables}\n";
            if (level.timeLimit > 0)
                stats += $"Time Limit: {level.timeLimit}s\n";
            if (level.isCompleted && level.bestTime < 999f)
                stats += $"\nBest Time: {level.bestTime:F1}s";
            
            statsLabel.text = stats;
        }
    }
    
    private void OnPlayLevelClicked()
    {
        if (selectedLevel == null)
        {
            Debug.LogWarning("[LevelSelectController] No level selected!");
            return;
        }
        
        if (!selectedLevel.isUnlocked)
        {
            Debug.LogWarning($"[LevelSelectController] Level {selectedLevel.levelNumber} is locked!");
            return;
        }
        
        // Store current level
        PlayerPrefs.SetInt("CurrentLevelNumber", selectedLevel.levelNumber);
        PlayerPrefs.Save();
        
        Debug.Log($"[LevelSelectController] Loading level: {selectedLevel.levelName}, Scene: {selectedLevel.sceneName}");
        
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
            Debug.LogError($"[LevelSelectController] Level {selectedLevel.levelNumber} has no valid scene reference!");
        }
    }
    
    private void OnBackClicked()
    {
        if (levelInfoPanel != null && levelInfoPanel.style.display == DisplayStyle.Flex)
        {
            // Hide info panel
            levelInfoPanel.style.display = DisplayStyle.None;
            selectedLevel = null;
        }
        else
        {
            // Return to main menu
            SceneManager.LoadScene("MainMenu");
        }
    }
    
    void OnDestroy()
    {
        // Clean up event listeners
        if (backButton != null)
            backButton.clicked -= OnBackClicked;
        
        if (playLevelButton != null)
            playLevelButton.clicked -= OnPlayLevelClicked;
    }
}
