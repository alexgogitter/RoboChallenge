using RoboChallenge.Abstractions;

namespace RoboChallenge;

class ExampleRobot : IRobot
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
        uint xRangeToWall = controlBus.Radar.Scan(lateralDirectionToGoal);
        uint yRangeToWall = controlBus.Radar.Scan(verticalDirectionToGoal);

        // Pick a direction to move in. If both directions are blocked, stop.
        if (xRangeToWall > 0)
        {
            controlBus.Motor.Speed = Speed.Moving;
            controlBus.Motor.FacingDirection = lateralDirectionToGoal;
        }
        else if (yRangeToWall > 0)
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
