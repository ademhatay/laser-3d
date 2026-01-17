using UnityEngine;

public class LaserBeam : MonoBehaviour
{
    [Header("Laser Settings")]
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private int maxReflections = 10;
    [SerializeField] private float laserWidth = 0.05f;
    private Color laserColor = new Color(0f, 0.8f, 1f, 1f);
    private LaserColorType colorType = LaserColorType.Red;
    
    [Header("References")]
    [SerializeField] private Transform laserOrigin;
    [SerializeField] private LayerMask reflectiveLayers;
    [SerializeField] private string mirrorTag = "Mirror";
    [SerializeField] private string targetTag = "Target";
    [SerializeField] private string absorberTag = "Absorber";
    
    private LineRenderer lineRenderer;
    private bool hitTarget = false;
    
    public bool HasHitTarget => hitTarget;
    
    public static event System.Action OnTargetHit;
    public static event System.Action OnTargetLost;
    
    void Start()
    {
        SetupLineRenderer();
        
        if (laserOrigin == null)
        {
            laserOrigin = transform;
        }
    }
    
    void Update()
    {
        UpdateLaser();
    }
    
    void OnDisable()
    {
        if (hitTarget)
        {
            hitTarget = false;
            OnTargetLost?.Invoke();
        }
        
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }
    }
    
    private void SetupLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        
        // Create laser material
        Material laserMat = new Material(Shader.Find("Sprites/Default"));
        laserMat.color = laserColor;
        lineRenderer.material = laserMat;
        
        // Set line properties
        lineRenderer.startWidth = laserWidth;
        lineRenderer.endWidth = laserWidth;
        lineRenderer.startColor = laserColor;
        lineRenderer.endColor = laserColor;
        lineRenderer.useWorldSpace = true;
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 4;
    }
    
    private void UpdateLaser()
    {
        Vector3[] points = new Vector3[maxReflections + 1];
        int pointCount = 0;
        
        Vector3 currentPosition = laserOrigin.position;
        Vector3 currentDirection = laserOrigin.forward;
        
        points[pointCount++] = currentPosition;
        
        bool previousHitTarget = hitTarget;
        hitTarget = false;
        
        for (int i = 0; i < maxReflections; i++)
        {
            RaycastHit hit;
            
            if (Physics.Raycast(currentPosition, currentDirection, out hit, maxDistance))
            {
                points[pointCount++] = hit.point;
                
        // Check if hit target
        LaserTarget target = hit.collider.GetComponent<LaserTarget>();
        if (target != null || hit.collider.CompareTag(targetTag))
        {
            if (target == null) target = hit.collider.GetComponentInParent<LaserTarget>();
            
            if (target != null && target.MatchesLaser(colorType))
            {
                hitTarget = true;
                target.OnLaserHit(colorType);
                break;
            }
        }
                
                // Check if hit mirror - reflect
                MirrorSelector mirror = hit.collider.GetComponent<MirrorSelector>();
                if (mirror != null || hit.collider.CompareTag(mirrorTag))
                {
                    // Calculate reflection
                    currentDirection = Vector3.Reflect(currentDirection, hit.normal);
                    currentPosition = hit.point + currentDirection * 0.01f; // Small offset to avoid self-hit
                }
                else if (hit.collider.CompareTag(absorberTag))
                {
                    // Hit absorber - trigger hit logic
                    LaserAbsorber absorber = hit.collider.GetComponent<LaserAbsorber>();
                    if (absorber != null)
                    {
                        absorber.OnLaserHit();
                    }
                    break;
                }
                else
                {
                    // Hit something else, stop laser
                    break;
                }
            }
            else
            {
                // No hit, extend laser to max distance
                points[pointCount++] = currentPosition + currentDirection * maxDistance;
                break;
            }
        }
        
        // Update line renderer
        lineRenderer.positionCount = pointCount;
        for (int i = 0; i < pointCount; i++)
        {
            lineRenderer.SetPosition(i, points[i]);
        }
        
        // Fire events
        if (hitTarget && !previousHitTarget)
        {
            OnTargetHit?.Invoke();
            Debug.Log("Laser hit target!");
        }
        else if (!hitTarget && previousHitTarget)
        {
            // If we lost target, we should notify the targets in the scene
            // This is handled by the static events usually, but Color matching needs careful handling
            OnTargetLost?.Invoke();
            Debug.Log("Laser lost target!");
        }
    }
    
    public Color GetLaserColor() => laserColor;
    
    public void SetLaserColor(Color color)
    {
        laserColor = color;
        if (lineRenderer != null)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.material.color = color;
        }
    }
    
    public void SetColorType(LaserColorType type)
    {
        colorType = type;
    }
    
    public LaserColorType GetColorType() => colorType;
    
    public void SetLaserWidth(float width)
    {
        laserWidth = width;
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
        }
    }


}