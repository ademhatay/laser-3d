using UnityEngine;

public class Collectable : MonoBehaviour
{
    [Header("Collectable Settings")]
    [SerializeField] private int pointValue = 50;
    [SerializeField] private CollectableType type = CollectableType.Generic;
    
    [Header("Visual Effects")]
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.2f;
    [SerializeField] private Color glowColor = Color.yellow;
    
    [Header("Collection Effects")]
    [SerializeField] private GameObject collectEffect;
    [SerializeField] private bool destroyOnCollect = true;
    
    private Vector3 startPosition;
    private Light glowLight;
    
    public int PointValue => pointValue;
    public CollectableType Type => type;
    
    public enum CollectableType
    {
        Generic,
        Key,
        PowerUp,
        Hint,
        BonusTime
    }
    
    void Start()
    {
        startPosition = transform.position;
        
        // Add glow light if not present
        glowLight = GetComponentInChildren<Light>();
        if (glowLight == null)
        {
            GameObject lightObj = new GameObject("GlowLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.zero;
            glowLight = lightObj.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = glowColor;
            glowLight.intensity = 1f;
            glowLight.range = 3f;
        }
        
        // Set renderer emission
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null && rend.material != null)
        {
            rend.material.EnableKeyword("_EMISSION");
            rend.material.SetColor("_EmissionColor", glowColor * 0.5f);
        }
    }
    
    void Update()
    {
        // Rotate continuously
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        
        // Bob up and down
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        
        // Pulse glow
        if (glowLight != null)
        {
            glowLight.intensity = 1f + Mathf.Sin(Time.time * 3f) * 0.3f;
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Check if player touched
        if (other.CompareTag("Player") || other.GetComponent<FirstPersonController>() != null)
        {
            Collect();
        }
    }
    
    private void Collect()
    {
        // Notify GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CollectItem(this);
        }
        
        // Spawn effect
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }
        
        // Handle special types
        switch (type)
        {
            case CollectableType.BonusTime:
                // Could add time if you implement that feature
                Debug.Log("Bonus Time collected!");
                break;
            case CollectableType.Key:
                Debug.Log("Key collected!");
                break;
            case CollectableType.PowerUp:
                Debug.Log("Power-up collected!");
                break;
            case CollectableType.Hint:
                Debug.Log("Hint collected!");
                break;
        }
        
        // Destroy or disable
        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
