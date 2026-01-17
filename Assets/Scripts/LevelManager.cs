using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    [Header("Level Information")]
    [SerializeField] private string levelName = "Level 1";
    [SerializeField] private int levelNumber = 1;
    
    [Header("Completion Requirements")]
    [Tooltip("Colors required to complete this level. ALL targets of these colors must be completed.")]
    [SerializeField] private LaserColorType[] requiredColors = new LaserColorType[0];
    
    [Header("Optional Requirements (Future)")]
    [SerializeField] private int requiredStars = 0;
    [SerializeField] private int requiredCollectables = 0;
    [SerializeField] private bool requireAllTargets = false;
    
    [Header("References")]
    [SerializeField] private InteractableDoor exitDoor;
    
    private List<LaserTarget> allTargets = new List<LaserTarget>();
    private Dictionary<LaserColorType, List<LaserTarget>> targetsByColor = new Dictionary<LaserColorType, List<LaserTarget>>();
    
    void Awake()
    {
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
        FindAllTargets();
        
        // Subscribe to target events
        LaserTarget.OnTargetActivated += CheckLevelCompletion;
        LaserTarget.OnTargetDeactivated += CheckLevelCompletion;
        
        // Find exit door if not assigned
        if (exitDoor == null)
        {
            exitDoor = FindObjectOfType<InteractableDoor>();
        }
        
        // Initial check
        CheckLevelCompletion();
    }
    
    void OnDestroy()
    {
        LaserTarget.OnTargetActivated -= CheckLevelCompletion;
        LaserTarget.OnTargetDeactivated -= CheckLevelCompletion;
    }
    
    private void FindAllTargets()
    {
        allTargets.Clear();
        targetsByColor.Clear();
        
        LaserTarget[] targets = FindObjectsOfType<LaserTarget>();
        allTargets.AddRange(targets);
        
        // Group targets by color
        foreach (LaserTarget target in allTargets)
        {
            LaserColorType color = target.RequiredColorType;
            
            if (!targetsByColor.ContainsKey(color))
            {
                targetsByColor[color] = new List<LaserTarget>();
            }
            
            targetsByColor[color].Add(target);
        }
        
        Debug.Log($"[LevelManager] {levelName} - Found {allTargets.Count} targets");
        // Detailed color breakdown (commented for cleaner logs)
        /*
        foreach (var kvp in targetsByColor)
        {
            Debug.Log($"  - {kvp.Key}: {kvp.Value.Count} target(s)");
        }
        */
    }
    
    private void CheckLevelCompletion()
    {
        bool levelComplete = CheckRequirements();
        
        if (levelComplete)
        {
            UnlockAndOpenDoor();
        }
        else
        {
            LockDoor();
        }
    }
    
    private bool CheckRequirements()
    {
        // If no specific colors required, check all targets
        if (requiredColors == null || requiredColors.Length == 0)
        {
            if (requireAllTargets)
            {
                return CheckAllTargetsCompleted();
            }
            else
            {
                // No requirements set - always allow door to open
                return true;
            }
        }
        
        // Check each required color
        foreach (LaserColorType requiredColor in requiredColors)
        {
            if (!CheckColorCompleted(requiredColor))
            {
                // Reduced logging
            // Debug.Log($"[LevelManager] Required color {requiredColor} not fully completed!");
                return false;
            }
        }
        
        Debug.Log($"[LevelManager] All required colors completed!");
        return true;
    }
    
    private bool CheckColorCompleted(LaserColorType color)
    {
        // Check if we have targets of this color
        if (!targetsByColor.ContainsKey(color))
        {
            Debug.LogWarning($"[LevelManager] Required color {color} has no targets in scene!");
            return false;
        }
        
        List<LaserTarget> colorTargets = targetsByColor[color];
        
        // ALL targets of this color must be activated
        foreach (LaserTarget target in colorTargets)
        {
            if (!target.IsActivated)
            {
                // Reduced logging
            // Debug.Log($"[LevelManager] Color {color}: Target not activated ({colorTargets.IndexOf(target) + 1}/{colorTargets.Count})");
                return false;
            }
        }
        
        // Reduced logging - only log completion
        // Debug.Log($"[LevelManager] Color {color}: All {colorTargets.Count} target(s) completed! ✓");
        return true;
    }
    
    private bool CheckAllTargetsCompleted()
    {
        foreach (LaserTarget target in allTargets)
        {
            if (!target.IsActivated)
            {
                return false;
            }
        }
        return true;
    }
    
    private void UnlockAndOpenDoor()
    {
        if (exitDoor != null)
        {
            exitDoor.Unlock();
            
            if (!exitDoor.IsOpen)
            {
                exitDoor.Open();
                Debug.Log($"[LevelManager] Level {levelName} completed! Door opened.");
            }
        }
    }
    
    private void LockDoor()
    {
        if (exitDoor != null && !exitDoor.IsLocked)
        {
            exitDoor.Lock();
            
            if (exitDoor.IsOpen)
            {
                exitDoor.Close();
            }
        }
    }
    
    // Public methods for future features
    public void AddRequiredColor(LaserColorType color)
    {
        if (!requiredColors.Contains(color))
        {
            var colorList = requiredColors.ToList();
            colorList.Add(color);
            requiredColors = colorList.ToArray();
        }
    }
    
    public void RemoveRequiredColor(LaserColorType color)
    {
        var colorList = requiredColors.ToList();
        colorList.Remove(color);
        requiredColors = colorList.ToArray();
    }
    
    public int GetTargetCountForColor(LaserColorType color)
    {
        if (targetsByColor.ContainsKey(color))
        {
            return targetsByColor[color].Count;
        }
        return 0;
    }
    
    public int GetCompletedTargetCountForColor(LaserColorType color)
    {
        if (targetsByColor.ContainsKey(color))
        {
            return targetsByColor[color].Count(t => t.IsActivated);
        }
        return 0;
    }
}
