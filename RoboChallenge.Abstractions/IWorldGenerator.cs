namespace RoboChallenge.Abstractions;

public interface IWorldGenerator
{
    IWorld.CellContent[,] GenerateWorld(out uint worldWidth, out uint worldHeight, out uint goalX, out uint goalY, out uint robotX, out uint robotY);
}
