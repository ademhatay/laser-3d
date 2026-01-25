using UnityEngine;

/// <summary>
/// Interactable door that can be opened/closed by the player
/// Works with PlayerInteraction system
/// Supports lock mechanism with dynamic unlock conditions
/// </summary>
public class InteractableDoor : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float closedAngle = 0f;
    [SerializeField] private float rotationDuration = 0.5f;
    [SerializeField] private Transform doorPivot; // The actual door mesh that rotates
    
    [Header("Lock Settings")]
    [SerializeField] private bool startsLocked = true;
    [SerializeField] private Color lockedHighlightColor = new Color(1f, 0.3f, 0.3f, 1f); // Red when locked
    [SerializeField] private Color unlockedHighlightColor = new Color(0.3f, 1f, 0.5f, 1f); // Green when unlocked
    [SerializeField] private AudioClip unlockSound;
    [SerializeField] private AudioClip lockedSound; // Sound when trying to open locked door
    
    [Header("Unlock Condition")]
    [SerializeField] private UnlockCondition unlockCondition = UnlockCondition.AllTargetsActivated;
    
    public enum UnlockCondition
    {
        None,                    // Always unlocked
        AllTargetsActivated,     // Unlock when all laser targets are hit
        AnyTargetActivated,      // Unlock when any target is hit
        Manual                   // Unlock via script/event
    }
    
    [Header("Audio")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    
    [Header("Selection Visual")]
    [SerializeField] private Color highlightColor = new Color(0.3f, 0.8f, 1f, 1f);
    [SerializeField] private float borderThickness = 0.03f;
    
    [Header("Win Condition")]
    [SerializeField] private string playerTag = "Player";
    
    // State
    private bool isOpen = false;
    private bool isLocked = true;
    private bool isAnimating = false;
    private float animationTimer = 0f;
    private float startAngle;
    private float targetAngle;
    
    // Selection visuals
    private bool isSelected = false;
    private GameObject[] borderParts;
    private Material borderMaterial;
    private AudioSource audioSource;
    
    // Events
    public static event System.Action<InteractableDoor> OnDoorUnlocked;
    public static event System.Action<InteractableDoor> OnDoorLocked;
    
    public bool IsOpen => isOpen;
    public bool IsLocked => isLocked;
    public bool IsSelected => isSelected;
    public string LockStatusText => isLocked ? "🔒 Locked" : "🔓 Unlocked";
    
    void Start()
    {
        // Get or create audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.playOnAwake = false;
        }
        
        // If no pivot assigned, use this transform
        if (doorPivot == null)
        {
            doorPivot = transform;
        }
        
        // Initialize lock state
        isLocked = startsLocked;
        if (unlockCondition == UnlockCondition.None)
        {
            isLocked = false;
        }
        
        // Subscribe to target events for unlock conditions
        // DISABLED: LevelManager now handles door unlocking
        /*
        if (unlockCondition == UnlockCondition.AllTargetsActivated || 
            unlockCondition == UnlockCondition.AnyTargetActivated)
        {
            LaserTarget.OnTargetActivated += CheckUnlockCondition;
            LaserTarget.OnTargetDeactivated += CheckLockCondition;
        }
        */
        
        // Create selection border
        CreateSelectionBorder();
        SetBorderActive(false);
        UpdateBorderColor();
    }
    
    void OnDestroy()
    {
        LaserTarget.OnTargetActivated -= CheckUnlockCondition;
        LaserTarget.OnTargetDeactivated -= CheckLockCondition;
    }
    
    void Update()
    {
        // Handle door animation
        if (isAnimating)
        {
            animationTimer += Time.deltaTime;
            float t = Mathf.Clamp01(animationTimer / rotationDuration);
            
            // Smooth step easing
            t = t * t * (3f - 2f * t);
            
            float currentAngle = Mathf.LerpAngle(startAngle, targetAngle, t);
            
            // Apply rotation (rotate around Y axis for typical door)
            Vector3 euler = doorPivot.localEulerAngles;
            euler.y = currentAngle;
            doorPivot.localEulerAngles = euler;
            
            if (t >= 1f)
            {
                // Snap to target
                euler.y = targetAngle;
                doorPivot.localEulerAngles = euler;
                isAnimating = false;
                animationTimer = 0f;
            }
        }
    }
    
    /// <summary>
    /// Toggle the door open/closed state
    /// Returns false if door is locked
    /// </summary>
    public bool Toggle()
    {
        if (isAnimating) return false;
        
        // Check if locked
        if (isLocked)
        {
            PlaySound(lockedSound);
            return false;
        }
        
        if (isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
        return true;
    }
    
    /// <summary>
    /// Open the door
    /// </summary>
    public void Open()
    {
        // Don't do anything if already open and not animating
        if (isOpen && !isAnimating) return;
        
        // Don't restart if already moving to open
        if (isAnimating && targetAngle == openAngle) return;
        
        startAngle = doorPivot.localEulerAngles.y;
        targetAngle = openAngle;
        isAnimating = true;
        isOpen = true;
        animationTimer = 0f;
        
        PlaySound(openSound);
    }
    
    /// <summary>
    /// Close the door
    /// </summary>
    public void Close()
    {
        // Don't do anything if already closed and not animating
        if (!isOpen && !isAnimating) return;
        
        // Don't restart if already moving to close
        if (isAnimating && targetAngle == closedAngle) return;
        
        startAngle = doorPivot.localEulerAngles.y;
        targetAngle = closedAngle;
        isAnimating = true;
        isOpen = false;
        animationTimer = 0f;
        
        PlaySound(closeSound);
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    #region Lock System
    
    /// <summary>
    /// Unlock the door
    /// </summary>
    public void Unlock()
    {
        if (!isLocked) return;
        
        isLocked = false;
        
        // Reset animating flag to allow Open to work if it was in the middle of closing
        isAnimating = false;
        
        // Auto-open
        Open();
        
        PlaySound(unlockSound);
        UpdateBorderColor();
        OnDoorUnlocked?.Invoke(this);
        
        Debug.Log($"Door '{gameObject.name}' unlocked and opened!");
    }
    
    /// <summary>
    /// Lock the door
    /// </summary>
    public void Lock()
    {
        if (isLocked) return;
        
        isLocked = true;
        
        // Auto-close if open
        if (isOpen)
        {
            // Reset animating flag to allow closing if it was in the middle of opening
            isAnimating = false; 
            Close();
        }
        
        UpdateBorderColor();
        OnDoorLocked?.Invoke(this);
        
        Debug.Log($"Door '{gameObject.name}' locked and closed!");
    }
    
    /// <summary>
    /// Check unlock condition when target is activated
    /// </summary>
    private void CheckUnlockCondition()
    {
        if (!isLocked) return;
        
        bool shouldUnlock = false;
        
        switch (unlockCondition)
        {
            case UnlockCondition.AnyTargetActivated:
                shouldUnlock = true;
                break;
                
            case UnlockCondition.AllTargetsActivated:
                // Find all targets and check if all are activated
                LaserTarget[] allTargets = FindObjectsByType<LaserTarget>(FindObjectsSortMode.None);
                if (allTargets.Length > 0)
                {
                    shouldUnlock = true;
                    foreach (var target in allTargets)
                    {
                        if (!target.IsActivated)
                        {
                            shouldUnlock = false;
                            break;
                        }
                    }
                }
                break;
        }
        
        if (shouldUnlock)
        {
            Unlock();
        }
    }
    
    /// <summary>
    /// Check if door should be re-locked when target is deactivated
    /// </summary>
    private void CheckLockCondition()
    {
        if (isLocked) return;
        if (unlockCondition == UnlockCondition.None || unlockCondition == UnlockCondition.Manual) return;
        
        bool shouldLock = false;
        
        switch (unlockCondition)
        {
            case UnlockCondition.AnyTargetActivated:
                // Check if any target is still active
                LaserTarget[] allTargets = FindObjectsByType<LaserTarget>(FindObjectsSortMode.None);
                shouldLock = true;
                foreach (var target in allTargets)
                {
                    if (target.IsActivated)
                    {
                        shouldLock = false;
                        break;
                    }
                }
                break;
                
            case UnlockCondition.AllTargetsActivated:
                // Lock if any target is deactivated
                shouldLock = true; // Will be locked since this is called on deactivation
                break;
        }
        
        if (shouldLock)
        {
            Lock();
        }
    }
    
    /// <summary>
    /// Update border color based on lock state
    /// </summary>
    private void UpdateBorderColor()
    {
        Color targetColor = isLocked ? lockedHighlightColor : unlockedHighlightColor;
        
        if (borderMaterial != null)
        {
            borderMaterial.color = targetColor;
            if (borderMaterial.HasProperty("_BaseColor"))
            {
                borderMaterial.SetColor("_BaseColor", targetColor);
            }
            if (borderMaterial.HasProperty("_EmissionColor"))
            {
                borderMaterial.SetColor("_EmissionColor", targetColor * 2f);
            }
        }
    }

    /// <summary>
    /// Trigger win if player enters the open door
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (isOpen && other.CompareTag(playerTag))
        {
            Debug.Log("Player reached the goal! Winning game...");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.WinGame();
            }
        }
    }
    
    #endregion
    
    /// <summary>
    /// Called when player looks at the door
    /// </summary>
    public void Select()
    {
        isSelected = true;
        SetBorderActive(true);
    }
    
    /// <summary>
    /// Called when player looks away from the door
    /// </summary>
    public void Deselect()
    {
        isSelected = false;
        SetBorderActive(false);
    }
    
    private void SetBorderActive(bool active)
    {
        if (borderParts != null)
        {
            foreach (var part in borderParts)
            {
                if (part != null)
                    part.SetActive(active);
            }
        }
    }
    
    private void CreateSelectionBorder()
    {
        // Create emissive material for border
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        
        borderMaterial = new Material(shader);
        borderMaterial.color = highlightColor;
        borderMaterial.EnableKeyword("_EMISSION");
        borderMaterial.SetColor("_EmissionColor", highlightColor * 2f);
        
        // Try to set URP emission
        if (borderMaterial.HasProperty("_BaseColor"))
        {
            borderMaterial.SetColor("_BaseColor", highlightColor);
        }
        if (borderMaterial.HasProperty("_EmissionColor"))
        {
            borderMaterial.SetColor("_EmissionColor", highlightColor * 2f);
        }
        
        // Find DoorPanel specifically (child of doorPivot named "DoorPanel")
        Transform doorPanelTransform = null;
        if (doorPivot != null)
        {
            doorPanelTransform = doorPivot.Find("DoorPanel");
            if (doorPanelTransform == null)
            {
                // Try to find any child with renderer
                foreach (Transform child in doorPivot)
                {
                    if (child.GetComponent<Renderer>() != null)
                    {
                        doorPanelTransform = child;
                        break;
                    }
                }
            }
        }
        
        if (doorPanelTransform != null)
        {
            // Get the local scale of DoorPanel for dimensions
            Vector3 panelScale = doorPanelTransform.localScale;
            CreateBorderFrame(panelScale, doorPanelTransform);
        }
        else
        {
            // Fallback - create border on doorPivot
            CreateBorderFrame(new Vector3(0.92f, 2.12f, 0.05f), doorPivot);
        }
    }
    
    private void CreateBorderFrame(Vector3 size, Transform parentTransform)
    {
        float width = size.x;
        float height = size.y;
        float depth = size.z + 0.02f; // Slightly thicker than door
        
        // Create 4 border bars (frame around the door panel)
        borderParts = new GameObject[4];
        
        // Calculate center offset based on panel's local position
        Vector3 centerOffset = parentTransform != null ? parentTransform.localPosition : Vector3.zero;
        
        // Top bar
        borderParts[0] = CreateBorderBar("TopBorder", 
            centerOffset + new Vector3(0, height/2 + borderThickness/2, 0),
            new Vector3(width + borderThickness*2, borderThickness, depth),
            parentTransform != null ? parentTransform.parent : doorPivot);
        
        // Bottom bar
        borderParts[1] = CreateBorderBar("BottomBorder", 
            centerOffset + new Vector3(0, -height/2 - borderThickness/2, 0),
            new Vector3(width + borderThickness*2, borderThickness, depth),
            parentTransform != null ? parentTransform.parent : doorPivot);
        
        // Left bar
        borderParts[2] = CreateBorderBar("LeftBorder", 
            centerOffset + new Vector3(-width/2 - borderThickness/2, 0, 0),
            new Vector3(borderThickness, height, depth),
            parentTransform != null ? parentTransform.parent : doorPivot);
        
        // Right bar
        borderParts[3] = CreateBorderBar("RightBorder", 
            centerOffset + new Vector3(width/2 + borderThickness/2, 0, 0),
            new Vector3(borderThickness, height, depth),
            parentTransform != null ? parentTransform.parent : doorPivot);
    }
    
    private GameObject CreateBorderBar(string name, Vector3 localPos, Vector3 scale, Transform parentObj)
    {
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = name;
        
        // Parent to doorPivot so border moves with the door
        bar.transform.SetParent(parentObj != null ? parentObj : transform);
        bar.transform.localPosition = localPos;
        bar.transform.localRotation = Quaternion.identity;
        bar.transform.localScale = scale;
        
        // Remove collider from border
        Collider col = bar.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        // Apply material
        Renderer rend = bar.GetComponent<Renderer>();
        rend.material = borderMaterial;
        
        return bar;
    }
}
