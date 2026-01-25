using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Manages game settings including audio and screen resolution
/// Settings are persisted using PlayerPrefs
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }
    
    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterVolumeParameter = "MasterVolume";
    
    // Settings keys
    private const string AUDIO_ENABLED_KEY = "AudioEnabled";
    private const string RESOLUTION_WIDTH_KEY = "ResolutionWidth";
    private const string RESOLUTION_HEIGHT_KEY = "ResolutionHeight";
    private const string FULLSCREEN_KEY = "Fullscreen";
    
    // Current settings
    private bool audioEnabled = true;
    private int currentWidth = 1920;
    private int currentHeight = 1080;
    private bool isFullscreen = true;
    
    // Properties
    public bool AudioEnabled => audioEnabled;
    public int ResolutionWidth => currentWidth;
    public int ResolutionHeight => currentHeight;
    public bool IsFullscreen => isFullscreen;
    
    // Events
    public static event System.Action<bool> OnAudioToggled;
    public static event System.Action<int, int, bool> OnResolutionChanged;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        ApplySettings();
    }
    
    /// <summary>
    /// Load settings from PlayerPrefs
    /// </summary>
    private void LoadSettings()
    {
        // Load audio setting (default: enabled)
        audioEnabled = PlayerPrefs.GetInt(AUDIO_ENABLED_KEY, 1) == 1;
        
        // Load resolution (default: current screen resolution)
        currentWidth = PlayerPrefs.GetInt(RESOLUTION_WIDTH_KEY, Screen.currentResolution.width);
        currentHeight = PlayerPrefs.GetInt(RESOLUTION_HEIGHT_KEY, Screen.currentResolution.height);
        isFullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, Screen.fullScreen ? 1 : 0) == 1;
        
        // Clamp to valid resolutions
        Resolution[] resolutions = Screen.resolutions;
        if (resolutions.Length > 0)
        {
            bool found = false;
            foreach (var res in resolutions)
            {
                if (res.width == currentWidth && res.height == currentHeight)
                {
                    found = true;
                    break;
                }
            }
            
            if (!found && resolutions.Length > 0)
            {
                // Use highest resolution as default
                Resolution highest = resolutions[resolutions.Length - 1];
                currentWidth = highest.width;
                currentHeight = highest.height;
            }
        }
    }
    
    /// <summary>
    /// Apply all settings
    /// </summary>
    private void ApplySettings()
    {
        ApplyAudioSettings();
        ApplyResolutionSettings();
    }
    
    /// <summary>
    /// Apply audio settings
    /// </summary>
    private void ApplyAudioSettings()
    {
        if (audioMixer != null)
        {
            // Set volume: 0 = max volume, -80 = muted
            float volume = audioEnabled ? 0f : -80f;
            audioMixer.SetFloat(masterVolumeParameter, volume);
        }
        else
        {
            // Fallback: use AudioListener
            AudioListener.volume = audioEnabled ? 1f : 0f;
        }
    }
    
    /// <summary>
    /// Apply resolution settings
    /// </summary>
    private void ApplyResolutionSettings()
    {
        Screen.SetResolution(currentWidth, currentHeight, isFullscreen);
    }
    
    /// <summary>
    /// Toggle audio on/off
    /// </summary>
    public void ToggleAudio()
    {
        audioEnabled = !audioEnabled;
        PlayerPrefs.SetInt(AUDIO_ENABLED_KEY, audioEnabled ? 1 : 0);
        PlayerPrefs.Save();
        
        ApplyAudioSettings();
        OnAudioToggled?.Invoke(audioEnabled);
        
        Debug.Log($"[SettingsManager] Audio {(audioEnabled ? "Enabled" : "Disabled")}");
    }
    
    /// <summary>
    /// Set audio enabled state
    /// </summary>
    public void SetAudioEnabled(bool enabled)
    {
        if (audioEnabled != enabled)
        {
            audioEnabled = enabled;
            PlayerPrefs.SetInt(AUDIO_ENABLED_KEY, audioEnabled ? 1 : 0);
            PlayerPrefs.Save();
            
            ApplyAudioSettings();
            OnAudioToggled?.Invoke(audioEnabled);
        }
    }
    
    /// <summary>
    /// Set screen resolution
    /// </summary>
    public void SetResolution(int width, int height, bool fullscreen)
    {
        currentWidth = width;
        currentHeight = height;
        isFullscreen = fullscreen;
        
        PlayerPrefs.SetInt(RESOLUTION_WIDTH_KEY, width);
        PlayerPrefs.SetInt(RESOLUTION_HEIGHT_KEY, height);
        PlayerPrefs.SetInt(FULLSCREEN_KEY, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
        
        ApplyResolutionSettings();
        OnResolutionChanged?.Invoke(width, height, fullscreen);
        
        Debug.Log($"[SettingsManager] Resolution set to {width}x{height}, Fullscreen: {fullscreen}");
    }
    
    /// <summary>
    /// Get available screen resolutions
    /// </summary>
    public Resolution[] GetAvailableResolutions()
    {
        return Screen.resolutions;
    }
    
    /// <summary>
    /// Get current resolution index from available resolutions
    /// </summary>
    public int GetCurrentResolutionIndex()
    {
        Resolution[] resolutions = Screen.resolutions;
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == currentWidth && 
                resolutions[i].height == currentHeight)
            {
                return i;
            }
        }
        return resolutions.Length > 0 ? resolutions.Length - 1 : 0;
    }
}
