namespace RoboChallenge.Backend;
using System;

interface IWorld
{
    enum CellContent
    {
        Empty,
        Wall,
        Robot,
        Goal
    }
    uint WorldWidth { get; }
    uint WorldHeight { get; }
    uint GoalX { get; } // No peeking - do not permit the Robot access to the IWorld interface directly. The only sensors are the Radar and BeaconScanner.
    uint GoalY { get; }
    uint RobotX { get; }
    uint RobotY { get; }

    public class HitWallException : Exception { };
    public class FoundBeaconException : Exception { };

    void SetRobotPosition(uint x, uint y); // Move the robot to a new position. Throws exception if robot hits a wall or finds the beacon.
    CellContent ScanCell(uint x, uint y); // Returns the content of a cell in the world. Used by the Radar and BeaconScanner to determine what is in a cell. NO CHEATING!

}
