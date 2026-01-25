using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Collections;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;
    
    private VisualElement root;
    private Button resumeButton;
    private Button restartButton;
    private Button mainMenuButton;
    
    private bool isInitialized = false;
    private bool isInitializing = false;
    
    void Awake()
    {
        // Initialize UI in Awake
        InitializeUI();
    }
    
    void Start()
    {
        // Ensure UI is hidden at start
        if (root != null)
        {
            root.style.display = DisplayStyle.None;
        }
    }
    
    private void InitializeUI()
    {
        if (isInitialized || isInitializing) return;
        
        isInitializing = true;
        
        // Get UI Document if not assigned
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
        
        if (uiDocument == null)
        {
            Debug.LogError("[PauseMenuController] UIDocument component not found!");
            isInitializing = false;
            return;
        }
        
        // Check if Visual Tree Asset is assigned
        if (uiDocument.visualTreeAsset == null)
        {
            Debug.LogError("[PauseMenuController] UIDocument Visual Tree Asset is not assigned! Please assign PauseMenu.uxml in the Inspector.");
            isInitializing = false;
            return;
        }
        
        // Get root visual element - query for the element with name "root"
        VisualElement documentRoot = uiDocument.rootVisualElement;
        
        if (documentRoot == null)
        {
            Debug.LogError("[PauseMenuController] Root visual element not found! UIDocument may not be initialized yet.");
            isInitializing = false;
            return;
        }
        
        // Get the pause menu root element (the one with name="root" in UXML)
        root = documentRoot.Q<VisualElement>("root");
        
        if (root == null)
        {
            Debug.LogError("[PauseMenuController] Pause menu root element (name='root') not found in UXML! Make sure the UXML has a VisualElement with name='root'.");
            isInitializing = false;
            return;
        }
        
        // Get buttons
        resumeButton = root.Q<Button>("resumeBtn");
        restartButton = root.Q<Button>("restartBtn");
        mainMenuButton = root.Q<Button>("mainMenuBtn");
        
        if (resumeButton == null || restartButton == null || mainMenuButton == null)
        {
            Debug.LogWarning("[PauseMenuController] Some buttons not found! Resume: " + (resumeButton != null) + 
                           ", Restart: " + (restartButton != null) + ", MainMenu: " + (mainMenuButton != null));
        }
        
        // Setup button listeners
        if (resumeButton != null)
            resumeButton.clicked += OnResumeClicked;
        
        if (restartButton != null)
            restartButton.clicked += OnRestartClicked;
        
        if (mainMenuButton != null)
            mainMenuButton.clicked += OnMainMenuClicked;
        
        // Hide by default (set directly, don't call SetVisible to avoid recursion)
        root.style.display = DisplayStyle.None;
        
        isInitialized = true;
        isInitializing = false;
        
        Debug.Log("[PauseMenuController] UI initialized successfully. Root display: " + root.style.display);
    }
    
    void OnEnable()
    {
        // Initialize if needed
        if (!isInitialized)
        {
            InitializeUI();
        }
        // Always hide when enabled
        if (root != null)
        {
            root.style.display = DisplayStyle.None;
        }
    }
    
    public void SetVisible(bool visible)
    {
        // Initialize if needed (but only once)
        if (!isInitialized && !isInitializing)
        {
            InitializeUI();
        }
        
        // Wait a frame if still initializing
        if (isInitializing)
        {
            StartCoroutine(DelayedSetVisible(visible));
            return;
        }
        
        if (root != null)
        {
            if (visible)
            {
                root.style.display = DisplayStyle.Flex;
                root.style.visibility = Visibility.Visible;
                root.style.opacity = 1f;
                
                // Force update
                root.MarkDirtyRepaint();
                
                Debug.Log($"[PauseMenuController] SetVisible(True) - Display: {root.style.display}, Visibility: {root.style.visibility}, Opacity: {root.style.opacity.value}, Root name: {root.name}");
            }
            else
            {
                root.style.display = DisplayStyle.None;
                Debug.Log($"[PauseMenuController] SetVisible(False) - Display: {root.style.display}");
            }
        }
        else
        {
            Debug.LogError("[PauseMenuController] Root visual element is null! Cannot show/hide pause menu. Make sure UIDocument has Source Asset assigned.");
        }
    }
    
    private System.Collections.IEnumerator DelayedSetVisible(bool visible)
    {
        yield return null; // Wait one frame
        SetVisible(visible);
    }
    
    private void OnResumeClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
        else
        {
            Debug.LogWarning("[PauseMenuController] GameManager.Instance not found!");
            // Fallback: just resume time
            Time.timeScale = 1f;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
            SetVisible(false);
        }
    }
    
    private void OnRestartClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartLevel();
        }
        else
        {
            Debug.LogWarning("[PauseMenuController] GameManager.Instance not found!");
            // Fallback: reload current scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
    
    private void OnMainMenuClicked()
    {
        // Resume time before loading menu
        Time.timeScale = 1f;
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadMainMenu();
        }
        else
        {
            Debug.LogWarning("[PauseMenuController] GameManager.Instance not found!");
            // Fallback: load main menu by name
            SceneManager.LoadScene("MainMenu");
        }
    }
    
    void OnDestroy()
    {
        // Clean up event listeners
        if (resumeButton != null)
            resumeButton.clicked -= OnResumeClicked;
        
        if (restartButton != null)
            restartButton.clicked -= OnRestartClicked;
        
        if (mainMenuButton != null)
            mainMenuButton.clicked -= OnMainMenuClicked;
    }
}
