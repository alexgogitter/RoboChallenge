namespace RoboChallenge.Abstractions;

/// <summary>
/// Robot brain - the AI that controls the robot. This is what you will be writing.
/// </summary>
public interface IRobot
{
    /// <summary>
    /// Called routinely. Make a decision about your world and the goal and then act on it. Game is won if (x,y) = the beacon location
    /// </summary>
    /// <param name="controlBus">The interface between the robot brain and the world. Provided by the game.</param>
    void ClockCycle(IRobotChassis controlBus);
    void CollidedWithWall(); // Idiot.
    void FoundBeacon(); // Game over!
}
