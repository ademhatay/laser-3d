using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private int gameSceneIndex = 1;
    [SerializeField] private bool useSceneName = true;
    
    [Header("UI References")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button levelSelectButton;
    [SerializeField] private Button quitButton;
    
    void Start()
    {
        // Ensure cursor is visible
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
        
        // Setup button listeners
        if (playButton != null)
            playButton.onClick.AddListener(PlayGame);
        
        if (levelSelectButton != null)
            levelSelectButton.onClick.AddListener(LevelSelect);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }
    
    public void PlayGame()
    {
        Debug.Log("Starting game...");
        if (useSceneName)
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            SceneManager.LoadScene(gameSceneIndex);
        }
    }
    
    public void LevelSelect()
    {
        Debug.Log("Level Select - Coming Soon!");
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
}
