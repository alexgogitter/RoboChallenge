namespace RoboChallenge.Backend;

using System;

class SimpleWorldGenerator : IWorldGenerator
{
    const uint WORLD_WIDTH = 70;
    const uint WORLD_HEIGHT = 25;
    const uint WALL_MIN_LENGTH = 3;
    const uint WALL_MAX_LENGTH = 15;
    const double MIN_WALL_DENSITY = 0.1;
    const double MAX_WALL_DENSITY = 0.4; // Do not consume more than this percentage of the board with walls
    const uint MIN_GOAL_DISTANCE_FROM_EDGE = 3; // goal must be at least this many squares from the edge of the board
    const uint MAX_GOAL_DISTANCE_FROM_EDGE = 7; // goal must be at most this many squares from the edge of the board
    const bool ALLOW_WALL_CROSSING = false;

    public IWorld.CellContent[,] GenerateWorld(out uint worldWidth, out uint worldHeight, out uint goalX, out uint goalY, out uint robotX, out uint robotY)
    {
        Random rng = new();
        IWorld.CellContent[,] board = new IWorld.CellContent[WORLD_WIDTH, WORLD_HEIGHT];
        worldWidth = WORLD_WIDTH;
        worldHeight = WORLD_HEIGHT;

        double wall_density = rng.NextDouble() * (MAX_WALL_DENSITY - MIN_WALL_DENSITY) + MIN_WALL_DENSITY;
        uint target_wall_square_count = (uint)(wall_density * (WORLD_HEIGHT * WORLD_WIDTH));

        for (uint wall_square_count = 0; wall_square_count < target_wall_square_count;)
        {
            bool vertical = rng.NextDouble() > 0.5;
            uint wallLength = (uint)rng.Next((int)WALL_MIN_LENGTH, (int)WALL_MAX_LENGTH);
            wall_square_count += wallLength; /* alright so it's possible to exceed the max density a little but we'll survive */
            uint wall_x, wall_y;

            // pick empty space to start wall
            do
            {
                wall_x = (uint)rng.Next((int)WORLD_WIDTH);
                wall_y = (uint)rng.Next((int)WORLD_HEIGHT);
            } while (board[wall_x, wall_y] != IWorld.CellContent.Empty);

            while (--wallLength > 0)
            {
                if (board[wall_x, wall_y] == IWorld.CellContent.Wall && !ALLOW_WALL_CROSSING) break; // don't make 'X's - more organic maze (probably)
                board[wall_x, wall_y] = IWorld.CellContent.Wall;
                if (vertical) ++wall_y;
                else ++wall_x;
                if (wall_x >= WORLD_WIDTH || wall_y >= WORLD_HEIGHT) break; // don't go off the board
            }

        }


        // Clear out a space around the player so they don't start in a wall:
        for (uint y = WORLD_HEIGHT / 2 - 1; y <= WORLD_HEIGHT / 2 + 1; ++y)
        {
            for (uint x = WORLD_WIDTH / 2 - 1; x <= WORLD_WIDTH / 2 + 1; ++x)
            {
                board[x, y] = IWorld.CellContent.Empty;
            }
        }
        // Player starts in the middle of the board:
        board[WORLD_WIDTH / 2, WORLD_HEIGHT / 2] = IWorld.CellContent.Robot;
        robotX = WORLD_WIDTH / 2;
        robotY = WORLD_HEIGHT / 2;

        // Place the goal in a random edge corner of the board, but not too close to the edge.
        // Pick a horizonal edge:
        bool left = rng.NextDouble() > 0.5;
        uint goal_x = left ? MIN_GOAL_DISTANCE_FROM_EDGE : WORLD_WIDTH - 1 - MIN_GOAL_DISTANCE_FROM_EDGE;
        // Jitter goal x right (if at left edge) or left (if at right edge)
        goal_x += (uint)(rng.Next(0, (int)(MAX_GOAL_DISTANCE_FROM_EDGE - MIN_GOAL_DISTANCE_FROM_EDGE)) * (left ? 1 : -1));

        // Pick a vertical edge:
        bool top = rng.NextDouble() > 0.5;
        uint goal_y = top ? MIN_GOAL_DISTANCE_FROM_EDGE : WORLD_HEIGHT - 1 - MIN_GOAL_DISTANCE_FROM_EDGE;
        // Jitter goal y down (if at top edge) or up (if at bottom edge)
        goal_y += (uint)(rng.Next(0, (int)(MAX_GOAL_DISTANCE_FROM_EDGE - MIN_GOAL_DISTANCE_FROM_EDGE)) * (top ? 1 : -1));

        // Clear out a space around the goal so it doesn't start in a wall:
        for (uint y = goal_y - 1; y <= goal_y + 1; ++y)
        {
            for (uint x = goal_x - 1; x <= goal_x + 1; ++x)
            {
                board[x, y] = IWorld.CellContent.Empty;
            }
        }
        board[goal_x, goal_y] = IWorld.CellContent.Goal;
        goalX = goal_x;
        goalY = goal_y;

        return board;
    }
}
