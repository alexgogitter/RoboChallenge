using System;
using System.Collections.Generic;
using System.Linq;
using RoboChallenge.Abstractions;

namespace RoboChallenge
{
    class ExamWorldGenerator : IWorldGenerator
    {
        // ---------- Configuration ----------
        private readonly uint _worldWidth;
        private readonly uint _worldHeight;
        private readonly double _mesaDensity;
        private readonly double _craterDensity;
        private readonly double _averageDegree;
        private readonly double _degreeDeviation;

        // Tunable parameters
        private const int MinCraterRadius = 9;
        private const int MaxCraterRadius = 12;
        private const int MinMesaSize = 6;
        private const int MaxMesaSize = 10;
        private const int MinValleyWidth = 8;
        private const int MaxValleyWidth = 12;
        private const int Border = 5;
        private const double MinDistanceFraction = 0.5;

        private readonly Random _random = new Random(); //13, 14, 15

        // Internal data
        private IWorld.CellContent[,] _grid;
        private List<MapFeature> _features;
        private List<Tuple<MapFeature, MapFeature>> _edges;

        // ---------- Feature representation ----------
        private class MapFeature
        {
            public int X { get; set; }
            public int Y { get; set; }
            public enum FeatureType { Crater, Mesa }
            public FeatureType Type { get; set; }
            // For craters
            public int Radius { get; set; }
            // For mesas (after rotation/skew, these are the original extents)
            public int Width { get; set; }
            public int Height { get; set; }
            public double Rotation { get; set; }   // in radians
            public double SkewX { get; set; }      // shear factor
            public double SkewY { get; set; }
        }

        // ---------- Constructor ----------
        public ExamWorldGenerator(uint worldWidth = 160, uint worldHeight = 90,
            double mesaDensity = 0.6, double craterDensity = 0.4,
            double averageDegree = 0.2, double degreeDeviation = 0.1)
        {
            _worldWidth = worldWidth;
            _worldHeight = worldHeight;
            _mesaDensity = mesaDensity;
            _craterDensity = craterDensity;
            _averageDegree = averageDegree;
            _degreeDeviation = degreeDeviation;
            
        }

        // ---------- Main generation ----------
        public IWorld.CellContent[,] GenerateWorld(out uint worldWidth, out uint worldHeight,
            out uint goalX, out uint goalY, out uint robotX, out uint robotY)
        {
            worldWidth = _worldWidth;
            worldHeight = _worldHeight;

            InitializeGrid();
            GenerateCraters();
            GenerateMesas();
            GenerateValleys();
            PlaceAlienStructures();
            PlaceRobotAndBeacon(out robotX, out robotY, out goalX, out goalY);

            return _grid;
        }

        // ---------- Phase methods ----------
        private void InitializeGrid()
        {
            _grid = new IWorld.CellContent[_worldWidth, _worldHeight];
            for (int x = 0; x < _worldWidth; x++)
                for (int y = 0; y < _worldHeight; y++)
                    _grid[x, y] = IWorld.CellContent.Wall;

            _features = new List<MapFeature>();
            _edges = new List<Tuple<MapFeature, MapFeature>>();
        }

        private void GenerateCraters()
        {
            int numCraters = (int)(_craterDensity * 10) + _random.Next(4, 10);
            numCraters = Math.Max(3, Math.Min(20, numCraters));

            for (int i = 0; i < numCraters; i++)
            {
                MapFeature crater = null;
                int attempts = 0;
                while (attempts < 50 && crater == null)
                {
                    int cx = _random.Next(Border, (int)_worldWidth - Border);
                    int cy = _random.Next(Border, (int)_worldHeight - Border);
                    int radius = _random.Next(MinCraterRadius, MaxCraterRadius + 1);

                    if (cx - radius >= 0 && cx + radius < _worldWidth &&
                        cy - radius >= 0 && cy + radius < _worldHeight)
                    {
                        crater = new MapFeature
                        {
                            X = cx,
                            Y = cy,
                            Type = MapFeature.FeatureType.Crater,
                            Radius = radius
                        };
                    }
                    attempts++;
                }
                if (crater != null)
                    _features.Add(crater);
            }

            // Guarantee at least 3 craters
            while (_features.Count(f => f.Type == MapFeature.FeatureType.Crater) < 3)
            {
                int cx = _random.Next(Border, (int)_worldWidth - Border);
                int cy = _random.Next(Border, (int)_worldHeight - Border);
                int radius = _random.Next(MinCraterRadius, MaxCraterRadius + 1);
                if (cx - radius >= 0 && cx + radius < _worldWidth &&
                    cy - radius >= 0 && cy + radius < _worldHeight)
                {
                    _features.Add(new MapFeature
                    {
                        X = cx,
                        Y = cy,
                        Type = MapFeature.FeatureType.Crater,
                        Radius = radius
                    });
                }
            }

            // Draw craters
            foreach (var feature in _features.Where(f => f.Type == MapFeature.FeatureType.Crater))
                DrawCircle(feature.X, feature.Y, feature.Radius, IWorld.CellContent.Empty);
        }

        private void GenerateMesas()
        {
            int numMesas = (int)(_mesaDensity * 8) + _random.Next(3, 6);
            numMesas = Math.Max(2, Math.Min(10, numMesas)); // at least 2 mesas

            for (int i = 0; i < numMesas; i++)
            {
                int width = _random.Next(MinMesaSize, MaxMesaSize + 1);
                int height = _random.Next(MinMesaSize, MaxMesaSize + 1);
                double rotation = _random.NextDouble() * Math.PI * 2; // 0..360°
                double skewX = (_random.NextDouble() - 0.5) * 0.8;    // -0.4 .. 0.4
                double skewY = (_random.NextDouble() - 0.5) * 0.8;

                bool placed = false;
                int attempts = 0;
                while (attempts < 30 && !placed)
                {
                    int cx = _random.Next(Border, (int)_worldWidth - Border);
                    int cy = _random.Next(Border, (int)_worldHeight - Border);

                    // Estimate bounding box after transformation to check fit
                    var points = GetMesaCorners(cx, cy, width, height, rotation, skewX, skewY);
                    bool fits = true;
                    foreach (var p in points)
                        if (p.Item1 < 0 || p.Item1 >= _worldWidth || p.Item2 < 0 || p.Item2 >= _worldHeight)
                            fits = false;

                    if (fits)
                    {
                        var mesa = new MapFeature
                        {
                            X = cx,
                            Y = cy,
                            Type = MapFeature.FeatureType.Mesa,
                            Width = width,
                            Height = height,
                            Rotation = rotation,
                            SkewX = skewX,
                            SkewY = skewY
                        };
                        _features.Add(mesa);
                        placed = true;
                    }
                    attempts++;
                }

                // If we couldn't place it, place a simple unrotated one
                if (!placed)
                {
                    int cx = _random.Next(Border, (int)_worldWidth - Border);
                    int cy = _random.Next(Border, (int)_worldHeight - Border);
                    if (cx + width < _worldWidth && cy + height < _worldHeight)
                    {
                        _features.Add(new MapFeature
                        {
                            X = cx,
                            Y = cy,
                            Type = MapFeature.FeatureType.Mesa,
                            Width = width,
                            Height = height,
                            Rotation = 0,
                            SkewX = 0,
                            SkewY = 0
                        });
                    }
                }
            }

            // Draw all mesas (as transformed polygons)
            foreach (var feature in _features.Where(f => f.Type == MapFeature.FeatureType.Mesa))
            {
                var corners = GetMesaCorners(feature.X, feature.Y, feature.Width, feature.Height,
                                             feature.Rotation, feature.SkewX, feature.SkewY);
                DrawFilledPolygon(corners, IWorld.CellContent.Empty);
            }
        }

        // Returns the four corners of a transformed rectangle (center‑based)
        private List<Tuple<double, double>> GetMesaCorners(int cx, int cy, int w, int h,
                                                           double rot, double skewX, double skewY)
        {
            // Half dimensions
            double hw = w / 2.0;
            double hh = h / 2.0;
            // Define the four corners in local coordinates (centered at 0,0)
            var local = new List<Tuple<double, double>>
            {
                Tuple.Create(-hw, -hh),
                Tuple.Create( hw, -hh),
                Tuple.Create( hw,  hh),
                Tuple.Create(-hw,  hh)
            };

            // Apply skew (shear) first
            var sheared = local.Select(p => Tuple.Create(
                p.Item1 + skewX * p.Item2,
                p.Item2 + skewY * p.Item1
            )).ToList();

            // Then rotate
            double cos = Math.Cos(rot);
            double sin = Math.Sin(rot);
            var rotated = sheared.Select(p => Tuple.Create(
                p.Item1 * cos - p.Item2 * sin,
                p.Item1 * sin + p.Item2 * cos
            )).ToList();

            // Translate to world coordinates
            return rotated.Select(p => Tuple.Create(p.Item1 + cx, p.Item2 + cy)).ToList();
        }

        // Draw a filled convex polygon (using scanline)
        private void DrawFilledPolygon(List<Tuple<double, double>> vertices, IWorld.CellContent value)
        {
            if (vertices.Count < 3) return;

            // Convert to integer points (rounding)
            var pts = vertices.Select(p => Tuple.Create((int)Math.Round(p.Item1), (int)Math.Round(p.Item2))).ToList();

            // Find bounding box
            int minX = pts.Min(p => p.Item1);
            int maxX = pts.Max(p => p.Item1);
            int minY = pts.Min(p => p.Item2);
            int maxY = pts.Max(p => p.Item2);

            // Clip to grid
            minX = Math.Max(0, minX);
            maxX = Math.Min((int)_worldWidth - 1, maxX);
            minY = Math.Max(0, minY);
            maxY = Math.Min((int)_worldHeight - 1, maxY);

            // For each scanline, find intersections with polygon edges
            for (int y = minY; y <= maxY; y++)
            {
                var intersections = new List<int>();
                for (int i = 0; i < pts.Count; i++)
                {
                    int j = (i + 1) % pts.Count;
                    var p1 = pts[i];
                    var p2 = pts[j];
                    // Check if edge crosses this y
                    if ((p1.Item2 <= y && p2.Item2 > y) || (p2.Item2 <= y && p1.Item2 > y))
                    {
                        double x = p1.Item1 + (double)(y - p1.Item2) * (p2.Item1 - p1.Item1) / (p2.Item2 - p1.Item2);
                        intersections.Add((int)Math.Round(x));
                    }
                }
                intersections.Sort();
                for (int k = 0; k < intersections.Count - 1; k += 2)
                {
                    int x1 = Math.Max(0, intersections[k]);
                    int x2 = Math.Min((int)_worldWidth - 1, intersections[k + 1]);
                    for (int x = x1; x <= x2; x++)
                        _grid[x, y] = value;
                }
            }
        }

        private void GenerateValleys()
        {
            if (_features.Count < 2) return;

            var distanceDict = new Dictionary<Tuple<MapFeature, MapFeature>, double>();
            for (int i = 0; i < _features.Count; i++)
                for (int j = i + 1; j < _features.Count; j++)
                {
                    double dx = _features[i].X - _features[j].X;
                    double dy = _features[i].Y - _features[j].Y;
                    distanceDict[Tuple.Create(_features[i], _features[j])] = Math.Sqrt(dx * dx + dy * dy);
                }

            var sortedEdges = distanceDict.OrderBy(kvp => kvp.Value).ToList();

            var disjointSet = new DisjointSet(_features);
            var mstEdges = new List<Tuple<MapFeature, MapFeature>>();
            foreach (var kvp in sortedEdges)
            {
                var edge = kvp.Key;
                if (disjointSet.Find(edge.Item1) != disjointSet.Find(edge.Item2))
                {
                    disjointSet.Union(edge.Item1, edge.Item2);
                    mstEdges.Add(edge);
                }
            }

            _edges.AddRange(mstEdges);

            int desiredDegree = (int)(_averageDegree + _random.NextDouble() * _degreeDeviation * 2 - _degreeDeviation);
            desiredDegree = Math.Max(2, Math.Min(_features.Count - 1, desiredDegree));

            var degrees = _features.ToDictionary(f => f, f => 0);
            foreach (var edge in _edges)
            {
                degrees[edge.Item1]++;
                degrees[edge.Item2]++;
            }

            int edgeIndex = 0;
            while (true)
            {
                double currentAvg = degrees.Values.Average();
                if (currentAvg >= desiredDegree || edgeIndex >= sortedEdges.Count)
                    break;

                var candidate = sortedEdges[edgeIndex].Key;
                if (!_edges.Any(e => (e.Item1 == candidate.Item1 && e.Item2 == candidate.Item2) ||
                                     (e.Item1 == candidate.Item2 && e.Item2 == candidate.Item1)))
                {
                    _edges.Add(candidate);
                    degrees[candidate.Item1]++;
                    degrees[candidate.Item2]++;
                }
                edgeIndex++;
            }

            foreach (var edge in _edges)
            {
                int width = _random.Next(MinValleyWidth, MaxValleyWidth + 1);
                DrawCorridor(edge.Item1.X, edge.Item1.Y, edge.Item2.X, edge.Item2.Y, width, IWorld.CellContent.Empty);
            }
        }

        private void PlaceAlienStructures()
        {
            var traversableCells = new List<Tuple<int, int>>();
            for (int x = 0; x < _worldWidth; x++)
                for (int y = 0; y < _worldHeight; y++)
                    if (_grid[x, y] == IWorld.CellContent.Empty)
                        traversableCells.Add(Tuple.Create(x, y));

            int numStructures = _random.Next(3, 9);
            for (int s = 0; s < numStructures && traversableCells.Count > 10; s++)
            {
                // Randomly choose between a line (wall) or a small rectangle
                if (_random.NextDouble() < 0.4) // 70% line, 30% rectangle
                {
                    // Line with thickness (diagonal walls)
                    int idx1 = _random.Next(traversableCells.Count);
                    int idx2 = _random.Next(traversableCells.Count);
                    var start = traversableCells[idx1];
                    var end = traversableCells[idx2];
                    int thickness = _random.Next(1, 4);

                    // Check if all points are Empty
                    bool allEmpty = true;
                    foreach (var point in GetThickLinePoints(start.Item1, start.Item2, end.Item1, end.Item2, thickness))
                    {
                        if (point.Item1 < 0 || point.Item1 >= _worldWidth ||
                            point.Item2 < 0 || point.Item2 >= _worldHeight ||
                            _grid[point.Item1, point.Item2] != IWorld.CellContent.Empty)
                        {
                            allEmpty = false;
                            break;
                        }
                    }

                    if (allEmpty)
                    {
                        foreach (var point in GetThickLinePoints(start.Item1, start.Item2, end.Item1, end.Item2, thickness))
                            _grid[point.Item1, point.Item2] = IWorld.CellContent.Wall;
                    }
                }
                else
                {
                    // Small rectangle wall
                    int w = _random.Next(2, 5);
                    int h = _random.Next(2, 5);
                    int idx = _random.Next(traversableCells.Count);
                    var center = traversableCells[idx];
                    int x1 = center.Item1 - w / 2;
                    int y1 = center.Item2 - h / 2;
                    bool allEmpty = true;
                    for (int dx = 0; dx < w; dx++)
                        for (int dy = 0; dy < h; dy++)
                        {
                            int px = x1 + dx;
                            int py = y1 + dy;
                            if (px < 0 || px >= _worldWidth || py < 0 || py >= _worldHeight ||
                                _grid[px, py] != IWorld.CellContent.Empty)
                                allEmpty = false;
                        }
                    if (allEmpty)
                    {
                        for (int dx = 0; dx < w; dx++)
                            for (int dy = 0; dy < h; dy++)
                                _grid[x1 + dx, y1 + dy] = IWorld.CellContent.Wall;
                    }
                }
            }
        }

        // Returns points of a line with a given thickness (perpendicular offset)
        private List<Tuple<int, int>> GetThickLinePoints(int x1, int y1, int x2, int y2, int thickness)
        {
            var points = new List<Tuple<int, int>>();
            var line = GetLinePoints(x1, y1, x2, y2);
            double dx = x2 - x1;
            double dy = y2 - y1;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.001) return line;
            double nx = -dy / len;
            double ny = dx / len;

            foreach (var p in line)
            {
                for (int w = -thickness / 2; w <= thickness / 2; w++)
                {
                    int px = (int)Math.Round(p.Item1 + nx * w);
                    int py = (int)Math.Round(p.Item2 + ny * w);
                    points.Add(Tuple.Create(px, py));
                }
            }
            return points.Distinct().ToList();
        }

        private void PlaceRobotAndBeacon(out uint robotX, out uint robotY, out uint goalX, out uint goalY)
        {
            var components = FindComponents(IWorld.CellContent.Empty);
            var largest = components.OrderByDescending(c => c.Count).FirstOrDefault();

            if (largest == null || largest.Count < 2)
            {
                robotX = 1; robotY = 1;
                goalX = 2; goalY = 1;
                _grid[robotX, robotY] = IWorld.CellContent.Robot;
                _grid[goalX, goalY] = IWorld.CellContent.Goal;
                return;
            }

            double mapDiagonal = Math.Sqrt(_worldWidth * _worldWidth + _worldHeight * _worldHeight);
            double minDist = mapDiagonal * MinDistanceFraction;

            for (int attempt = 0; attempt < 5; attempt++)
            {
                int robotIdx = _random.Next(largest.Count);
                var robotPos = largest[robotIdx];

                var candidates = new List<Tuple<int, int>>();
                foreach (var cell in largest)
                {
                    double dx = cell.Item1 - robotPos.Item1;
                    double dy = cell.Item2 - robotPos.Item2;
                    if (dx * dx + dy * dy >= minDist * minDist)
                        candidates.Add(cell);
                }

                if (candidates.Count > 0)
                {
                    int beaconIdx = _random.Next(candidates.Count);
                    var beaconPos = candidates[beaconIdx];

                    robotX = (uint)robotPos.Item1;
                    robotY = (uint)robotPos.Item2;
                    goalX = (uint)beaconPos.Item1;
                    goalY = (uint)beaconPos.Item2;

                    _grid[robotX, robotY] = IWorld.CellContent.Robot;
                    _grid[goalX, goalY] = IWorld.CellContent.Goal;
                    return;
                }
                minDist *= 0.8;
            }

            // Fallback: two different cells
            int fallbackRobotIdx = _random.Next(largest.Count);
            int fallbackBeaconIdx = _random.Next(largest.Count);
            while (fallbackBeaconIdx == fallbackRobotIdx && largest.Count > 1)
                fallbackBeaconIdx = _random.Next(largest.Count);

            var fallbackRobot = largest[fallbackRobotIdx];
            var fallbackBeacon = largest[fallbackBeaconIdx];
            robotX = (uint)fallbackRobot.Item1;
            robotY = (uint)fallbackRobot.Item2;
            goalX = (uint)fallbackBeacon.Item1;
            goalY = (uint)fallbackBeacon.Item2;

            _grid[robotX, robotY] = IWorld.CellContent.Robot;
            _grid[goalX, goalY] = IWorld.CellContent.Goal;
        }

        // ---------- Drawing helpers (unchanged) ----------
        private void DrawCircle(int cx, int cy, int radius, IWorld.CellContent value)
        {
            for (int y = -radius; y <= radius; y++)
            {
                int dx = (int)Math.Sqrt(radius * radius - y * y);
                for (int x = -dx; x <= dx; x++)
                {
                    int px = cx + x;
                    int py = cy + y;
                    if (px >= 0 && px < _worldWidth && py >= 0 && py < _worldHeight)
                        _grid[px, py] = value;
                }
            }
        }

        private void DrawCorridor(int x1, int y1, int x2, int y2, int width, IWorld.CellContent value)
        {
            var points = GetLinePoints(x1, y1, x2, y2);
            foreach (var p in points)
            {
                double dx = x2 - x1;
                double dy = y2 - y1;
                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len < 0.001) continue;
                double nx = -dy / len;
                double ny = dx / len;

                for (int w = -width / 2; w <= width / 2; w++)
                {
                    int px = (int)Math.Round(p.Item1 + nx * w);
                    int py = (int)Math.Round(p.Item2 + ny * w);
                    if (px >= 0 && px < _worldWidth && py >= 0 && py < _worldHeight)
                        _grid[px, py] = value;
                }
            }
        }

        private List<Tuple<int, int>> GetLinePoints(int x1, int y1, int x2, int y2)
        {
            var points = new List<Tuple<int, int>>();
            int dx = Math.Abs(x2 - x1), sx = x1 < x2 ? 1 : -1;
            int dy = -Math.Abs(y2 - y1), sy = y1 < y2 ? 1 : -1;
            int err = dx + dy, e2;
            while (true)
            {
                points.Add(Tuple.Create(x1, y1));
                if (x1 == x2 && y1 == y2) break;
                e2 = 2 * err;
                if (e2 >= dy) { err += dy; x1 += sx; }
                if (e2 <= dx) { err += dx; y1 += sy; }
            }
            return points;
        }

        private List<List<Tuple<int, int>>> FindComponents(IWorld.CellContent target)
        {
            int width = (int)_worldWidth;
            int height = (int)_worldHeight;
            var visited = new bool[width, height];
            var components = new List<List<Tuple<int, int>>>();

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (!visited[x, y] && _grid[x, y] == target)
                    {
                        var comp = new List<Tuple<int, int>>();
                        var queue = new Queue<Tuple<int, int>>();
                        queue.Enqueue(Tuple.Create(x, y));
                        visited[x, y] = true;
                        while (queue.Count > 0)
                        {
                            var p = queue.Dequeue();
                            comp.Add(p);
                            foreach (var dir in new[] { Tuple.Create(1, 0), Tuple.Create(-1, 0), Tuple.Create(0, 1), Tuple.Create(0, -1) })
                            {
                                int nx = p.Item1 + dir.Item1;
                                int ny = p.Item2 + dir.Item2;
                                if (nx >= 0 && nx < width && ny >= 0 && ny < height &&
                                    !visited[nx, ny] && _grid[nx, ny] == target)
                                {
                                    visited[nx, ny] = true;
                                    queue.Enqueue(Tuple.Create(nx, ny));
                                }
                            }
                        }
                        components.Add(comp);
                    }
                }
            return components;
        }

        // ---------- Disjoint Set (Union-Find) ----------
        private class DisjointSet
        {
            private readonly Dictionary<MapFeature, MapFeature> _parent = new Dictionary<MapFeature, MapFeature>();
            private readonly Dictionary<MapFeature, int> _rank = new Dictionary<MapFeature, int>();

            public DisjointSet(List<MapFeature> nodes)
            {
                foreach (var node in nodes)
                {
                    _parent[node] = node;
                    _rank[node] = 0;
                }
            }

            public MapFeature Find(MapFeature node)
            {
                if (_parent[node] != node)
                    _parent[node] = Find(_parent[node]);
                return _parent[node];
            }

            public void Union(MapFeature a, MapFeature b)
            {
                MapFeature ra = Find(a);
                MapFeature rb = Find(b);
                if (ra == rb) return;
                if (_rank[ra] < _rank[rb])
                    _parent[ra] = rb;
                else if (_rank[ra] > _rank[rb])
                    _parent[rb] = ra;
                else
                {
                    _parent[rb] = ra;
                    _rank[ra]++;
                }
            }
        }
    }
}