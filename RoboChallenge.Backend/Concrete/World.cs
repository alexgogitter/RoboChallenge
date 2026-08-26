namespace RoboChallenge.Backend.Concrete;

using RoboChallenge.Abstractions;
using System;

class World : IWorld
{
    /* The world is a 100x100 board, -50->50. The Goal occupies one random corner. The Player Robot is at 0,0. A maze/tunnel is between the robot and the goal.
    Use the radar to scout the maze, the beacon scanner to find the target and command your robot to move until you find the goal.
    Walls are only ever horizonal or vertical.
    I guess it's possible to generate an unsolvable maze....
    */

    private IWorld.CellContent[,] board;
    public uint GoalX { get; init; }
    public uint GoalY { get; init; }
    public uint RobotX { get; private set; }
    public uint RobotY { get; private set; }
    public uint WorldWidth { get; init; }
    public uint WorldHeight { get; init; }

    public IWorld.CellContent ScanCell(uint x, uint y)
    {
        if (x >= WorldWidth || y >= WorldHeight) throw new ArgumentOutOfRangeException();
        return board[x, y];
    }


    public void SetRobotPosition(uint x, uint y)
    {
        if (x >= WorldWidth || y >= WorldHeight) throw new ArgumentOutOfRangeException();
        if (board[x, y] == IWorld.CellContent.Wall) throw new IWorld.HitWallException();
        if (board[x, y] == IWorld.CellContent.Goal) throw new IWorld.FoundBeaconException();
        // Clear out the old robot position:
        board[RobotX, RobotY] = IWorld.CellContent.Empty;
        RobotX = x;
        RobotY = y;
        board[x, y] = IWorld.CellContent.Robot;
    }

    public World(IWorldGenerator wg)
    {
        board = wg.GenerateWorld(out uint worldWidth, out uint worldHeight, out uint goalX, out uint goalY, out uint robotX, out uint robotY);
        WorldWidth = worldWidth;
        WorldHeight = worldHeight;
        GoalX = goalX;
        GoalY = goalY;
        RobotX = robotX;
        RobotY = robotY;

    }
}
