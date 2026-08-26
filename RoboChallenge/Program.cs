using RoboChallenge.Abstractions;

namespace RoboChallenge;

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
