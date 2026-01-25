using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;
    
    [Header("Scene Settings")]
    [SerializeField] private int firstLevelBuildIndex = 1;
    
    private VisualElement root;
    private Button playButton;
    private Button levelsButton;
    private Button settingsButton;
    private Button quitButton;
    
    // Settings Panel
    private VisualElement settingsPanel;
    private Button settingsCloseButton;
    private Toggle audioToggle;
    private DropdownField resolutionDropdown;
    private Toggle fullscreenToggle;
    private Button resetDataButton;
    
    private bool isSettingsOpen = false;
    
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
            Debug.LogError("[MainMenuController] UIDocument component not found!");
            return;
        }
        
        // Get root visual element
        root = uiDocument.rootVisualElement;
        
        // Get buttons
        playButton = root.Q<Button>("playBtn");
        levelsButton = root.Q<Button>("levelsBtn");
        settingsButton = root.Q<Button>("settingsBtn");
        quitButton = root.Q<Button>("quitBtn");
        
        // Get settings panel elements
        settingsPanel = root.Q<VisualElement>("settingsPanel");
        settingsCloseButton = root.Q<Button>("settingsCloseBtn");
        audioToggle = root.Q<Toggle>("audioToggle");
        resolutionDropdown = root.Q<DropdownField>("resolutionDropdown");
        fullscreenToggle = root.Q<Toggle>("fullscreenToggle");
        resetDataButton = root.Q<Button>("resetDataBtn");
        
        // Setup button listeners
        if (playButton != null)
            playButton.clicked += OnPlayClicked;
        
        if (levelsButton != null)
            levelsButton.clicked += OnLevelsClicked;
        
        if (settingsButton != null)
            settingsButton.clicked += OnSettingsClicked;
        
        if (quitButton != null)
            quitButton.clicked += OnQuitClicked;
        
        // Setup settings panel
        InitializeSettingsPanel();
    }
    
    private void InitializeSettingsPanel()
    {
        // Ensure SettingsManager exists
        if (SettingsManager.Instance == null)
        {
            GameObject settingsObj = new GameObject("SettingsManager");
            settingsObj.AddComponent<SettingsManager>();
        }
        
        // Setup settings close button
        if (settingsCloseButton != null)
        {
            settingsCloseButton.clicked += CloseSettings;
        }
        
        // Setup audio toggle
        if (audioToggle != null && SettingsManager.Instance != null)
        {
            audioToggle.value = SettingsManager.Instance.AudioEnabled;
            HideToggleCheckmarkIcon(audioToggle);
            SetupToggleAnimation(audioToggle);
            audioToggle.RegisterValueChangedCallback(OnAudioToggleChanged);
        }
        
        // Setup resolution dropdown
        if (resolutionDropdown != null)
        {
            SetupResolutionDropdown();
        }
        
        // Setup fullscreen toggle
        if (fullscreenToggle != null && SettingsManager.Instance != null)
        {
            fullscreenToggle.value = SettingsManager.Instance.IsFullscreen;
            HideToggleCheckmarkIcon(fullscreenToggle);
            SetupToggleAnimation(fullscreenToggle);
            fullscreenToggle.RegisterValueChangedCallback(OnFullscreenToggleChanged);
        }
        
        // Setup reset data button
        if (resetDataButton != null)
        {
            resetDataButton.clicked += OnResetDataClicked;
        }
    }
    
    private void SetupResolutionDropdown()
    {
        if (SettingsManager.Instance == null) return;
        
        Resolution[] resolutions = SettingsManager.Instance.GetAvailableResolutions();
        
        // Filter unique resolutions (remove duplicates)
        List<string> resolutionOptions = new List<string>();
        HashSet<string> seen = new HashSet<string>();
        
        foreach (var res in resolutions)
        {
            string resString = $"{res.width} x {res.height}";
            if (!seen.Contains(resString))
            {
                seen.Add(resString);
                resolutionOptions.Add(resString);
            }
        }
        
        // Set choices
        resolutionDropdown.choices = resolutionOptions;
        
        // Set current resolution
        int currentIndex = SettingsManager.Instance.GetCurrentResolutionIndex();
        if (currentIndex >= 0 && currentIndex < resolutions.Length)
        {
            string currentRes = $"{SettingsManager.Instance.ResolutionWidth} x {SettingsManager.Instance.ResolutionHeight}";
            resolutionDropdown.value = currentRes;
        }
        
        // Register callback
        resolutionDropdown.RegisterValueChangedCallback(OnResolutionChanged);
    }
    
    private void HideToggleCheckmarkIcon(Toggle toggle)
    {
        // Get the checkmark (knob) element
        VisualElement checkmark = toggle.Q<VisualElement>(className: "unity-toggle__checkmark");
        if (checkmark != null)
        {
            // Remove background image if any
            checkmark.style.backgroundImage = null;
            checkmark.style.unityBackgroundImageTintColor = new StyleColor(Color.clear);
            
            // Hide all child elements inside checkmark (icons, images, etc.)
            foreach (var child in checkmark.Children())
            {
                child.style.display = DisplayStyle.None;
                child.style.visibility = Visibility.Hidden;
                child.style.opacity = 0f;
            }
            
            // Also query for any images or other elements
            var allChildren = checkmark.Query<VisualElement>().ToList();
            foreach (var child in allChildren)
            {
                if (child != checkmark)
                {
                    child.style.display = DisplayStyle.None;
                    child.style.visibility = Visibility.Hidden;
                    child.style.opacity = 0f;
                }
            }
            
            // Ensure it's a pure white circle
            checkmark.style.backgroundColor = Color.white;
            // Set all border radius values to 50% for perfect circle
            float radius = 13.5f; // Half of 27px width/height
            checkmark.style.borderTopLeftRadius = radius;
            checkmark.style.borderTopRightRadius = radius;
            checkmark.style.borderBottomLeftRadius = radius;
            checkmark.style.borderBottomRightRadius = radius;
        }
    }
    
    private void SetupToggleAnimation(Toggle toggle)
    {
        // Get the checkmark (knob) element
        VisualElement checkmark = toggle.Q<VisualElement>(className: "unity-toggle__checkmark");
        if (checkmark != null)
        {
            // Set initial position based on current value
            UpdateTogglePosition(toggle, toggle.value);
        }
    }
    
    private void AnimateToggle(Toggle toggle, bool targetValue)
    {
        VisualElement checkmark = toggle.Q<VisualElement>(className: "unity-toggle__checkmark");
        if (checkmark == null) return;
        
        StartCoroutine(AnimateToggleCoroutine(toggle, checkmark, targetValue));
    }
    
    private IEnumerator AnimateToggleCoroutine(Toggle toggle, VisualElement checkmark, bool targetValue)
    {
        float duration = 0.3f; // Animation duration in seconds
        float elapsed = 0f;
        
        // Start positions
        float startMarginLeft = checkmark.resolvedStyle.marginLeft;
        float targetMarginLeft = targetValue ? 22f : 2f;
        
        // Start colors
        Color startBgColor = toggle.resolvedStyle.backgroundColor;
        Color targetBgColor = targetValue ? new Color(0.2f, 0.78f, 0.35f, 1f) : new Color(0.56f, 0.56f, 0.58f, 1f);
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Smooth easing (ease-in-out)
            t = t * t * (3f - 2f * t);
            
            // Animate margin (knob position)
            float currentMargin = Mathf.Lerp(startMarginLeft, targetMarginLeft, t);
            checkmark.style.marginLeft = currentMargin;
            checkmark.style.marginRight = targetValue ? 2f : 0f;
            
            // Animate background color
            Color currentColor = Color.Lerp(startBgColor, targetBgColor, t);
            toggle.style.backgroundColor = currentColor;
            
            yield return null;
        }
        
        // Ensure final values
        checkmark.style.marginLeft = targetMarginLeft;
        checkmark.style.marginRight = targetValue ? 2f : 0f;
        toggle.style.backgroundColor = targetBgColor;
    }
    
    private void UpdateTogglePosition(Toggle toggle, bool value)
    {
        VisualElement checkmark = toggle.Q<VisualElement>(className: "unity-toggle__checkmark");
        if (checkmark != null)
        {
            checkmark.style.marginLeft = value ? 22f : 2f;
            checkmark.style.marginRight = value ? 2f : 0f;
        }
    }
    
    private void OnAudioToggleChanged(ChangeEvent<bool> evt)
    {
        if (audioToggle != null)
        {
            AnimateToggle(audioToggle, evt.newValue);
        }
        
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetAudioEnabled(evt.newValue);
        }
    }
    
    private void OnResolutionChanged(ChangeEvent<string> evt)
    {
        if (SettingsManager.Instance == null) return;
        
        // Parse resolution string "1920 x 1080"
        string[] parts = evt.newValue.Split('x');
        if (parts.Length == 2)
        {
            if (int.TryParse(parts[0].Trim(), out int width) && 
                int.TryParse(parts[1].Trim(), out int height))
            {
                bool fullscreen = SettingsManager.Instance.IsFullscreen;
                SettingsManager.Instance.SetResolution(width, height, fullscreen);
            }
        }
    }
    
    private void OnFullscreenToggleChanged(ChangeEvent<bool> evt)
    {
        if (fullscreenToggle != null)
        {
            AnimateToggle(fullscreenToggle, evt.newValue);
        }
        
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetResolution(
                SettingsManager.Instance.ResolutionWidth,
                SettingsManager.Instance.ResolutionHeight,
                evt.newValue
            );
        }
    }
    
    private void OnPlayClicked()
    {
        Debug.Log("[MainMenuController] Play button clicked - Loading first level");
        // Load first level (Level_01)
        SceneManager.LoadScene(firstLevelBuildIndex);
    }
    
    private void OnLevelsClicked()
    {
        Debug.Log("[MainMenuController] Levels button clicked - Loading LevelSelect scene");
        SceneManager.LoadScene("LevelSelect");
    }
    
    private void OnSettingsClicked()
    {
        Debug.Log("[MainMenuController] Settings button clicked");
        OpenSettings();
    }
    
    private void OpenSettings()
    {
        if (settingsPanel != null)
        {
            isSettingsOpen = true;
            settingsPanel.style.display = DisplayStyle.Flex;
            
            // Refresh settings values
            if (SettingsManager.Instance != null)
            {
                if (audioToggle != null)
                {
                    audioToggle.value = SettingsManager.Instance.AudioEnabled;
                    HideToggleCheckmarkIcon(audioToggle);
                    UpdateTogglePosition(audioToggle, audioToggle.value);
                }
                
                if (fullscreenToggle != null)
                {
                    fullscreenToggle.value = SettingsManager.Instance.IsFullscreen;
                    HideToggleCheckmarkIcon(fullscreenToggle);
                    UpdateTogglePosition(fullscreenToggle, fullscreenToggle.value);
                }
                
                // Refresh resolution dropdown
                SetupResolutionDropdown();
            }
        }
    }
    
    private void CloseSettings()
    {
        if (settingsPanel != null)
        {
            isSettingsOpen = false;
            settingsPanel.style.display = DisplayStyle.None;
        }
    }
    
    private void OnQuitClicked()
    {
        Debug.Log("[MainMenuController] Quit button clicked");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    private void OnResetDataClicked()
    {
        Debug.Log("[MainMenuController] Reset data button clicked");
        ResetAllData();
    }
    
    private void ResetAllData()
    {
        // Save current settings before deleting (to restore them after)
        bool audioEnabled = PlayerPrefs.GetInt("AudioEnabled", 1) == 1;
        int resolutionWidth = PlayerPrefs.GetInt("ResolutionWidth", Screen.currentResolution.width);
        int resolutionHeight = PlayerPrefs.GetInt("ResolutionHeight", Screen.currentResolution.height);
        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        
        // Clear all level progress data (check up to 50 levels to be safe)
        for (int i = 1; i <= 50; i++)
        {
            PlayerPrefs.DeleteKey($"Level_{i}_Completed");
            PlayerPrefs.DeleteKey($"Level_{i}_Stars");
            PlayerPrefs.DeleteKey($"Level_{i}_BestTime");
        }
        
        // Clear total stars and current level
        PlayerPrefs.DeleteKey("TotalStars");
        PlayerPrefs.DeleteKey("CurrentLevelNumber");
        
        // Clear level progress save data
        PlayerPrefs.DeleteKey("LaserGameProgress");
        
        // Restore settings (keep user's audio/display preferences)
        PlayerPrefs.SetInt("AudioEnabled", audioEnabled ? 1 : 0);
        PlayerPrefs.SetInt("ResolutionWidth", resolutionWidth);
        PlayerPrefs.SetInt("ResolutionHeight", resolutionHeight);
        PlayerPrefs.SetInt("Fullscreen", fullscreen ? 1 : 0);
        
        // Save changes
        PlayerPrefs.Save();
        
        // Reset LevelProgressManager if it exists
        if (LevelProgressManager.Instance != null)
        {
            LevelProgressManager.Instance.ResetProgress();
        }
        
        Debug.Log("[MainMenuController] All level progress and user data has been reset! (Settings preserved)");
        
        // Refresh the settings panel
        if (isSettingsOpen)
        {
            CloseSettings();
            OpenSettings(); // Refresh
        }
    }
    
    void Update()
    {
        // Close settings with Escape key
        if (isSettingsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseSettings();
        }
    }
    
    void OnDestroy()
    {
        // Clean up event listeners
        if (playButton != null)
            playButton.clicked -= OnPlayClicked;
        
        if (levelsButton != null)
            levelsButton.clicked -= OnLevelsClicked;
        
        if (settingsButton != null)
            settingsButton.clicked -= OnSettingsClicked;
        
        if (quitButton != null)
            quitButton.clicked -= OnQuitClicked;
        
        if (settingsCloseButton != null)
            settingsCloseButton.clicked -= CloseSettings;
        
        if (audioToggle != null)
            audioToggle.UnregisterValueChangedCallback(OnAudioToggleChanged);
        
        if (resolutionDropdown != null)
            resolutionDropdown.UnregisterValueChangedCallback(OnResolutionChanged);
        
        if (fullscreenToggle != null)
            fullscreenToggle.UnregisterValueChangedCallback(OnFullscreenToggleChanged);
        
        if (resetDataButton != null)
            resetDataButton.clicked -= OnResetDataClicked;
    }
}
