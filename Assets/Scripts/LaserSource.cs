using UnityEngine;

public class LaserSource : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private bool isActive = true;
    
    [Header("References")]
    [SerializeField] private LaserBeam laserBeam;
    [SerializeField] private Light sourceLight;
    [SerializeField] private Renderer sourceRenderer;
    
    [Header("Color Settings")]
    [SerializeField] private LaserColorType colorType = LaserColorType.Red;
    
    private Color activeColor;
    private Color inactiveColor;
    
    [Header("Energy Settings")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float drainRate = 5f; // Energy depleted per second
    [SerializeField] private bool canRecharge = false;
    [SerializeField] private float rechargeRate = 2f;
    
    private float currentEnergy;
    
    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;
    public float EnergyNormalized => Mathf.Clamp01(currentEnergy / maxEnergy);
    
    public static event System.Action<float> OnEnergyChanged;
    
    public bool IsActive => isActive;
    public LaserColorType ColorType => colorType;
    
    void Start()
    {
        // Find components if not assigned
        if (laserBeam == null)
        {
            laserBeam = GetComponent<LaserBeam>();
            if (laserBeam == null)
            {
                laserBeam = GetComponentInChildren<LaserBeam>();
            }
        }
        
        if (sourceLight == null)
        {
            sourceLight = GetComponent<Light>();
            if (sourceLight == null)
            {
                sourceLight = GetComponentInChildren<Light>();
            }
        }
        
        if (sourceRenderer == null)
        {
            sourceRenderer = GetComponent<Renderer>();
            if (sourceRenderer == null)
            {
                sourceRenderer = GetComponentInChildren<Renderer>();
            }
        }
        
        currentEnergy = maxEnergy;
        
        // Set colors based on selected color type
        activeColor = LaserColors.GetColor(colorType);
        inactiveColor = LaserColors.GetInactiveColor(colorType);
        
        // Initialize beam color if present
        if (laserBeam != null)
        {
            laserBeam.SetLaserColor(activeColor);
            laserBeam.SetColorType(colorType);
        }
        
        UpdateVisuals();
    }
    
    void Update()
    {
        if (isActive)
        {
            currentEnergy -= drainRate * Time.deltaTime;
            currentEnergy = Mathf.Max(0, currentEnergy);
            OnEnergyChanged?.Invoke(EnergyNormalized);
            
            if (currentEnergy <= 0)
            {
                SetActive(false);
                Debug.Log("Laser Source: OUT OF ENERGY!");
                
                // Trigger Lose Game when energy is empty
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.LoseGame("Laser Power Depleted!");
                }
            }
        }
        else if (canRecharge && currentEnergy < maxEnergy)
        {
            currentEnergy += rechargeRate * Time.deltaTime;
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy);
            OnEnergyChanged?.Invoke(EnergyNormalized);
        }
    }
    
    public void Toggle()
    {
        if (!isActive && currentEnergy <= 0)
        {
            Debug.Log("Cannot activate laser: No energy!");
            return;
        }
        
        isActive = !isActive;
        UpdateVisuals();
        Debug.Log("Laser Source " + (isActive ? "ACTIVATED" : "DEACTIVATED"));
    }
    
    public void SetActive(bool active)
    {
        isActive = active;
        UpdateVisuals();
    }
    
    private void UpdateVisuals()
    {
        // Enable/disable laser beam
        if (laserBeam != null)
        {
            laserBeam.enabled = isActive;
            
            // Also disable the LineRenderer
            LineRenderer lr = laserBeam.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.enabled = isActive;
            }
        }
        
        // Update light
        if (sourceLight != null)
        {
            sourceLight.enabled = isActive;
            sourceLight.color = isActive ? activeColor : inactiveColor;
        }
        
        // Update material color
        if (sourceRenderer != null)
        {
            Color color = isActive ? activeColor : inactiveColor;
            sourceRenderer.material.color = color;
            sourceRenderer.material.SetColor("_EmissionColor", color * (isActive ? 2f : 0.5f));
        }
    }

    // SetColor removed for uniform behavior
}