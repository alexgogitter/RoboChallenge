namespace RoboChallenge.Abstractions;

public interface IBeaconScanner
{
    void Detect(out Direction lateralDirectionToBeacon, out Direction verticalDirectionToBeacon);
}
