namespace RoboChallenge.Backend;

using System;

class World : IWorld
{
    /* The world is a 100x100 board, -50->50. The Goal occupies one random corner. The Player Robot is at 0,0. A maze/tunnel is between the robot and the goal.
    Use the radar to scout the maze, the beacon scanner to find the target and command your robot to move until you find the goal.
    Walls are only ever horizonal or vertical.
    I guess it's possible to generate an unsolvable maze....
    */
    const uint WORLD_WIDTH = 70;
    const uint WORLD_HEIGHT = 35;
    private IWorld.CellContent[,] board = new IWorld.CellContent[WORLD_WIDTH, WORLD_HEIGHT];
    private Random rng;
    public uint GoalX { get; init; }
    public uint GoalY { get; init; }
    public uint RobotX { get; private set; }
    public uint RobotY { get; private set; }
    public uint WorldWidth => WORLD_WIDTH;
    public uint WorldHeight => WORLD_HEIGHT;

    public IWorld.CellContent ScanCell(uint x, uint y)
    {
        if (x >= WORLD_WIDTH || y >= WORLD_HEIGHT) throw new ArgumentOutOfRangeException();
        return board[x, y];
    }


    public void SetRobotPosition(uint x, uint y)
    {
        if (x >= WORLD_WIDTH || y >= WORLD_HEIGHT) throw new ArgumentOutOfRangeException();
        if (board[x, y] == IWorld.CellContent.Wall) throw new IWorld.HitWallException();
        if (board[x, y] == IWorld.CellContent.Goal) throw new IWorld.FoundBeaconException();
        // Clear out the old robot position:
        board[RobotX, RobotY] = IWorld.CellContent.Empty;
        RobotX = x;
        RobotY = y;
        board[x, y] = IWorld.CellContent.Robot;
    }

    public World()
    {
        rng = new Random();
        const uint WALL_MIN_LENGTH = 3;
        const uint WALL_MAX_LENGTH = 15;
        //const uint MIN_WALLS = 10;
        //const uint MAX_WALLS = 20;
        const double MIN_WALL_DENSITY = 0.1;
        const double MAX_WALL_DENSITY = 0.4; // Do not consume more than this percentage of the board with walls
        //const double MIN_GOAL_SEPERATION = 0.65; // goal must be n% away from player start at least
        const uint MIN_GOAL_DISTANCE_FROM_EDGE = 3; // goal must be at least this many squares from the edge of the board
        const uint MAX_GOAL_DISTANCE_FROM_EDGE = 7; // goal must be at most this many squares from the edge of the board
        const bool ALLOW_WALL_CROSSING = false;

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
        RobotX = WORLD_WIDTH / 2;
        RobotY = WORLD_HEIGHT / 2;

        // Place the goal in a random edge corner of the board, but not too close to the edge.
        // Pick a horizonal edge:
        bool left = rng.NextDouble() > 0.5;
        uint goal_x = left ? MIN_GOAL_DISTANCE_FROM_EDGE : WORLD_WIDTH - 1 - MIN_GOAL_DISTANCE_FROM_EDGE;
        // Jitter goal x
        goal_x += (uint)rng.Next(0, (int)(MAX_GOAL_DISTANCE_FROM_EDGE - MIN_GOAL_DISTANCE_FROM_EDGE));

        // Pick a vertical edge:
        bool top = rng.NextDouble() > 0.5;
        uint goal_y = top ? MIN_GOAL_DISTANCE_FROM_EDGE : WORLD_HEIGHT - 1 - MIN_GOAL_DISTANCE_FROM_EDGE;
        // Jitter goal y
        goal_y += (uint)rng.Next(0, (int)(MAX_GOAL_DISTANCE_FROM_EDGE - MIN_GOAL_DISTANCE_FROM_EDGE));

        // Clear out a space around the goal so it doesn't start in a wall:
        for (uint y = goal_y - 1; y <= goal_y + 1; ++y)
        {
            for (uint x = goal_x - 1; x <= goal_x + 1; ++x)
            {
                board[x, y] = IWorld.CellContent.Empty;
            }
        }
        board[goal_x, goal_y] = IWorld.CellContent.Goal;
        GoalX = goal_x;
        GoalY = goal_y;
        
    }

    // Maps a cell to an ANSI foreground color escape code.
    private static string AnsiColor(IWorld.CellContent content) => content switch
    {
        IWorld.CellContent.Wall  => "\u001b[90m",  // bright black / dark gray
        IWorld.CellContent.Empty => "\u001b[34m",  // blue
        IWorld.CellContent.Goal  => "\u001b[93m",  // bright yellow
        IWorld.CellContent.Robot => "\u001b[92m",  // bright green
        _                        => "\u001b[95m",  // bright magenta
    };

    public void Draw()
    {
        // Build the entire frame in memory, then write it in one call (double-buffering).
        var sb = new System.Text.StringBuilder((int)((WORLD_WIDTH + 20) * WORLD_HEIGHT));

        // Home the cursor instead of clearing the screen - avoids the blank flash.
        sb.Append("\u001b[H");

        IWorld.CellContent lastContent = (IWorld.CellContent)(-1); // force first color emit
        for (uint y = 0; y < WORLD_HEIGHT; ++y)
        {
            for (uint x = 0; x < WORLD_WIDTH; ++x)
            {
                IWorld.CellContent content = board[x, y];

                // Only emit a color escape when the color actually changes - keeps the buffer small.
                if (content != lastContent)
                {
                    sb.Append(AnsiColor(content));
                    lastContent = content;
                }

                char c = content switch
                {
                    IWorld.CellContent.Wall => '#',
                    IWorld.CellContent.Empty => '.',
                    IWorld.CellContent.Goal => '$',
                    IWorld.CellContent.Robot => '@',
                    _ => '?'
                };
                sb.Append(c);
            }
            sb.Append('\n');
        }


        sb.Append("\u001b[0m"); // reset colors
        Console.Out.Write(sb.ToString()); // single write - no per-char flushing
    }
}
