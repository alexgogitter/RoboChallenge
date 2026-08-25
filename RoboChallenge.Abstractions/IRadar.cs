namespace RoboChallenge.Abstractions;

public interface IRadar
{
    uint Scan(Direction direction); // Returns range to next wall, directly ahead
    uint MaxRange { get; } // Maximum range of the radar
}
