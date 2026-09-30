using RoboChallenge.Abstractions;

namespace RoboChallenge.Backend.Concrete;

class BeaconScanner(IWorld w) : IBeaconScanner
{
    public void Detect(out Direction lateral, out Direction vertical)
    {
        // Determine the direction to the goal from the robot's position:
        int dx = (int)w.GoalX - (int)w.RobotX;
        int dy = (int)w.GoalY - (int)w.RobotY;
        if (dx > 0) lateral = Direction.Right;
        else if (dx < 0) lateral = Direction.Left;
        else lateral = Direction.Up; // No lateral movement needed (erm this is an exception)

        if (dy > 0) vertical = Direction.Up;
        else if (dy < 0) vertical = Direction.Down;
        else vertical = Direction.Left; // No vertical movement needed (erm this is an exception)
    }

}
