using RoboChallenge.Abstractions;

namespace RoboChallenge;

public enum RoverState
{
    Exploring,
    Backtracking,
    Complete
}

public class RobotMemory
{
    private enum CellState
    {
        Open,
        Wall
    }

    private static readonly Direction[] MovementDirections =
    [
        Direction.Up,
        Direction.Right,
        Direction.Down,
        Direction.Left
    ];

    private const uint RadarMaxRange = 8;
    private const int PathLengthWeight = 10;
    private const int TraversedNeighbourPenaltyWeight = 7;

    private readonly Dictionary<(int X, int Y), CellState> _knownCells = [];
    private readonly HashSet<(int X, int Y)> _visitedCells = [];
    private readonly Queue<Direction> _plannedRoute = [];

    private RoverState _currentState = RoverState.Exploring;
    private (int X, int Y) _currentPosition = (0, 0);
    private Direction? _pendingMove;
    private bool _lastMoveCollided;
    private bool _foundBeacon;

    public RobotMemory()
    {
        MarkOpen(_currentPosition);
        _visitedCells.Add(_currentPosition);
    }

    public bool hasVisited(int x, int y)
    {
        return _visitedCells.Contains(((int)x, (int)y));
    }

    public void CollidedWithWall()
    {
        if (_pendingMove is not Direction direction)
        {
            return;
        }

        MarkWall(Neighbour(_currentPosition, direction));
        _lastMoveCollided = true;
        _plannedRoute.Clear();
        _currentState = RoverState.Backtracking;
    }

    public void FoundBeacon()
    {
        _foundBeacon = true;
        _plannedRoute.Clear();
        _currentState = RoverState.Complete;
    }

    public void CreateDecision(Direction lateral, Direction vertical, RadarDirections dirs, out RoboIntention intention)
    {
        ReconcilePreviousMove();

        intention = new RoboIntention
        {
            Speed = Speed.Stopped,
            Direction = Direction.Up
        };

        if (_foundBeacon)
        {
            _currentState = RoverState.Complete;
            return;
        }

        ScanVisibleCells(dirs);
        _visitedCells.Add(_currentPosition);

        Direction? nextStep = GetNextPlannedStep(lateral, vertical);
        if (nextStep is null)
        {
            _currentState = RoverState.Complete;
            return;
        }

        _pendingMove = nextStep.Value;
        intention.Speed = Speed.Moving;
        intention.Direction = nextStep.Value;
    }

    private void ReconcilePreviousMove()
    {
        if (_pendingMove is not Direction direction)
        {
            return;
        }

        if (_lastMoveCollided)
        {
            MarkWall(Neighbour(_currentPosition, direction));
            _lastMoveCollided = false;
        }
        else
        {
            _currentPosition = Neighbour(_currentPosition, direction);
            MarkOpen(_currentPosition);
        }

        _pendingMove = null;
    }

    private void ScanVisibleCells(RadarDirections dirs)
    {
        foreach (Direction movementDirection in MovementDirections)
        {
            uint distanceToWall = DistanceForMovementDirection(dirs, movementDirection);

            for (int step = 1; step <= distanceToWall; step++)
            {
                MarkOpen(Offset(_currentPosition, movementDirection, step));
            }

            if (distanceToWall < RadarMaxRange)
            {
                MarkWall(Offset(_currentPosition, movementDirection, (int)distanceToWall + 1));
            }
        }
    }

    private Direction? GetNextPlannedStep(Direction lateral, Direction vertical)
    {
        if (_plannedRoute.Count > 0)
        {
            _currentState = RoverState.Backtracking;
            return _plannedRoute.Dequeue();
        }

        (int X, int Y)? target = ChooseFrontierTarget(lateral, vertical);
        if (target is null)
        {
            return ChooseSafeFallbackMove(lateral, vertical);
        }

        List<Direction> route = FindPathAStar(_currentPosition, target.Value, lateral, vertical);
        if (route.Count == 0)
        {
            return ChooseSafeFallbackMove(lateral, vertical);
        }

        foreach (Direction direction in route.Skip(1))
        {
            _plannedRoute.Enqueue(direction);
        }

        _currentState = route.Count == 1 ? RoverState.Exploring : RoverState.Backtracking;
        return route[0];
    }

    private (int X, int Y)? ChooseFrontierTarget(Direction lateral, Direction vertical)
    {
        (int X, int Y)? bestTarget = null;
        int bestScore = int.MaxValue;

        foreach (var cell in _knownCells)
        {
            if (cell.Value != CellState.Open || _visitedCells.Contains(cell.Key))
            {
                continue;
            }

            List<Direction> path = FindPathAStar(_currentPosition, cell.Key, lateral, vertical);
            if (path.Count == 0)
            {
                continue;
            }

            int score = GetTargetScore(cell.Key, path.Count, lateral, vertical);
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = cell.Key;
            }
        }

        return bestTarget;
    }

    private int GetTargetScore((int X, int Y) target, int pathLength, Direction lateral, Direction vertical)
    {
        return (pathLength * PathLengthWeight)
            + BeaconAlignmentPenalty(target, lateral, vertical)
            + TraversedNeighbourPenalty(target);
    }

    private int TraversedNeighbourPenalty((int X, int Y) target)
    {
        int traversedNeighbours = CountTraversedNeighbours(target);
        return traversedNeighbours * TraversedNeighbourPenaltyWeight;
    }

    private int CountTraversedNeighbours((int X, int Y) target)
    {
        int count = 0;

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                if (_visitedCells.Contains((target.X + dx, target.Y + dy)))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private Direction? ChooseSafeFallbackMove(Direction lateral, Direction vertical)
    {
        List<Direction> safeMoves = MovementDirections
            .Where(direction => IsKnownOpen(Neighbour(_currentPosition, direction)))
            .OrderBy(direction => _visitedCells.Contains(Neighbour(_currentPosition, direction)) ? 10 : 0)
            .ThenBy(direction => DirectionPenalty(direction, lateral, vertical))
            .ToList();

        return safeMoves.Count == 0 ? null : safeMoves[0];
    }

    private List<Direction> FindPathAStar(
        (int X, int Y) start,
        (int X, int Y) target,
        Direction lateral,
        Direction vertical)
    {
        if (start == target)
        {
            return [];
        }

        PriorityQueue<(int X, int Y), int> openSet = new();
        Dictionary<(int X, int Y), (int X, int Y)> cameFrom = [];
        Dictionary<(int X, int Y), int> costSoFar = [];

        openSet.Enqueue(start, 0);
        costSoFar[start] = 0;

        while (openSet.Count > 0)
        {
            (int X, int Y) current = openSet.Dequeue();
            if (current == target)
            {
                return BuildDirectionsFromPath(cameFrom, start, target);
            }

            foreach (Direction direction in OrderedDirections(lateral, vertical))
            {
                (int X, int Y) next = Neighbour(current, direction);
                if (!IsKnownOpen(next))
                {
                    continue;
                }

                int newCost = costSoFar[current] + 1;
                if (!costSoFar.TryGetValue(next, out int existingCost) || newCost < existingCost)
                {
                    costSoFar[next] = newCost;
                    cameFrom[next] = current;

                    int priority = newCost
                        + ManhattanDistance(next, target)
                        + DirectionPenalty(direction, lateral, vertical);

                    openSet.Enqueue(next, priority);
                }
            }
        }

        return [];
    }

    private IEnumerable<Direction> OrderedDirections(Direction lateral, Direction vertical)
    {
        return MovementDirections.OrderBy(direction => DirectionPenalty(direction, lateral, vertical));
    }

    private List<Direction> BuildDirectionsFromPath(
        Dictionary<(int X, int Y), (int X, int Y)> cameFrom,
        (int X, int Y) start,
        (int X, int Y) target)
    {
        Stack<(int X, int Y)> reversedPath = new();
        (int X, int Y) current = target;

        while (current != start)
        {
            reversedPath.Push(current);
            current = cameFrom[current];
        }

        List<Direction> directions = [];
        current = start;

        while (reversedPath.Count > 0)
        {
            (int X, int Y) next = reversedPath.Pop();
            directions.Add(DirectionBetween(current, next));
            current = next;
        }

        return directions;
    }

    private int BeaconAlignmentPenalty((int X, int Y) cell, Direction lateral, Direction vertical)
    {
        int penalty = 0;

        if (!LateralAxisAligned(lateral))
        {
            int dx = cell.X - _currentPosition.X;
            penalty += lateral == Direction.Right
                ? Math.Max(0, -dx) + Math.Abs(dx == 0 ? 1 : 0)
                : Math.Max(0, dx) + Math.Abs(dx == 0 ? 1 : 0);
        }

        if (!VerticalAxisAligned(vertical))
        {
            int dy = cell.Y - _currentPosition.Y;
            penalty += vertical == Direction.Up
                ? Math.Max(0, -dy) + Math.Abs(dy == 0 ? 1 : 0)
                : Math.Max(0, dy) + Math.Abs(dy == 0 ? 1 : 0);
        }

        return penalty * 4;
    }

    private int DirectionPenalty(Direction direction, Direction lateral, Direction vertical)
    {
        if (direction == lateral && !LateralAxisAligned(lateral))
        {
            return 0;
        }

        if (direction == vertical && !VerticalAxisAligned(vertical))
        {
            return 0;
        }

        return 2;
    }

    private static bool LateralAxisAligned(Direction lateral)
    {
        return lateral == Direction.Up;
    }

    private static bool VerticalAxisAligned(Direction vertical)
    {
        return vertical == Direction.Left;
    }

    private static int ManhattanDistance((int X, int Y) a, (int X, int Y) b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    private static uint DistanceForMovementDirection(RadarDirections dirs, Direction movementDirection)
    {
        return movementDirection switch
        {
            Direction.Up => dirs.Down,
            Direction.Down => dirs.Up,
            Direction.Left => dirs.Left,
            Direction.Right => dirs.Right,
            _ => 0
        };
    }

    private static (int X, int Y) Neighbour((int X, int Y) point, Direction direction)
    {
        return Offset(point, direction, 1);
    }

    private static (int X, int Y) Offset((int X, int Y) point, Direction direction, int distance)
    {
        return direction switch
        {
            Direction.Up => (point.X, point.Y + distance),
            Direction.Down => (point.X, point.Y - distance),
            Direction.Left => (point.X - distance, point.Y),
            Direction.Right => (point.X + distance, point.Y),
            _ => point
        };
    }

    private static Direction DirectionBetween((int X, int Y) from, (int X, int Y) to)
    {
        if (to.X > from.X)
        {
            return Direction.Right;
        }

        if (to.X < from.X)
        {
            return Direction.Left;
        }

        if (to.Y > from.Y)
        {
            return Direction.Up;
        }

        return Direction.Down;
    }

    private bool IsKnownOpen((int X, int Y) point)
    {
        return _knownCells.TryGetValue(point, out CellState state) && state == CellState.Open;
    }

    private void MarkOpen((int X, int Y) point)
    {
        _knownCells[point] = CellState.Open;
    }

    private void MarkWall((int X, int Y) point)
    {
        if (point == _currentPosition)
        {
            return;
        }

        _knownCells[point] = CellState.Wall;
    }

    public (int x, int y) getPosition()
    {
        return _currentPosition;
    }

}
