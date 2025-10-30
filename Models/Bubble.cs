using SkiaSharp;

namespace BubbleShot.Models;

public class Bubble
{
    public int Row { get; set; }
    public int Col { get; set; }
    public BubbleColor Color { get; set; }
    public SKPoint Position { get; set; }
    public float Radius { get; set; }
    public bool IsPopping { get; set; }
    public float PopAnimationProgress { get; set; }

    public Bubble(int row, int col, BubbleColor color, SKPoint position, float radius)
    {
        Row = row;
        Col = col;
        Color = color;
        Position = position;
        Radius = radius;
        IsPopping = false;
        PopAnimationProgress = 0;
    }

    public bool CollidesWith(SKPoint point, float otherRadius)
    {
        var distance = SKPoint.Distance(Position, point);
        return distance < (Radius + otherRadius);
    }

    public SKColor GetSKColor()
    {
        return Color switch
        {
            BubbleColor.Red => SKColors.Red,
            BubbleColor.Blue => SKColors.Blue,
            BubbleColor.Green => SKColors.Green,
            BubbleColor.Yellow => SKColors.Yellow,
            BubbleColor.Purple => SKColors.Purple,
            BubbleColor.Orange => SKColors.Orange,
            _ => SKColors.Transparent
        };
    }
}
