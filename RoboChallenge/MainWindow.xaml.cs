using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RoboChallenge.Abstractions;

namespace RoboChallenge.Visualizer
{
    public partial class WpfWorldVisualizer : Window, IWorldVisualiser
    {
        private IWorld? _world;
        private readonly IRobot _robot;
        private WriteableBitmap? _bitmap;
        private readonly int _cellSize = 10;
        private bool _showExploration;

        private (uint x, uint y) robotInitPosition; // Initial position of the robot in the world
        private bool hasRobotInitPosition;

        // Colors
        private static readonly Color WallColor = Colors.DarkGray;
        private static readonly Color EmptyColor = Colors.LightBlue;
        private static readonly Color GoalColor = Colors.Gold;
        private static readonly Color RobotColor = Colors.LimeGreen;
        private static readonly Color UnknownColor = Colors.DimGray;

        private string _gameStatus = string.Empty;
        public string GameStatus
        {
            get => _gameStatus;
            set
            {
                _gameStatus = value;
                RunOnUiThread(() => UpdateStatusText());
            }
        }

        public WpfWorldVisualizer(IRobot robot)
        {
            InitializeComponent();
            _robot = robot;
            

            ExplorationToggle.Checked += (s, e) => { _showExploration = true; Render(); };
            ExplorationToggle.Unchecked += (s, e) => { _showExploration = false; Render(); };

            this.Loaded += (s, e) => Render();
        }

        /// <summary>
        /// Call this after constructing the window to display it.
        /// </summary>
        public void ShowWindow()
        {
            RunOnUiThread(Show);
        }

        /// <summary>
        /// IWorldVisualiser.Draw – called from simulation loop.
        /// </summary>
        public void Draw(IWorld world)
        {
            if (!hasRobotInitPosition)
            {
                robotInitPosition = (world.RobotX, world.RobotY);
                hasRobotInitPosition = true;
            }
            _world = world;
            RunOnUiThread(Render);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e) => Render();

        private void Render()
        {
            if (_world == null) return;

            uint w = _world.WorldWidth;
            uint h = _world.WorldHeight;
            int width = (int)w * _cellSize;
            int height = (int)h * _cellSize;

            // Create bitmap if needed
            if (_bitmap == null || _bitmap.PixelWidth != width || _bitmap.PixelHeight != height)
            {
                _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                WorldImage.Source = _bitmap;
            }

            int stride = width * 4;
            byte[] pixels = new byte[height * stride];

            for (uint y = 0; y < h; y++)
            {
                for (uint x = 0; x < w; x++)
                {
                    Color color = GetCellColor(x, y);
                    for (int dy = 0; dy < _cellSize; dy++)
                    {
                        int row = (int)(y * _cellSize + dy);
                        for (int dx = 0; dx < _cellSize; dx++)
                        {
                            int col = (int)(x * _cellSize + dx);
                            int idx = row * stride + col * 4;
                            pixels[idx] = color.B;
                            pixels[idx + 1] = color.G;
                            pixels[idx + 2] = color.R;
                            pixels[idx + 3] = 255;
                        }
                    }
                }
            }

            _bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            UpdateStatusText();
        }

        private Color GetCellColor(uint x, uint y)
        {
            IWorld.CellContent content = _world!.ScanCell(x, y);

            if (!_showExploration)
                return ContentToColor(content);

            if (content == IWorld.CellContent.Robot || content == IWorld.CellContent.Goal)
            {
                return ContentToColor(content);
            }

            int memoryX = (int)x - (int)robotInitPosition.x;
            int memoryY = (int)y - (int)robotInitPosition.y;
            bool visited = _robot.hasVisited(memoryX, memoryY);
            return visited ? ContentToColor(content) : UnknownColor;
        }

        private void UpdateStatusText()
        {
            if (_world == null)
            {
                StatusText.Text = string.IsNullOrWhiteSpace(_gameStatus) ? "Ready" : _gameStatus;
                return;
            }

            StatusText.Text = $"World: {_world.WorldWidth}x{_world.WorldHeight}   |   Explorer: {(_showExploration ? "ON" : "OFF")}   |   {_gameStatus}";
        }

        private void RunOnUiThread(Action action)
        {
            if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
            {
                return;
            }

            if (Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                Dispatcher.BeginInvoke(action);
            }
        }

        private static Color ContentToColor(IWorld.CellContent content)
        {
            return content switch
            {
                IWorld.CellContent.Wall => WallColor,
                IWorld.CellContent.Empty => EmptyColor,
                IWorld.CellContent.Goal => GoalColor,
                IWorld.CellContent.Robot => RobotColor,
                _ => Colors.Magenta
            };
        }
    }
}
