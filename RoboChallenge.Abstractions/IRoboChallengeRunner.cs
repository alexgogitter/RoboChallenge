namespace RoboChallenge.Abstractions;

public interface IRoboChallengeRunner
{
    /// <summary>
    /// Runs the challenge. Can your robot find the Beacon and avoid the walls? This is where the robot's AI will be tested.
    /// </summary>
    /// <param name="robot">Robot brain - the AI that controls the robot. This is what you will be writing.</param>
    void RunChallenge(IRobot robot);
}
