using RoboChallenge.Abstractions;

namespace RoboChallenge;

class ExampleRobot: IRobot
{
    public void CollidedWithWall()
    {
        // Oops! We hit a wall. Stop moving and try a different direction next time.
        System.Console.WriteLine("Ouch!");
    }
    public void FoundBeacon()
    {
        // Game over!
        System.Console.WriteLine("I found the beacon!");
    }
    public void ClockCycle(IRobotChassis controlBus) // EXAMPLE CODE I DUNNO
    {
        // For now, just move the robot to the right.
        //controlBus.Velocity = new Velocity(Speed.Moving, Direction.Right);

        // OK. Which direction is the goal?
        controlBus.BeaconScanner.Detect(out Direction lateralDirectionToGoal, out Direction verticalDirectionToGoal);

        // Head towards the goal, but check for walls first:
        uint lateralRange = controlBus.Radar.Scan(lateralDirectionToGoal);
        uint verticalRange = controlBus.Radar.Scan(verticalDirectionToGoal);

        // Pick a direction to move in. If both directions are blocked, stop.
        if (lateralRange > 0)
        {
            controlBus.Motor.Speed = Speed.Moving;
            controlBus.Motor.FacingDirection = lateralDirectionToGoal;
        }
        else if (verticalRange > 0)
        {
            controlBus.Motor.Speed = Speed.Moving;
            controlBus.Motor.FacingDirection = verticalDirectionToGoal;
        }
        else
        {
            controlBus.Motor.Speed = Speed.Stopped; // No movement possible
        }
    }
}

class Application
{
    public static void Main(string[] args)
    {
        /*
         * Challenge 1. Write a Robot brain to navigate the maze using only the sensors provided and locate the Beacon. Avoid the Walls.
         * Challenge 2. Modify the World/Maze Generator so that creating an unsolvable maze is impossible.
         * Challenge 3. Write a new Front End to replace the Console front-end in WFP.
         */
        IRobot myRobot = new ExampleRobot();
        IRoboChallengeRunner runner = new Backend.RoboChallengeRunner();
        
        runner.RunChallenge(myRobot);

    }
}
