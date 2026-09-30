namespace RoboChallenge.Abstractions;

public enum Speed
{
    Stopped,
    Moving
}

public enum Direction
{
    Up,
    Down,
    Left,
    Right
}

public struct RoboIntention
{
    public Direction Direction { get; set; }
    public Speed Speed { get; set; }
}

public struct RadarDirections
{
    public RadarDirections()
    {
        Up = 0;
        Down = 0;
        Left = 0;
        Right = 0;
    }
    public uint Up, Down, Left, Right;
}
