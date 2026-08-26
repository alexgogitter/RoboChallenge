namespace RoboChallenge.Abstractions;

public interface IWorldVisualiser
{
    void Draw(IWorld world);
    string GameStatus { get; set; }
}
