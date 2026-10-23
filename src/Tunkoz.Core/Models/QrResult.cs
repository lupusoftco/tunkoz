namespace Tunkoz.Core.Models;

public class QrResult
{
    public required string RawContent { get; set; }
    public required Payload Payload { get; set; }
    public Rectangle? BoundingBox { get; set; }
}

public struct Rectangle
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public bool Contains(int x, int y) =>
        x >= X && x < X + Width && y >= Y && y < Y + Height;
}
