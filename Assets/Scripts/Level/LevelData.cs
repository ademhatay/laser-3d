using UnityEngine;
using System;

[CreateAssetMenu(fileName = "Level", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public int levelNumber = 1;
    public string levelName = "Level 1";
    [TextArea(2, 4)]
    public string levelDescription = "Aynayı kullanarak ışığı hedefe yönlendir";
    
    [Header("Scene Reference")]
    public string sceneName; // "Level_01", "Level_02" etc.
    public int sceneBuildIndex = -1;
    
    [Header("Tamamlanma Kuralları")]
    [Tooltip("Hangi renk lazerler tüm hedeflerine ulaşmalı")]
    public LaserColorType[] requiredColors;
    
    [Tooltip("Tüm toplanabilir itemler toplanmalı mı?")]
    public bool requireAllCollectables = false;
    
    [Tooltip("Sadece kırmızı lazerleri yakmalı mı?")]
    public bool allowOnlyRedLasers = false;
    
    [Tooltip("Süre sınırı (saniye, 0 = sınır yok)")]
    public float timeLimit = 0f;
    
    [Header("Level Elemanları")]
    public int totalMirrors = 3;
    public int totalTargets = 2;
    public int totalCollectables = 0;
    
    [Header("Yıldız Kriterleri")]
    public float threeStarTime = 30f;
    public float twoStarTime = 60f;
    public float oneStarTime = 120f;
    
    [Header("Kilit Gereksinimleri")]
    public int requiredLevel = 0; // Hangi level tamamlanmalı
    public int requiredStars = 0; // Toplam kaç yıldız gerekli
    
    [Header("Görsel")]
    public Sprite levelThumbnail;
    public Color themeColor = Color.cyan;
    
    // Runtime'da hesaplanır
    [NonSerialized] public bool isUnlocked = false;
    [NonSerialized] public bool isCompleted = false;
    [NonSerialized] public int bestStars = 0;
    [NonSerialized] public float bestTime = 999f;
    
    public int CalculateStars(float completionTime)
    {
        if (completionTime <= threeStarTime) return 3;
        if (completionTime <= twoStarTime) return 2;
        if (completionTime <= oneStarTime) return 1;
        return 0;
    }
}