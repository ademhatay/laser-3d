using UnityEngine;

public class LaserAbsorber : MonoBehaviour
{
    [SerializeField] private int maxAbsorption = 3;
    [SerializeField] private Material hitMaterial;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private bool loseOnHit = true; // Added property to trigger lose condition
    
    private int currentHits = 0;
    private Material originalMaterial;
    private Renderer meshRenderer;
    private float flashTimer = 0f;
    private Color originalColor;
    
    void Start()
    {
        meshRenderer = GetComponent<Renderer>();
        if (meshRenderer != null)
        {
            originalMaterial = meshRenderer.material;
            originalColor = originalMaterial.color;
        }
    }
    
    void Update()
    {
        if (flashTimer > 0)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0)
            {
                ResetColor();
            }
        }
    }
    
    // Called by LaserBeam system
    public void OnLaserHit()
    {
        if (loseOnHit)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoseGame("Laser absorbed by unstable core!");
            }
            return;
        }

        currentHits++;
        Flash();
        
        if (currentHits >= maxAbsorption)
        {
            Explode();
        }
    }
    
    private void Flash()
    {
        if (meshRenderer != null)
        {
            meshRenderer.material.color = hitFlashColor;
            flashTimer = flashDuration;
        }
    }
    
    private void ResetColor()
    {
        if (meshRenderer != null)
        {
            meshRenderer.material.color = originalColor;
        }
    }
    
    private void Explode()
    {
        Debug.Log($"Absorber {gameObject.name} exploded!");
        
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, transform.rotation);
        }
        
        // Destroy the absorber
        Destroy(gameObject);
    }
}
