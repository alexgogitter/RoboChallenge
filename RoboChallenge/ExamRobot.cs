using RoboChallenge.Abstractions;

namespace RoboChallenge;

class ExamRobot : IRobot
{
    private readonly RobotMemory memory = new();

    public void ClockCycle(IRobotChassis controlBus)
    {
        controlBus.BeaconScanner.Detect(out Direction lateral, out Direction vertical);
        GatherDistanceData(controlBus, out RadarDirections distances);
        memory.CreateDecision(lateral, vertical, distances, out RoboIntention intent);

        controlBus.Motor.Speed = intent.Speed;
        controlBus.Motor.FacingDirection = intent.Direction;
    }

    public void CollidedWithWall()
    {
        memory.CollidedWithWall();
    }

    public void FoundBeacon()
    {
        memory.FoundBeacon();
    }

    private static void GatherDistanceData(IRobotChassis controlBus, out RadarDirections distances)
    {
        distances.Up = controlBus.Radar.Scan(Direction.Up);
        distances.Down = controlBus.Radar.Scan(Direction.Down);
        distances.Left = controlBus.Radar.Scan(Direction.Left);
        distances.Right = controlBus.Radar.Scan(Direction.Right);
    }

    public bool hasVisited(int x, int y)
    {
        return memory.hasVisited(x, y);
    }

    internal (int x, int y) getPosition()
    {
       return (memory.getPosition());
    }
}
