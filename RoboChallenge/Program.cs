using RoboChallenge.Abstractions;
using RoboChallenge.Visualizer;

namespace RoboChallenge;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        /*
         * Challenge 1. Write a Robot brain to navigate the maze using only the sensors provided and locate and move to the Beacon. Avoid the Walls.
         * Challenge 2. Write a better World Generator that generates mazes with circular and rectangular features as well as walls. Ensure the maze is always solvable.
         * Challenge 3. Write a new Front End to replace the Console front-end in WPF.
         */
        IRobot myRobot = new ExamRobot();

        IWorldGenerator worldGenerator = new ExamWorldGenerator();
        WpfWorldVisualizer visualiser = new(myRobot);

        IRoboChallengeRunner runner = new Backend.RoboChallengeRunner(worldGenerator, visualiser);
        System.Windows.Application app = new();

        visualiser.Loaded += (_, _) =>
        {
            Task.Run(() =>
            {
                try
                {
                    runner.RunChallenge(myRobot);
                }
                catch (Exception ex)
                {
                    visualiser.GameStatus = $"Simulation error: {ex.Message}";
                }
            });
        };

        app.Run(visualiser);
    }
}
