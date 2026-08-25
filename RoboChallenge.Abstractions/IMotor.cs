namespace RoboChallenge.Abstractions;

public interface IMotor
{
    Speed Speed { get; set; }
    Direction FacingDirection { get; set; }
}
