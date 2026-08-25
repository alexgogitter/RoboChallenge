using RoboChallenge.Abstractions;

namespace RoboChallenge.Backend;

class RobotChassis : IRobotChassis
{
    World world;
    public RobotChassis(World w)
    {
        world = w;
        BeaconScanner = new BeaconScanner(w);
        Radar = new Radar(w);
        Motor = new Motor();
    }
    public int X { get; private set; } = 0;
    public int Y { get; private set; } = 0;
    public IRadar Radar { get; init; }
    public IBeaconScanner BeaconScanner { get; init; }
    public IMotor Motor { get; init; }
}
