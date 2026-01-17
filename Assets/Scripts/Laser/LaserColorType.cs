using UnityEngine;

public enum LaserColorType
{
    Red,
    Blue,
    Green,
    Yellow,
    Orange,
    Pink,
    White,
    Black
}

public static class LaserColors
{
    public static Color GetColor(LaserColorType type)
    {
        return type switch
        {
            LaserColorType.Red => new Color(1f, 0.2f, 0.2f, 1f),
            LaserColorType.Blue => new Color(0.2f, 0.4f, 1f, 1f),
            LaserColorType.Green => new Color(0.2f, 1f, 0.3f, 1f),
            LaserColorType.Yellow => new Color(1f, 0.9f, 0.2f, 1f),
            LaserColorType.Orange => new Color(1f, 0.5f, 0.1f, 1f),
            LaserColorType.Pink => new Color(1f, 0.4f, 0.7f, 1f),
            LaserColorType.White => new Color(1f, 1f, 1f, 1f),
            LaserColorType.Black => new Color(0.1f, 0.1f, 0.1f, 1f),
            _ => Color.white
        };
    }
    
    public static Color GetInactiveColor(LaserColorType type)
    {
        Color baseColor = GetColor(type);
        return new Color(baseColor.r * 0.3f, baseColor.g * 0.3f, baseColor.b * 0.3f, 1f);
    }
}
