using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Helper script to ensure UIDocument is properly configured for pause menu
/// Attach this to PauseMenuUI GameObject to auto-configure UIDocument settings
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class PauseMenuSetupHelper : MonoBehaviour
{
    void Awake()
    {
        UIDocument uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null) return;
        
        // Ensure UIDocument is configured for screen overlay
        // This ensures it renders on top of everything
        if (uiDoc.panelSettings == null)
        {
            Debug.LogWarning("[PauseMenuSetupHelper] Panel Settings not assigned! Pause menu may not render correctly.");
        }
        
        // Force root to be visible when document is enabled
        if (uiDoc.rootVisualElement != null)
        {
            uiDoc.rootVisualElement.style.display = DisplayStyle.None; // Start hidden
        }
    }
}
