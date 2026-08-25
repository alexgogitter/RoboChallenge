using RoboChallenge.Abstractions;

namespace RoboChallenge.Backend;

class BeaconScanner : IBeaconScanner
{
    private World world;
    public BeaconScanner(World w)
    {
        world = w;
    }

    public void Detect(out Direction lateral, out Direction vertical)
    {
        // Determine the direction to the goal from the robot's position:
        int dx = (int)world.GoalX - (int)world.RobotX;
        int dy = (int)world.GoalY - (int)world.RobotY;
        if (dx > 0) lateral = Direction.Right;
        else if (dx < 0) lateral = Direction.Left;
        else lateral = Direction.Up; // No lateral movement needed (erm this is an exception)

        if (dy > 0) vertical = Direction.Down;
        else if (dy < 0) vertical = Direction.Up;
        else vertical = Direction.Up; // No lateral movement needed (erm this is an exception)
    }

}
