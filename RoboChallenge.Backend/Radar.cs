namespace RoboChallenge.Backend;

using RoboChallenge.Abstractions;
using System;

class Radar : IRadar
{
    World world;
    public Radar(World w)
    {
        world = w;
    }
    public uint MaxRange => 8; // Arbitrary value for maximum range of the radar
    public uint Scan(Direction direction)
    {
        // Determine the range to the next wall in the given direction from the robot's position:
        int x = (int)world.RobotX;
        int y = (int)world.RobotY;
        int range = 0;
        switch (direction)
        {
            case Direction.Up:
                while (y - range > 0 && world.ScanCell((uint)x, (uint)(y - range)) != IWorld.CellContent.Wall) ++range;
                break;
            case Direction.Down:
                while (y + range < world.WorldHeight && world.ScanCell((uint)x, (uint)(y + range)) != IWorld.CellContent.Wall) ++range;
                break;
            case Direction.Left:
                while (x - range > 0 && world.ScanCell((uint)(x - range), (uint)y) != IWorld.CellContent.Wall) ++range;
                break;
            case Direction.Right:
                while (x + range < world.WorldWidth && world.ScanCell((uint)(x + range), (uint)y) != IWorld.CellContent.Wall) ++range;
                break;
        }

        return (uint)Math.Min(range, (int)MaxRange);
    }
}
