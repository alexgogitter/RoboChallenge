using RoboChallenge.Abstractions;

namespace RoboChallenge.Backend;

public class RoboChallengeRunner : IRoboChallengeRunner
{
    public void RunChallenge(IRobot robot)
    {
        World w = new();
        w.Draw();

        IRobotChassis chassis = new RobotChassis(w); // The interface between the robot brain and the world. Provided by the game.

        while (true)
        {
            System.Console.Clear();
            w.Draw();

            // Sleep for a second.
            // Ask the robot what do to
            // Based on robot's velocity - move the robot in the world, check for collisions, check for goal found.
            Thread.Sleep(1000);

            robot.ClockCycle(chassis);

            // What did the robot decide to do? Move it in the world:
            if (chassis.Velocity.speed == Speed.Stopped)
            {
                System.Console.WriteLine("Robot is stopped.");
                continue;
            }
            try
            {
                switch (chassis.Velocity.direction)
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
            }
            catch (IWorld.FoundBeaconException)
            {
                robot.FoundBeacon();
                break; // Game over!
            }

        }

        System.Console.WriteLine("Press any key to continue...");
        System.Console.ReadKey();
    }
}
