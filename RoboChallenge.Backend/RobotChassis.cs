using RoboChallenge.Abstractions;

namespace RoboChallenge.Backend;

class RobotChassis(IWorld w) : IRobotChassis
{
    public int X { get; private set; } = 0;
    public int Y { get; private set; } = 0;
    public IRadar Radar { get; init; } = new Radar(w);
    public IBeaconScanner BeaconScanner { get; init; } = new BeaconScanner(w);
    public IMotor Motor { get; init; } = new Motor();
}
