using RoboChallenge.Abstractions;

namespace RoboChallenge;

class ExampleRobot : IRobot
{
    public void CollidedWithWall()
    {
        // Oops! We hit a wall. Stop moving and try a different direction next time.
        System.Console.WriteLine($"Ouch!");
    }
    public void FoundBeacon()
    {
        // Game over!
        System.Console.WriteLine($"I found the beacon!");
    }
    public void ClockCycle(IRobotChassis controlBus)
    {
        // TODO: use the controlbus->scanner to determine the direction to the goal
        // TODO: use the controlbus->radar to determine if there are walls in the way
        // TODO: make a decision about which direction to move in, and set the controlbus->motor accordingly
        // TODO: maybe remember where you've been and what you've seen so you can backtrack if necessary

        // For now, just move the robot to the right.

        controlBus.Motor.Speed = Speed.Moving;
        controlBus.Motor.FacingDirection = Direction.Right;        


    }

    public bool hasVisited(int x, int y)
    {
        throw new NotImplementedException();
    }
}
