using UnityEngine;

public class MirrorSelector : MonoBehaviour
{
    public static MirrorSelector CurrentSelected { get; private set; }
    
    [Header("Selection Settings")]
    [SerializeField] private Material selectionMaterial;
    [SerializeField] private Color selectionColor = new Color(1f, 0.9f, 0f, 1f);
    [SerializeField] private float borderThickness = 0.05f;
    
    [Header("Rotation Settings")]
    [SerializeField] private float rotationAngle = 45f;
    [SerializeField] private float rotationDuration = 0.3f;
    [SerializeField] private float rotationCooldown = 0.1f;
    
    private bool isSelected = false;
    private float targetYRotation;
    private float startYRotation;
    private bool isRotating = false;
    private float rotationTimer = 0f;
    private float cooldownTimer = 0f;
    private GameObject[] borderParts;
    private Material runtimeMaterial;
    
    void Start()
    {
        CreateSelectionBorder();
        SetBorderActive(false);
        targetYRotation = transform.eulerAngles.y;
        startYRotation = targetYRotation;
    }
    
    void Update()
    {
        // Update cooldown timer
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }
        
        // Smooth rotation animation
        if (isRotating)
        {
            rotationTimer += Time.deltaTime;
            float t = Mathf.Clamp01(rotationTimer / rotationDuration);
            
            // Use smooth step for easing
            t = t * t * (3f - 2f * t);
            
            float currentY = Mathf.LerpAngle(startYRotation, targetYRotation, t);
            transform.rotation = Quaternion.Euler(0, currentY, 0);
            
            if (t >= 1f)
            {
                transform.rotation = Quaternion.Euler(0, targetYRotation, 0);
                isRotating = false;
                rotationTimer = 0f;
            }
        }
    }
    
    public void RotateMirror(float angle)
    {
        // Prevent rotation if on cooldown or already rotating
        if (cooldownTimer > 0 || isRotating) return;
        
        // Set rotation parameters
        startYRotation = transform.eulerAngles.y;
        targetYRotation = startYRotation + angle;
        
        // Normalize angle
        targetYRotation = (targetYRotation % 360 + 360) % 360;
        
        // Start rotation
        isRotating = true;
        rotationTimer = 0f;
        cooldownTimer = rotationCooldown;
    }
    
    public void Select()
    {
        if (CurrentSelected != null && CurrentSelected != this)
        {
            CurrentSelected.Deselect();
        }
        
        CurrentSelected = this;
        isSelected = true;
        SetBorderActive(true);
    }
    
    public void Deselect()
    {
        isSelected = false;
        SetBorderActive(false);
        if (CurrentSelected == this)
        {
            CurrentSelected = null;
        }
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
        // Create yellow emissive material
        runtimeMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (runtimeMaterial == null)
        {
            runtimeMaterial = new Material(Shader.Find("Standard"));
        }
        runtimeMaterial.color = selectionColor;
        runtimeMaterial.EnableKeyword("_EMISSION");
        runtimeMaterial.SetColor("_EmissionColor", selectionColor * 2f);
        
        // Get mirror dimensions
        Vector3 mirrorScale = transform.localScale;
        float width = mirrorScale.x;
        float height = mirrorScale.y;
        float depth = mirrorScale.z;
        
        // Create 4 border bars (frame around the mirror)
        borderParts = new GameObject[4];
        
        // Top bar
        borderParts[0] = CreateBorderBar("TopBorder", 
            new Vector3(0, height/2 + borderThickness/2, 0),
            new Vector3(width + borderThickness*2, borderThickness, depth + borderThickness*2));
        
        // Bottom bar
        borderParts[1] = CreateBorderBar("BottomBorder", 
            new Vector3(0, -height/2 - borderThickness/2, 0),
            new Vector3(width + borderThickness*2, borderThickness, depth + borderThickness*2));
        
        // Left bar
        borderParts[2] = CreateBorderBar("LeftBorder", 
            new Vector3(-width/2 - borderThickness/2, 0, 0),
            new Vector3(borderThickness, height, depth + borderThickness*2));
        
        // Right bar
        borderParts[3] = CreateBorderBar("RightBorder", 
            new Vector3(width/2 + borderThickness/2, 0, 0),
            new Vector3(borderThickness, height, depth + borderThickness*2));
    }
    
    private GameObject CreateBorderBar(string name, Vector3 localPos, Vector3 scale)
    {
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = name;
        bar.transform.SetParent(transform);
        bar.transform.localPosition = localPos;
        bar.transform.localRotation = Quaternion.identity;
        bar.transform.localScale = scale;
        
        // Remove collider
        Collider col = bar.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        // Apply material
        Renderer rend = bar.GetComponent<Renderer>();
        rend.material = runtimeMaterial;
        
        return bar;
    }
    
    public bool IsSelected() => isSelected;
    public bool IsRotating() => isRotating;
    public float GetRotationAngle() => rotationAngle;
    public void SetRotationAngle(float angle) => rotationAngle = angle;
}