using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactDistance = 5f;
    [SerializeField] private float placeDistance = 10f;
    [SerializeField] private Key inputKeyF = Key.F;
    
    [Header("Placement Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Color validPlaceColor = new Color(0f, 1f, 0f, 0.5f);
    [SerializeField] private Color invalidPlaceColor = new Color(1f, 0f, 0f, 0.5f);
    
    [Header("UI")]
    [SerializeField] private bool showCrosshair = true;
    
    private Camera playerCamera;
    private MirrorSelector currentMirror;
    private LaserSource currentSource;
    private InteractableDoor currentDoor;
    private MirrorSelector selectedMirror;
    private InteractableDoor selectedDoor;
    private MirrorSelector carriedMirror;
    private GameObject mirrorPreview;
    private Material previewMaterial;
    private bool isCarrying = false;
    private Vector3 originalMirrorPosition;
    private Quaternion originalMirrorRotation;
    
    void Start()
    {
        playerCamera = Camera.main;
        
        // Set ground layer to default if not set
        if (groundLayer == 0)
        {
            groundLayer = LayerMask.GetMask("Default");
        }
        
        CreatePreviewMaterial();
    }
    
    void Update()
    {
        if (isCarrying)
        {
            UpdateMirrorPlacement();
            HandlePlacement();
        }
        else
        {
            CheckLookingAt();
            HandleInteraction();
            HandleMirrorRotation();
        }
    }
    
    private void CreatePreviewMaterial()
    {
        // Try URP Lit shader first, then Standard
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        
        if (shader == null)
        {
            // Fallback to a simple unlit shader that always works
            shader = Shader.Find("Unlit/Color");
        }
        
        previewMaterial = new Material(shader);
        
        // Try to set transparency based on shader type
        if (shader.name.Contains("Universal") || shader.name.Contains("URP"))
        {
            // URP Lit transparency settings
            previewMaterial.SetFloat("_Surface", 1); // 1 = Transparent
            previewMaterial.SetFloat("_Blend", 0); // Alpha blend
            previewMaterial.SetFloat("_AlphaClip", 0);
            previewMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            previewMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            previewMaterial.SetFloat("_ZWrite", 0);
            previewMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            previewMaterial.renderQueue = 3000;
            previewMaterial.SetColor("_BaseColor", validPlaceColor);
        }
        else if (shader.name == "Standard")
        {
            // Standard shader transparency
            previewMaterial.SetFloat("_Mode", 3); // Transparent
            previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            previewMaterial.SetInt("_ZWrite", 0);
            previewMaterial.DisableKeyword("_ALPHATEST_ON");
            previewMaterial.EnableKeyword("_ALPHABLEND_ON");
            previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            previewMaterial.renderQueue = 3000;
            previewMaterial.color = validPlaceColor;
        }
        else
        {
            // Simple unlit - just set color
            previewMaterial.color = new Color(validPlaceColor.r, validPlaceColor.g, validPlaceColor.b, 1f);
        }
    }
    
    private void CheckLookingAt()
    {
        MirrorSelector newMirror = null;
        LaserSource newSource = null;
        InteractableDoor newDoor = null;
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            // Check for mirror
            newMirror = hit.collider.GetComponent<MirrorSelector>();
            
            // Only check for other interactables if no mirror found
            if (newMirror == null)
            {
                // Check for door
                newDoor = hit.collider.GetComponent<InteractableDoor>();
                if (newDoor == null)
                {
                    newDoor = hit.collider.GetComponentInParent<InteractableDoor>();
                }
                
                // Check for laser source if no door found
                if (newDoor == null)
                {
                    newSource = hit.collider.GetComponent<LaserSource>();
                    if (newSource == null)
                    {
                        newSource = hit.collider.GetComponentInParent<LaserSource>();
                    }
                }
            }
        }
        
        currentMirror = newMirror;
        currentSource = newSource;
        currentDoor = newDoor;
        
        // Auto-selection Logic for Mirror
        if (selectedMirror != currentMirror)
        {
            if (selectedMirror != null)
            {
                selectedMirror.Deselect();
            }
            
            selectedMirror = currentMirror;
            
            if (selectedMirror != null)
            {
                selectedMirror.Select();
            }
        }
        
        // Auto-selection Logic for Door
        if (selectedDoor != currentDoor)
        {
            if (selectedDoor != null)
            {
                selectedDoor.Deselect();
            }
            
            selectedDoor = currentDoor;
            
            if (selectedDoor != null)
            {
                selectedDoor.Select();
            }
        }
    }
    
    private void HandleInteraction()
    {
        if (Mouse.current == null || Keyboard.current == null) return;
        
        // F key interactions
        if (Keyboard.current[inputKeyF].wasPressedThisFrame)
        {
            // Pick up mirror
            if (currentMirror != null)
            {
                PickUpMirror(currentMirror);
            }
            // Toggle door
            else if (currentDoor != null)
            {
                currentDoor.Toggle();
            }
            // Toggle laser source
            else if (currentSource != null)
            {
                currentSource.Toggle();
            }
        }
    }
    
    private void PickUpMirror(MirrorSelector mirror)
    {
        carriedMirror = mirror;
        isCarrying = true;
        
        // Store original position
        originalMirrorPosition = mirror.transform.position;
        originalMirrorRotation = mirror.transform.rotation;
        
        // Hide the actual mirror
        mirror.gameObject.SetActive(false);
        
        // Create preview object
        CreateMirrorPreview(mirror);
        
        // Deselect if selected
        if (selectedMirror == mirror)
        {
            selectedMirror = null;
        }
    }
    
    private void CreateMirrorPreview(MirrorSelector mirror)
    {
        // Create preview cube matching mirror size
        mirrorPreview = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mirrorPreview.name = "MirrorPreview";
        mirrorPreview.transform.localScale = mirror.transform.localScale;
        mirrorPreview.transform.rotation = mirror.transform.rotation;
        
        // Remove collider
        Destroy(mirrorPreview.GetComponent<Collider>());
        
        // Apply transparent material
        Renderer rend = mirrorPreview.GetComponent<Renderer>();
        rend.material = new Material(previewMaterial); // Create instance to avoid shared material issues
        SetPreviewColor(rend, validPlaceColor);
    }
    
    private void SetPreviewColor(Renderer rend, Color color)
    {
        if (rend == null || rend.material == null) return;
        
        // Set color for different shader types
        if (rend.material.HasProperty("_BaseColor"))
        {
            rend.material.SetColor("_BaseColor", color);
        }
        if (rend.material.HasProperty("_Color"))
        {
            rend.material.color = color;
        }
    }
    
    private void UpdateMirrorPlacement()
    {
        if (mirrorPreview == null) return;
        
        // Cast ray from exact screen center (crosshair position)
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        Renderer rend = mirrorPreview.GetComponent<Renderer>();
        
        bool validPlacement = false;
        Vector3 targetPosition;
        
        // Raycast to find placement position
        if (Physics.Raycast(ray, out hit, placeDistance))
        {
            // Place exactly at hit point
            targetPosition = hit.point;
            
            // Check if hit ground (horizontal surface) for valid placement
            float dotProduct = Vector3.Dot(hit.normal, Vector3.up);
            
            if (dotProduct > 0.3f) // Ground or ramp
            {
                // Adjust height so mirror sits on surface
                float mirrorHalfHeight = mirrorPreview.transform.localScale.y * 0.5f;
                targetPosition.y = hit.point.y + mirrorHalfHeight;
                validPlacement = true;
            }
            else
            {
                // Wall or other surface - offset slightly from surface
                targetPosition = hit.point + hit.normal * 0.05f;
            }
        }
        else
        {
            // No hit - place at fixed distance in front of camera
            targetPosition = ray.origin + ray.direction * 3f;
        }
        
        // Set position INSTANTLY (no lerp) - exactly where crosshair points
        mirrorPreview.transform.position = targetPosition;
        
        // Update color based on validity
        SetPreviewColor(rend, validPlacement ? validPlaceColor : invalidPlaceColor);
        
        // Store validity for placement check
        mirrorPreview.name = validPlacement ? "MirrorPreview_Valid" : "MirrorPreview_Invalid";
        
        // Rotate preview with Q/E keys
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            mirrorPreview.transform.Rotate(0, -45f, 0);
        }
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            mirrorPreview.transform.Rotate(0, 45f, 0);
        }
    }
    
    private void HandlePlacement()
    {
        if (Mouse.current == null || Keyboard.current == null) return;

        // Left click to place
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            PlaceMirror();
        }
        
        // Right click or Escape to cancel
        if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelPlacement();
        }
    }
    
    private void PlaceMirror()
    {
        if (carriedMirror == null || mirrorPreview == null) return;
        
        // Check if valid placement using name flag
        if (mirrorPreview.name == "MirrorPreview_Invalid")
        {
            // Invalid placement - don't place
            return;
        }
        
        // Place the mirror at exact preview position
        carriedMirror.transform.position = mirrorPreview.transform.position;
        carriedMirror.transform.rotation = mirrorPreview.transform.rotation;
        carriedMirror.gameObject.SetActive(true);
        
        // Cleanup
        FinishPlacement();
    }
    
    private void CancelPlacement()
    {
        if (carriedMirror == null) return;
        
        // Return mirror to original position
        carriedMirror.transform.position = originalMirrorPosition;
        carriedMirror.transform.rotation = originalMirrorRotation;
        carriedMirror.gameObject.SetActive(true);
        
        // Cleanup
        FinishPlacement();
    }
    
    private void FinishPlacement()
    {
        if (mirrorPreview != null)
        {
            Destroy(mirrorPreview);
            mirrorPreview = null;
        }
        
        carriedMirror = null;
        isCarrying = false;
    }
    
    private void HandleMirrorRotation()
    {
        if (selectedMirror == null || Keyboard.current == null) return;
        
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            selectedMirror.RotateMirror(-45f);
        }
        
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            selectedMirror.RotateMirror(45f);
        }
    }
    
    void OnGUI()
    {
        if (!showCrosshair) return;
        
        float crosshairSize = 20f;
        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;
        
        Color crosshairColor = Color.white;
        string hint = "";
        
        if (isCarrying)
        {
            crosshairColor = Color.green;
            hint = "[Click] Place  [Q/E] Rotate  [RightClick] Cancel";
        }
        else if (currentMirror != null)
        {
            crosshairColor = Color.yellow;
            hint = $"[Q/E] Rotate  [{inputKeyF}] Pick Up";
        }
        else if (currentDoor != null)
        {
            if (currentDoor.IsLocked)
            {
                crosshairColor = new Color(1f, 0.3f, 0.3f); // Red for locked
                hint = "🔒 Locked - Activate all targets to unlock";
            }
            else
            {
                crosshairColor = new Color(0.3f, 1f, 0.5f); // Green for unlocked
                hint = $"[{inputKeyF}] " + (currentDoor.IsOpen ? "Close" : "Open") + " Door";
            }
        }
        else if (currentSource != null)
        {
            crosshairColor = currentSource.IsActive ? Color.cyan : Color.gray;
            hint = $"[{inputKeyF}] " + (currentSource.IsActive ? "Turn OFF" : "Turn ON") + " Laser";
        }
        
        // Create crosshair texture
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, crosshairColor);
        texture.Apply();
        
        // Draw crosshair
        GUI.DrawTexture(new Rect(centerX - crosshairSize/2, centerY - 1, crosshairSize, 2), texture);
        GUI.DrawTexture(new Rect(centerX - 1, centerY - crosshairSize/2, 2, crosshairSize), texture);
        
        // Show hint
        if (!string.IsNullOrEmpty(hint))
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 18;
            style.normal.textColor = crosshairColor;
            style.alignment = TextAnchor.MiddleCenter;
            
            GUI.Label(new Rect(centerX - 200, centerY + 40, 400, 30), hint, style);
        }
    }
}