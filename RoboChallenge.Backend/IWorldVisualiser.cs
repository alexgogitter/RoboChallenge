namespace RoboChallenge.Backend;

interface IWorldVisualiser
{
    void Draw(IWorld world);
    string GameStatus { get; set; }
}
