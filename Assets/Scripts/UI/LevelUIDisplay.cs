using UnityEngine;
using TMPro;

public class LevelUIDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelNameText;
    [SerializeField] private TextMeshProUGUI levelNumberText;
    [SerializeField] private TextMeshProUGUI completionStatusText;

    private void Start()
    {
        if (LevelManager.Instance != null)
        {
            UpdateLevelDisplay();
            LevelManager.Instance.OnLevelDataUpdated += UpdateLevelDisplay;
        }
        else
        {
            Debug.LogWarning("[LevelUIDisplay] LevelManager Instance bulunamadı!");
        }
    }

    private void UpdateLevelDisplay()
    {
        if (levelNameText != null)
        {
            levelNameText.text = LevelManager.Instance.GetLevelName();
        }

        if (levelNumberText != null)
        {
            levelNumberText.text = $"#{LevelManager.Instance.GetLevelNumber()}";
        }

        UpdateCompletionStatus();
    }

    private void UpdateCompletionStatus()
    {
        if (completionStatusText != null)
        {
            int completed = LevelManager.Instance.GetCompletedTargetCount();
            int total = LevelManager.Instance.GetTotalTargetCount();
            completionStatusText.text = $"{completed}/{total} Hedef";
        }
    }

    private void OnDestroy()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelDataUpdated -= UpdateLevelDisplay;
        }
    }
}
