using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Playing;
    
    [Header("Level Settings")]
    [SerializeField] private float levelTimeLimit = 0f; // 0 = no time limit
    [SerializeField] private int requiredTargets = 1;
    [SerializeField] private bool requireAllTargets = true;
    
    [Header("Win Condition")]
    [SerializeField] private float winDelay = 1.5f; // Delay before showing win screen
    [SerializeField] private bool autoProgressLevel = false;
    
    [Header("Collectables")]
    [SerializeField] private int totalCollectables = 0;
    
    [Header("Audio")]
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip loseSound;
    [SerializeField] private AudioClip collectSound;
    
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject winScreenUI;
    [SerializeField] private GameObject loseScreenUI;
    [SerializeField] private GameObject gameHUD;
    
    // Game Stats
    private float currentTime = 0f;
    private int score = 0;
    private int collectablesCollected = 0;
    private int activatedTargets = 0;
    private List<LaserTarget> allTargets = new List<LaserTarget>();
    private float currentLaserEnergy = 1f; // Normalized 0-1
    
    // Events
    public static event System.Action<GameState> OnGameStateChanged;
    public static event System.Action<int> OnScoreChanged;
    public static event System.Action<float> OnTimeChanged;
    public static event System.Action<int, int> OnCollectableCollected;
    public static event System.Action OnLevelComplete;
    public static event System.Action OnLevelFailed;
    
    // Properties
    public GameState CurrentState => currentState;
    public float CurrentTime => currentTime;
    public int Score => score;
    public int CollectablesCollected => collectablesCollected;
    public int TotalCollectables => totalCollectables;
    public bool IsPlaying => currentState == GameState.Playing;
    
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        Win,
        Lose
    }
    
    void Awake()
    {
        // Singleton pattern
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
        // Find all targets in scene
        FindAllTargets();
        
        // Subscribe to target events
        LaserTarget.OnTargetActivated += HandleTargetActivated;
        LaserTarget.OnTargetDeactivated += HandleTargetDeactivated;
        LaserSource.OnEnergyChanged += HandleEnergyChanged;
        
        // Count collectables if not set
        if (totalCollectables == 0)
        {
            totalCollectables = FindObjectsOfType<Collectable>().Length;
        }
        
        // Start game
        StartGame();
    }
    
    void OnDestroy()
    {
        LaserTarget.OnTargetActivated -= HandleTargetActivated;
        LaserTarget.OnTargetDeactivated -= HandleTargetDeactivated;
        LaserSource.OnEnergyChanged -= HandleEnergyChanged;
    }
    
    void Update()
    {
        if (currentState == GameState.Playing)
        {
            UpdateTimer();
            CheckPauseInput();
        }
        else if (currentState == GameState.Paused)
        {
            CheckPauseInput();
        }
    }
    
    private void FindAllTargets()
    {
        allTargets.Clear();
        LaserTarget[] targets = FindObjectsOfType<LaserTarget>();
        allTargets.AddRange(targets);
        
        if (requiredTargets == 0)
        {
            requiredTargets = allTargets.Count;
        }
    }
    
    private void UpdateTimer()
    {
        currentTime += Time.deltaTime;
        OnTimeChanged?.Invoke(currentTime);
        
        // Check time limit
        if (levelTimeLimit > 0 && currentTime >= levelTimeLimit)
        {
            LoseGame("Time's Up!");
        }
    }
    
    private void CheckPauseInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (currentState == GameState.Playing)
            {
                PauseGame();
            }
            else if (currentState == GameState.Paused)
            {
                ResumeGame();
            }
        }
    }
    
    #region Game State Management
    
    public void StartGame()
    {
        currentTime = 0f;
        score = 0;
        collectablesCollected = 0;
        activatedTargets = 0;
        
        SetGameState(GameState.Playing);
        
        // Unlock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Time.timeScale = 1f;
        
        ShowUI(gameHUD);
        HideUI(pauseMenuUI);
        HideUI(winScreenUI);
        HideUI(loseScreenUI);
    }
    
    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;
        
        SetGameState(GameState.Paused);
        Time.timeScale = 0f;
        
        // Show cursor for menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        ShowUI(pauseMenuUI);
    }
    
    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;
        
        SetGameState(GameState.Playing);
        Time.timeScale = 1f;
        
        // Lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        HideUI(pauseMenuUI);
    }
    
    public void WinGame()
    {
        if (currentState == GameState.Win) return;
        
        SetGameState(GameState.Win);
        Time.timeScale = 0f;
        
        // Play win sound
        if (winSound != null)
        {
            AudioSource.PlayClipAtPoint(winSound, Camera.main.transform.position);
        }
        
        // Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        ShowUI(winScreenUI);
        HideUI(gameHUD);
        
        OnLevelComplete?.Invoke();
        
        Debug.Log($"Level Complete! Time: {currentTime:F2}s, Score: {score}");
    }
    
    public void LoseGame(string reason = "")
    {
        if (currentState == GameState.Lose) return;
        
        SetGameState(GameState.Lose);
        Time.timeScale = 0f;
        
        // Play lose sound
        if (loseSound != null)
        {
            AudioSource.PlayClipAtPoint(loseSound, Camera.main.transform.position);
        }
        
        // Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        ShowUI(loseScreenUI);
        HideUI(gameHUD);
        
        OnLevelFailed?.Invoke();
        
        Debug.Log($"Game Over! Reason: {reason}");
    }
    
    private void SetGameState(GameState newState)
    {
        currentState = newState;
        OnGameStateChanged?.Invoke(currentState);
    }
    
    #endregion
    
    #region Target Handling
    
    private void HandleTargetActivated()
    {
        activatedTargets++;
        AddScore(100);
        
        // Reduced logging
        // Debug.Log($"Target activated! {activatedTargets}/{requiredTargets}");
        
        // Check win condition
        CheckWinCondition();
    }
    
    private void HandleTargetDeactivated()
    {
        activatedTargets = Mathf.Max(0, activatedTargets - 1);
        Debug.Log($"Target deactivated! {activatedTargets}/{requiredTargets}");
    }
    
    private void CheckWinCondition()
    {
        // LevelManager now handles door unlocking
        // This just tracks score and stats
        
        bool allActivated = false;
        
        if (requireAllTargets)
        {
            allActivated = activatedTargets >= requiredTargets;
        }
        else
        {
            allActivated = activatedTargets > 0;
        }
        
        // Reduced logging
        // if (allActivated)
        // {
        //     Debug.Log($"Targets activated: {activatedTargets}/{requiredTargets}");
        // }
    }
    
    #endregion
    
    #region Collectables & Score
    
    public void CollectItem(Collectable item)
    {
        collectablesCollected++;
        AddScore(item.PointValue);
        
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, item.transform.position);
        }
        
        OnCollectableCollected?.Invoke(collectablesCollected, totalCollectables);
        
        Debug.Log($"Collected item! {collectablesCollected}/{totalCollectables}");
    }
    
    public void AddScore(int points)
    {
        score += points;
        OnScoreChanged?.Invoke(score);
    }
    
    private void HandleEnergyChanged(float normalizedEnergy)
    {
        currentLaserEnergy = normalizedEnergy;
    }
    
    #endregion
    
    #region Scene Management
    
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            // No more levels - go to main menu or restart
            LoadMainMenu();
        }
    }
    
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0); // Assuming main menu is scene 0
    }
    
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    #endregion
    
    #region UI Helpers
    
    private void ShowUI(GameObject ui)
    {
        if (ui != null) ui.SetActive(true);
    }
    
    private void HideUI(GameObject ui)
    {
        if (ui != null) ui.SetActive(false);
    }
    
    #endregion
    
    #region Debug / GUI
    
    private Texture2D overlayTexture;
    private Texture2D buttonTexture;
    private Texture2D buttonHoverTexture;
    private Texture2D batteryBgTexture;
    private Texture2D batteryFillTexture;
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle hudStyle;
    private bool stylesInitialized = false;
    
    private void InitializeStyles()
    {
        if (stylesInitialized) return;
        
        // Create dark overlay texture
        overlayTexture = new Texture2D(1, 1);
        overlayTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.85f));
        overlayTexture.Apply();
        
        // Button normal texture
        buttonTexture = new Texture2D(1, 1);
        buttonTexture.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.2f, 0.9f));
        buttonTexture.Apply();
        
        // Button hover texture
        buttonHoverTexture = new Texture2D(1, 1);
        buttonHoverTexture.SetPixel(0, 0, new Color(0.1f, 0.5f, 0.8f, 1f));
        buttonHoverTexture.Apply();
        
        // Battery textures
        batteryBgTexture = new Texture2D(1, 1);
        batteryBgTexture.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.1f, 0.8f));
        batteryBgTexture.Apply();
        
        batteryFillTexture = new Texture2D(1, 1);
        batteryFillTexture.SetPixel(0, 0, Color.white);
        batteryFillTexture.Apply();
        
        // Title style
        titleStyle = new GUIStyle();
        titleStyle.fontSize = 64;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        
        // Subtitle style
        subtitleStyle = new GUIStyle();
        subtitleStyle.fontSize = 28;
        subtitleStyle.alignment = TextAnchor.MiddleCenter;
        subtitleStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        
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
        buttonStyle.active.background = buttonHoverTexture;
        buttonStyle.padding = new RectOffset(20, 20, 15, 15);
        buttonStyle.border = new RectOffset(4, 4, 4, 4);
        
        // HUD style
        hudStyle = new GUIStyle();
        hudStyle.fontSize = 18;
        hudStyle.fontStyle = FontStyle.Bold;
        hudStyle.normal.textColor = Color.white;
        
        stylesInitialized = true;
    }
    
    void OnGUI()
    {
        InitializeStyles();
        
        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;
        
        // HUD - always show when playing
        if (currentState == GameState.Playing)
        {
            DrawHUD();
        }
        else if (currentState == GameState.Paused)
        {
            DrawOverlay();
            DrawPauseMenu(centerX, centerY);
        }
        else if (currentState == GameState.Win)
        {
            DrawOverlay();
            DrawWinScreen(centerX, centerY);
        }
        else if (currentState == GameState.Lose)
        {
            DrawOverlay();
            DrawLoseScreen(centerX, centerY);
        }
    }
    
    private void DrawOverlay()
    {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlayTexture);
    }
    
    private void DrawHUD()
    {
        float x = 20;
        float y = 20;
        float lineHeight = 28;
        
        // Get level info from LevelManager - using clean getter methods
        string levelInfo = "Level 1";
        string targetInfo = $"{activatedTargets}/{requiredTargets}";
        
        if (LevelManager.Instance != null)
        {
            levelInfo = $"{LevelManager.Instance.GetLevelName()} #{LevelManager.Instance.GetLevelNumber()}";
            
            // Get target progress
            int completed = LevelManager.Instance.GetCompletedTargetCount();
            int total = LevelManager.Instance.GetTotalTargetCount();
            targetInfo = total > 0 ? $"{completed}/{total}" : "0/0";
        }
        
        // Background box for HUD
        Texture2D hudBg = new Texture2D(1, 1);
        hudBg.SetPixel(0, 0, new Color(0, 0, 0, 0.5f));
        hudBg.Apply();
        GUI.DrawTexture(new Rect(10, 10, 200, 130), hudBg);
        
        GUI.Label(new Rect(x, y, 180, 25), $"📍 {levelInfo}", hudStyle);
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, 180, 25), $"⏱ Time: {currentTime:F1}s", hudStyle);
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, 180, 25), $"⭐ Score: {score}", hudStyle);
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, 180, 25), $"🎯 Targets: {targetInfo}", hudStyle);
        y += lineHeight + 5;
        
        // Draw Battery/Power Bar
        DrawBatteryBar(x, y, 170, 18);
    }
    
    private void DrawBatteryBar(float x, float y, float width, float height)
    {
        // Label
        GUI.Label(new Rect(x, y, width, 20), "⚡ LASER POWER", hudStyle);
        y += 20;
        
        // Background
        GUI.DrawTexture(new Rect(x, y, width, height), batteryBgTexture);
        
        // Fill
        float fillWidth = (width - 4) * currentLaserEnergy;
        Color batteryColor = currentLaserEnergy > 0.25f ? new Color(0f, 0.8f, 1f, 1f) : Color.red;
        
        GUI.color = batteryColor;
        GUI.DrawTexture(new Rect(x + 2, y + 2, fillWidth, height - 4), batteryFillTexture);
        GUI.color = Color.white;
        
        // Percentage text
        GUIStyle percentStyle = new GUIStyle(hudStyle);
        percentStyle.alignment = TextAnchor.MiddleCenter;
        percentStyle.fontSize = 12;
        GUI.Label(new Rect(x, y, width, height), $"{(currentLaserEnergy * 100):F0}%", percentStyle);
    }
    
    private void DrawPauseMenu(float centerX, float centerY)
    {
        // Title
        titleStyle.normal.textColor = new Color(0.3f, 0.8f, 1f, 1f);
        GUI.Label(new Rect(centerX - 250, centerY - 180, 500, 80), "⏸ PAUSED", titleStyle);
        
        // Subtitle
        GUI.Label(new Rect(centerX - 200, centerY - 100, 400, 40), "Game is paused", subtitleStyle);
        
        // Buttons
        float buttonWidth = 200;
        float buttonHeight = 50;
        float buttonSpacing = 15;
        float startY = centerY - 20;
        
        if (GUI.Button(new Rect(centerX - buttonWidth/2, startY, buttonWidth, buttonHeight), "▶ RESUME", buttonStyle))
        {
            ResumeGame();
        }
        
        startY += buttonHeight + buttonSpacing;
        if (GUI.Button(new Rect(centerX - buttonWidth/2, startY, buttonWidth, buttonHeight), "🔄 RESTART", buttonStyle))
        {
            RestartLevel();
        }
        
        startY += buttonHeight + buttonSpacing;
        if (GUI.Button(new Rect(centerX - buttonWidth/2, startY, buttonWidth, buttonHeight), "🏠 MAIN MENU", buttonStyle))
        {
            LoadMainMenu();
        }
        
        // Footer hint
        GUIStyle hintStyle = new GUIStyle();
        hintStyle.fontSize = 16;
        hintStyle.alignment = TextAnchor.MiddleCenter;
        hintStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        GUI.Label(new Rect(centerX - 200, Screen.height - 60, 400, 30), "Press ESC or P to resume", hintStyle);
    }
    
    private void DrawWinScreen(float centerX, float centerY)
    {
        // Title
        titleStyle.normal.textColor = new Color(0.2f, 1f, 0.4f, 1f);
        GUI.Label(new Rect(centerX - 300, centerY - 200, 600, 80), "🏆 LEVEL COMPLETE!", titleStyle);
        
        // Stats
        GUIStyle statsStyle = new GUIStyle();
        statsStyle.fontSize = 32;
        statsStyle.alignment = TextAnchor.MiddleCenter;
        statsStyle.normal.textColor = Color.white;
        
        GUI.Label(new Rect(centerX - 200, centerY - 100, 400, 50), $"⏱ Time: {currentTime:F2}s", statsStyle);
        GUI.Label(new Rect(centerX - 200, centerY - 50, 400, 50), $"⭐ Score: {score}", statsStyle);
        
        // Buttons
        float buttonWidth = 180;
        float buttonHeight = 50;
        float buttonSpacing = 20;
        float startY = centerY + 30;
        
        if (GUI.Button(new Rect(centerX - buttonWidth - buttonSpacing/2, startY, buttonWidth, buttonHeight), "▶ NEXT LEVEL", buttonStyle))
        {
            LoadNextLevel();
        }
        
        if (GUI.Button(new Rect(centerX + buttonSpacing/2, startY, buttonWidth, buttonHeight), "🔄 RESTART", buttonStyle))
        {
            RestartLevel();
        }
        
        startY += buttonHeight + buttonSpacing;
        if (GUI.Button(new Rect(centerX - buttonWidth/2, startY, buttonWidth, buttonHeight), "🏠 MAIN MENU", buttonStyle))
        {
            LoadMainMenu();
        }
    }
    
    private void DrawLoseScreen(float centerX, float centerY)
    {
        // Title
        titleStyle.normal.textColor = new Color(1f, 0.3f, 0.3f, 1f);
        GUI.Label(new Rect(centerX - 250, centerY - 180, 500, 80), "💀 GAME OVER", titleStyle);
        
        // Subtitle
        subtitleStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        GUI.Label(new Rect(centerX - 200, centerY - 100, 400, 40), "Better luck next time!", subtitleStyle);
        
        // Stats
        GUIStyle statsStyle = new GUIStyle();
        statsStyle.fontSize = 24;
        statsStyle.alignment = TextAnchor.MiddleCenter;
        statsStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        
        GUI.Label(new Rect(centerX - 200, centerY - 40, 400, 35), $"Score: {score}", statsStyle);
        
        // Buttons
        float buttonWidth = 180;
        float buttonHeight = 50;
        float buttonSpacing = 20;
        float startY = centerY + 20;
        
        if (GUI.Button(new Rect(centerX - buttonWidth - buttonSpacing/2, startY, buttonWidth, buttonHeight), "🔄 TRY AGAIN", buttonStyle))
        {
            RestartLevel();
        }
        
        if (GUI.Button(new Rect(centerX + buttonSpacing/2, startY, buttonWidth, buttonHeight), "🏠 MAIN MENU", buttonStyle))
        {
            LoadMainMenu();
        }
    }
    
    #endregion
}
