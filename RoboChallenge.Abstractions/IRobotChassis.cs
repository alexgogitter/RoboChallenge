namespace RoboChallenge.Abstractions;

public interface IRobotChassis
{
    int X { get; }
    int Y { get; }

    IRadar Radar { get; }
    IBeaconScanner BeaconScanner { get; }

    Velocity Velocity { get; set; }
}
