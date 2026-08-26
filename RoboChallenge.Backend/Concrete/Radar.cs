namespace RoboChallenge.Backend.Concrete;

using RoboChallenge.Abstractions;
using System;

class Radar(IWorld w) : IRadar
{
    public uint MaxRange => 8; // Arbitrary value for maximum range of the radar
    public uint Scan(Direction direction)
    {
        // Determine the range to the next wall in the given direction from the robot's position:
        int x = (int)w.RobotX;
        int y = (int)w.RobotY;
        int range = 0;
        switch (direction)
        {
            case Direction.Up:
                while (y - range > 0 && w.ScanCell((uint)x, (uint)(y - range)) != IWorld.CellContent.Wall) ++range;
                break;
            case Direction.Down:
                while (y + range < w.WorldHeight && w.ScanCell((uint)x, (uint)(y + range)) != IWorld.CellContent.Wall) ++range;
                break;
            case Direction.Left:
                while (x - range > 0 && w.ScanCell((uint)(x - range), (uint)y) != IWorld.CellContent.Wall) ++range;
                break;
            case Direction.Right:
                while (x + range < w.WorldWidth && w.ScanCell((uint)(x + range), (uint)y) != IWorld.CellContent.Wall) ++range;
                break;
        }

        return (uint)Math.Min(range-1, (int)MaxRange);
    }
}
