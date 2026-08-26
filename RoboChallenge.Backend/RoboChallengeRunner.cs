using RoboChallenge.Abstractions;

namespace RoboChallenge.Backend;

public class RoboChallengeRunner : IRoboChallengeRunner
{
    public const int SLEEP_TIME_MS = 500; /* 1/frame rate */

    public void RunChallenge(IRobot robot)
    {
        IWorld w = new World();
        IWorldVisualiser visualiser = new ConsoleWorldVisualiser();

        IRobotChassis chassis = new RobotChassis(w); // The interface between the robot brain and the world. Provided by the game.

        visualiser.Draw(w);

        while (true)
        {
            // Sleep for a second.
            // Ask the robot what do to
            // Based on robot's velocity - move the robot in the world, check for collisions, check for goal found.
            Thread.Sleep(SLEEP_TIME_MS);

            robot.ClockCycle(chassis);

            // What did the robot decide to do? Move it in the world:
            if (chassis.Motor.Speed == Speed.Stopped)
            {
                visualiser.GameStatus = "Robot is stopped!";
            }
            else
            {
                try
                {
                    switch (chassis.Motor.FacingDirection)
                    {
                        case Direction.Up:
                            w.SetRobotPosition(w.RobotX, w.RobotY + 1);
                            break;
                        case Direction.Down:
                            w.SetRobotPosition(w.RobotX, w.RobotY - 1);
                            break;
                        case Direction.Left:
                            w.SetRobotPosition(w.RobotX - 1, w.RobotY);
                            break;
                        case Direction.Right:
                            w.SetRobotPosition(w.RobotX + 1, w.RobotY);
                            break;
                    }
                }
                catch (IWorld.HitWallException)
                {
                    robot.CollidedWithWall();
                    visualiser.GameStatus = "Robot hit a wall!";
                }
                catch (IWorld.FoundBeaconException)
                {
                    robot.FoundBeacon();
                    visualiser.GameStatus = "Robot found the beacon - game over!";
                    break; // Game over!
                }
            }

            visualiser.Draw(w);
        }

        System.Console.WriteLine("Press any key to continue...");
        System.Console.ReadKey();
    }
}
