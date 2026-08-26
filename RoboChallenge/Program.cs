using RoboChallenge.Abstractions;

namespace RoboChallenge;

class Application
{
    public static void Main(string[] args)
    {
        /*
         * Challenge 1. Write a Robot brain to navigate the maze using only the sensors provided and locate the Beacon. Avoid the Walls.
         * Challenge 2. Modify the World/Maze Generator so that creating an unsolvable maze is impossible. Also make it stop crashing when we hit the edge of the maze (add walls!)
         * Challenge 3. Write a new Front End to replace the Console front-end in WFP.
         */
        IRobot myRobot = new ExampleRobot();

        IWorldGenerator worldGenerator = new Backend.Concrete.SimpleWorldGenerator();
        IWorldVisualiser visualiser = new Backend.Concrete.ConsoleWorldVisualiser();
        IRoboChallengeRunner runner = new Backend.RoboChallengeRunner(worldGenerator, visualiser);

        runner.RunChallenge(myRobot);

    }
}
