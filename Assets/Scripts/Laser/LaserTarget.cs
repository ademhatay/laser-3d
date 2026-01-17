using UnityEngine;

public class LaserTarget : MonoBehaviour
{
    [Header("Color Settings")]
    [SerializeField] private LaserColorType requiredColorType = LaserColorType.Red;
    [SerializeField] private bool acceptAnyColor = false;
    
    [Header("Visual Feedback")]
    private Color inactiveColor;
    private Color targetColor;
    private readonly Color wrongColorIndicator = new Color(1f, 0.5f, 0f, 1f); // Orange for wrong color
    private readonly Color completedColor = new Color(0.2f, 1f, 0.3f, 1f); // Green for completed
    [SerializeField] private Light targetLight;
    [SerializeField] private TextMesh targetLabel;
    
    private Renderer targetRenderer;
    private Renderer[] allRenderers; // All renderers (TargetBase + TargetReceiver)
    private bool isActivated = false;
    private float lastHitTime = 0f;
    private const float hitThreshold = 0.1f;
    
    public bool IsActivated => isActivated;
    public Color TargetColor => targetColor;
    public LaserColorType RequiredColorType => requiredColorType;
    
    public static event System.Action OnTargetActivated;
    public static event System.Action OnTargetDeactivated;
    
    void Start()
    {
        // Find TargetBase renderer specifically
        Transform targetBase = transform.Find("TargetBase");
        if (targetBase != null)
        {
            targetRenderer = targetBase.GetComponent<Renderer>();
        }
        
        // Fallback to any child renderer if TargetBase not found
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }
        
        // Get ALL renderers (TargetBase + TargetReceiver) for visual updates
        allRenderers = GetComponentsInChildren<Renderer>();
        
        if (targetLight == null)
        {
            targetLight = GetComponentInChildren<Light>();
        }
        
        // Find the TargetLabel by name if not assigned
        if (targetLabel == null)
        {
            Transform labelTransform = transform.Find("Label");
            if (labelTransform != null)
            {
                targetLabel = labelTransform.GetComponent<TextMesh>();
            }
        }
        
        // Set colors based on required color type
        targetColor = LaserColors.GetColor(requiredColorType);
        inactiveColor = LaserColors.GetInactiveColor(requiredColorType);
        
        SetInactive();
    }
    
    void Update()
    {
        // Auto-deactivate if not hit recently
        if (isActivated && Time.time - lastHitTime > hitThreshold)
        {
            SetInactive();
        }
    }
    
    public bool MatchesLaser(LaserColorType laserColor)
    {
        return acceptAnyColor || laserColor == requiredColorType;
    }
    

    
    public void OnLaserHit(LaserColorType laserColor)
    {
        lastHitTime = Time.time;
        
        if (MatchesLaser(laserColor))
        {
            if (!isActivated)
            {
                SetActive();
            }
        }
        else
        {
            // Wrong color - show feedback
            ShowWrongColorFeedback();
        }
    }
    
    private void ShowWrongColorFeedback()
    {
        // Flash orange to indicate wrong color
        if (targetRenderer != null)
        {
            targetRenderer.material.color = wrongColorIndicator;
            targetRenderer.material.SetColor("_EmissionColor", wrongColorIndicator * 1.5f);
        }
        
        if (targetLight != null)
        {
            targetLight.color = wrongColorIndicator;
        }
    }
    
    public void SetActive()
    {
        isActivated = true;
        
        // Always use GREEN when completed (regardless of required color)
        Color greenColor = completedColor;
        
        // Update ALL renderers (TargetBase + TargetReceiver)
        if (allRenderers != null)
        {
            foreach (Renderer rend in allRenderers)
            {
                if (rend != null)
                {
                    rend.material.color = greenColor;
                    rend.material.SetColor("_EmissionColor", greenColor * 2f);
                }
            }
        }
        
        if (targetLight != null)
        {
            targetLight.color = greenColor;
            targetLight.intensity = 2f;
        }
        
        // Update the label to show COMPLETED in green
        if (targetLabel != null)
        {
            targetLabel.text = "COMPLETED";
            targetLabel.color = greenColor;
        }
        
        OnTargetActivated?.Invoke();
    }
    
    public void SetInactive()
    {
        isActivated = false;
        
        // Update ALL renderers (TargetBase + TargetReceiver)
        if (allRenderers != null)
        {
            foreach (Renderer rend in allRenderers)
            {
                if (rend != null)
                {
                    rend.material.color = inactiveColor;
                    rend.material.SetColor("_EmissionColor", inactiveColor);
                }
            }
        }
        
        if (targetLight != null)
        {
            targetLight.color = inactiveColor;
            targetLight.intensity = 1f;
        }
        
        // Update the label back to TARGET in inactive color
        if (targetLabel != null)
        {
            targetLabel.text = "TARGET";
            targetLabel.color = inactiveColor;
        }
        
        OnTargetDeactivated?.Invoke();
    }

    // SetTargetSettings removed for uniform behavior
}