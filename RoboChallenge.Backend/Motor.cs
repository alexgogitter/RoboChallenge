using RoboChallenge.Abstractions;

namespace RoboChallenge.Backend;

class Motor : IMotor
{
    public Speed Speed { get; set; } = Speed.Stopped;
    public Direction FacingDirection { get; set; } = Direction.Up;
}
