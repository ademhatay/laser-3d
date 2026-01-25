using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button levelSelectButton;
    [SerializeField] private Button quitButton;
    
    [Header("Level Select")]
    [SerializeField] private string levelsResourcePath = "Levels";
    
    // GUI State
    private bool showLevelSelect = false;
    private LevelData[] allLevels;
    private LevelData selectedLevel;
    private Vector2 scrollPosition;
    private LevelData levelToLoad = null; // Flag for loading level
    
    // GUI Styles
    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle levelButtonStyle;
    private GUIStyle levelButtonLockedStyle;
    private GUIStyle levelButtonCompletedStyle;
    private GUIStyle infoStyle;
    private GUIStyle descriptionStyle;
    private Texture2D overlayTexture;
    private Texture2D panelTexture;
    private Texture2D buttonTexture;
    private Texture2D buttonHoverTexture;
    private Texture2D levelBtnTexture;
    private Texture2D levelBtnLockedTexture;
    private Texture2D levelBtnCompletedTexture;
    private bool stylesInitialized = false;
    
    void Start()
    {
        // Ensure cursor is visible
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
        
        // Load all levels
        LoadAllLevels();
        
        // Setup button listeners
        if (playButton != null)
            playButton.onClick.AddListener(PlayGame);
        
        if (levelSelectButton != null)
            levelSelectButton.onClick.AddListener(OpenLevelSelect);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }
    
    void Update()
    {
        // Handle level loading outside of OnGUI
        if (levelToLoad != null)
        {
            LevelData level = levelToLoad;
            levelToLoad = null;
            
            PlayerPrefs.SetInt("CurrentLevelNumber", level.levelNumber);
            PlayerPrefs.Save();
            
            Debug.Log($"[Update] Loading level: {level.levelName}, BuildIndex: {level.sceneBuildIndex}");
            
            if (level.sceneBuildIndex >= 0)
            {
                SceneManager.LoadScene(level.sceneBuildIndex);
            }
            else if (!string.IsNullOrEmpty(level.sceneName))
            {
                SceneManager.LoadScene(level.sceneName);
            }
        }
    }
    
    private void LoadAllLevels()
    {
        allLevels = Resources.LoadAll<LevelData>(levelsResourcePath);
        System.Array.Sort(allLevels, (a, b) => a.levelNumber.CompareTo(b.levelNumber));
        
        Debug.Log($"[MainMenuUI] {allLevels.Length} level yüklendi.");
        
        // Load progress for each level
        foreach (LevelData level in allLevels)
        {
            level.isCompleted = PlayerPrefs.GetInt($"Level_{level.levelNumber}_Completed", 0) == 1;
            level.bestStars = PlayerPrefs.GetInt($"Level_{level.levelNumber}_Stars", 0);
            level.bestTime = PlayerPrefs.GetFloat($"Level_{level.levelNumber}_BestTime", 999f);
        }
        
        // Update unlock status based on previous level completion
        for (int i = 0; i < allLevels.Length; i++)
        {
            LevelData level = allLevels[i];
            
            if (level.levelNumber == 1)
            {
                // First level is always unlocked
                level.isUnlocked = true;
            }
            else
            {
                // Check if previous level is completed
                int prevLevelNum = level.levelNumber - 1;
                bool prevCompleted = PlayerPrefs.GetInt($"Level_{prevLevelNum}_Completed", 0) == 1;
                
                // Also check required stars if any
                int totalStars = PlayerPrefs.GetInt("TotalStars", 0);
                
                level.isUnlocked = prevCompleted && totalStars >= level.requiredStars;
            }
            
            Debug.Log($"Level {level.levelNumber}: Completed={level.isCompleted}, Unlocked={level.isUnlocked}");
        }
    }
    
    public void PlayGame()
    {
        Debug.Log("Starting game...");
        // Load first level (Level_01)
        SceneManager.LoadScene(1); // Level_01 build index
    }
    
    public void OpenLevelSelect()
    {
        showLevelSelect = true;
        selectedLevel = null;
    }
    
    public void CloseLevelSelect()
    {
        showLevelSelect = false;
        selectedLevel = null;
    }
    
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    private void InitializeStyles()
    {
        if (stylesInitialized) return;
        
        // Textures
        overlayTexture = MakeTexture(new Color(0, 0, 0, 0.9f));
        panelTexture = MakeTexture(new Color(0.1f, 0.1f, 0.15f, 0.95f));
        buttonTexture = MakeTexture(new Color(0.2f, 0.2f, 0.25f, 1f));
        buttonHoverTexture = MakeTexture(new Color(0.1f, 0.5f, 0.8f, 1f));
        levelBtnTexture = MakeTexture(new Color(0.15f, 0.4f, 0.6f, 1f));
        levelBtnLockedTexture = MakeTexture(new Color(0.3f, 0.3f, 0.3f, 1f));
        levelBtnCompletedTexture = MakeTexture(new Color(0.2f, 0.6f, 0.3f, 1f));
        
        // Title style
        titleStyle = new GUIStyle();
        titleStyle.fontSize = 48;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = new Color(0.3f, 0.8f, 1f, 1f);
        
        // Button style
        buttonStyle = new GUIStyle();
        buttonStyle.fontSize = 22;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.normal.background = buttonTexture;
        buttonStyle.hover.textColor = Color.white;
        buttonStyle.hover.background = buttonHoverTexture;
        buttonStyle.active.textColor = Color.cyan;
        buttonStyle.padding = new RectOffset(15, 15, 12, 12);
        
        // Level button style
        levelButtonStyle = new GUIStyle();
        levelButtonStyle.fontSize = 28;
        levelButtonStyle.fontStyle = FontStyle.Bold;
        levelButtonStyle.alignment = TextAnchor.MiddleCenter;
        levelButtonStyle.normal.textColor = Color.white;
        levelButtonStyle.normal.background = levelBtnTexture;
        levelButtonStyle.hover.background = buttonHoverTexture;
        levelButtonStyle.padding = new RectOffset(10, 10, 10, 10);
        
        // Locked button style
        levelButtonLockedStyle = new GUIStyle(levelButtonStyle);
        levelButtonLockedStyle.normal.background = levelBtnLockedTexture;
        levelButtonLockedStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        levelButtonLockedStyle.hover.background = levelBtnLockedTexture;
        
        // Completed button style
        levelButtonCompletedStyle = new GUIStyle(levelButtonStyle);
        levelButtonCompletedStyle.normal.background = levelBtnCompletedTexture;
        
        // Info style
        infoStyle = new GUIStyle();
        infoStyle.fontSize = 24;
        infoStyle.fontStyle = FontStyle.Bold;
        infoStyle.alignment = TextAnchor.MiddleLeft;
        infoStyle.normal.textColor = Color.white;
        infoStyle.wordWrap = true;
        
        // Description style
        descriptionStyle = new GUIStyle();
        descriptionStyle.fontSize = 18;
        descriptionStyle.alignment = TextAnchor.UpperLeft;
        descriptionStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        descriptionStyle.wordWrap = true;
        
        stylesInitialized = true;
    }
    
    private Texture2D MakeTexture(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
    
    void OnGUI()
    {
        if (!showLevelSelect) return;
        
        InitializeStyles();
        
        float screenW = Screen.width;
        float screenH = Screen.height;
        float centerX = screenW / 2f;
        float centerY = screenH / 2f;
        
        // Draw overlay
        GUI.DrawTexture(new Rect(0, 0, screenW, screenH), overlayTexture);
        
        // Panel dimensions
        float panelW = Mathf.Min(900, screenW - 40);
        float panelH = Mathf.Min(650, screenH - 40);
        float panelX = centerX - panelW / 2;
        float panelY = centerY - panelH / 2;
        
        // Draw panel background
        GUI.DrawTexture(new Rect(panelX, panelY, panelW, panelH), panelTexture);
        
        // Title
        GUI.Label(new Rect(panelX, panelY + 15, panelW, 60), "🎮 LEVEL SEÇİMİ", titleStyle);
        
        // Back button
        if (GUI.Button(new Rect(panelX + 15, panelY + 20, 100, 40), "← GERİ", buttonStyle))
        {
            if (selectedLevel != null)
                selectedLevel = null;
            else
                CloseLevelSelect();
        }
        
        // Content area
        float contentY = panelY + 90;
        float contentH = panelH - 110;
        
        if (selectedLevel != null)
        {
            DrawLevelDetails(panelX + 20, contentY, panelW - 40, contentH);
        }
        else
        {
            DrawLevelGrid(panelX + 20, contentY, panelW - 40, contentH);
        }
    }
    
    private void DrawLevelGrid(float x, float y, float width, float height)
    {
        if (allLevels == null || allLevels.Length == 0)
        {
            GUI.Label(new Rect(x, y + 50, width, 40), "Hiç level bulunamadı!", infoStyle);
            return;
        }
        
        float buttonSize = 100;
        float spacing = 15;
        int buttonsPerRow = Mathf.FloorToInt((width + spacing) / (buttonSize + spacing));
        
        float totalWidth = buttonsPerRow * buttonSize + (buttonsPerRow - 1) * spacing;
        float startX = x + (width - totalWidth) / 2;
        
        // Calculate scroll area
        int rows = Mathf.CeilToInt((float)allLevels.Length / buttonsPerRow);
        float contentHeight = rows * (buttonSize + spacing);
        
        scrollPosition = GUI.BeginScrollView(
            new Rect(x, y, width, height),
            scrollPosition,
            new Rect(0, 0, width - 20, contentHeight)
        );
        
        for (int i = 0; i < allLevels.Length; i++)
        {
            LevelData level = allLevels[i];
            int row = i / buttonsPerRow;
            int col = i % buttonsPerRow;
            
            float btnX = (startX - x) + col * (buttonSize + spacing);
            float btnY = row * (buttonSize + spacing);
            
            // Select style based on level state
            GUIStyle style;
            if (!level.isUnlocked)
                style = levelButtonLockedStyle;
            else if (level.isCompleted)
                style = levelButtonCompletedStyle;
            else
                style = levelButtonStyle;
            
            // Button content
            string btnText = level.isUnlocked ? level.levelNumber.ToString() : "🔒";
            if (level.isCompleted && level.isUnlocked)
            {
                btnText += "\n" + GetStarString(level.bestStars);
            }
            
            // Draw button
            GUI.enabled = level.isUnlocked;
            if (GUI.Button(new Rect(btnX, btnY, buttonSize, buttonSize), btnText, style))
            {
                selectedLevel = level;
            }
            GUI.enabled = true;
        }
        
        GUI.EndScrollView();
    }
    
    private void DrawLevelDetails(float x, float y, float width, float height)
    {
        // Level name
        titleStyle.fontSize = 36;
        GUI.Label(new Rect(x, y, width, 50), selectedLevel.levelName, titleStyle);
        titleStyle.fontSize = 48;
        
        y += 60;
        
        // Stars display
        string starsDisplay = "Yıldızlar: " + GetStarString(selectedLevel.bestStars);
        infoStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(x, y, width, 35), starsDisplay, infoStyle);
        infoStyle.normal.textColor = Color.white;
        
        y += 45;
        
        // Description
        GUI.Label(new Rect(x, y, width, 80), selectedLevel.levelDescription, descriptionStyle);
        
        y += 90;
        
        // Stats
        DrawStat(x, y, "🪞 Aynalar:", selectedLevel.totalMirrors.ToString());
        y += 35;
        DrawStat(x, y, "🎯 Hedefler:", selectedLevel.totalTargets.ToString());
        y += 35;
        
        if (selectedLevel.totalCollectables > 0)
        {
            DrawStat(x, y, "💎 Toplanabilir:", selectedLevel.totalCollectables.ToString());
            y += 35;
        }
        
        if (selectedLevel.timeLimit > 0)
        {
            DrawStat(x, y, "⏱ Süre Limiti:", $"{selectedLevel.timeLimit}s");
            y += 35;
        }
        
        y += 10;
        
        // Star time requirements
        descriptionStyle.fontSize = 16;
        GUI.Label(new Rect(x, y, width, 25), $"⭐⭐⭐ {selectedLevel.threeStarTime}s altı  |  ⭐⭐ {selectedLevel.twoStarTime}s altı  |  ⭐ {selectedLevel.oneStarTime}s altı", descriptionStyle);
        descriptionStyle.fontSize = 18;
        
        y += 35;
        
        // Best time if completed
        if (selectedLevel.isCompleted && selectedLevel.bestTime < 999f)
        {
            DrawStat(x, y, "🏆 En İyi Süre:", $"{selectedLevel.bestTime:F1}s");
            y += 45;
        }
        
        // Play button - fixed position at bottom of panel
        float playBtnW = 200;
        float playBtnH = 55;
        float playBtnX = x + (width - playBtnW) / 2;
        float playBtnY = y + 30; // Position after stats
        
        if (GUI.Button(new Rect(playBtnX, playBtnY, playBtnW, playBtnH), "▶ OYNA", buttonStyle))
        {
            Debug.Log($"OYNA tıklandı! Level: {selectedLevel.levelName}, BuildIndex: {selectedLevel.sceneBuildIndex}");
            // Set flag to load level in Update (not in OnGUI)
            levelToLoad = selectedLevel;
        }
    }
    
    private void DrawStat(float x, float y, string label, string value)
    {
        GUI.Label(new Rect(x, y, 200, 30), label, infoStyle);
        GUI.Label(new Rect(x + 180, y, 100, 30), value, infoStyle);
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
    
    private void PlayLevel(LevelData level)
    {
        if (level == null) 
        {
            Debug.LogError("Level null!");
            return;
        }
        
        if (!level.isUnlocked)
        {
            Debug.LogWarning("Level kilitli!");
            return;
        }
        
        // Store current level
        PlayerPrefs.SetInt("CurrentLevelNumber", level.levelNumber);
        PlayerPrefs.Save();
        
        Debug.Log($"Loading level: {level.levelName}, Scene: {level.sceneName}, BuildIndex: {level.sceneBuildIndex}");
        
        // Close level select first
        showLevelSelect = false;
        
        // Load by build index
        if (level.sceneBuildIndex >= 0 && level.sceneBuildIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(level.sceneBuildIndex);
        }
        else if (!string.IsNullOrEmpty(level.sceneName))
        {
            SceneManager.LoadScene(level.sceneName);
        }
        else
        {
            Debug.LogError($"Level {level.levelNumber} için sahne bulunamadı!");
        }
    }
}
