namespace RoboChallenge.Backend.Concrete;

using System;
using RoboChallenge.Abstractions;
public class ConsoleWorldVisualiser : IWorldVisualiser
{
    public string GameStatus { get; set; } = string.Empty;

    // Maps a cell to an ANSI foreground color escape code.
    private static string AnsiColor(IWorld.CellContent content) => content switch
    {
        IWorld.CellContent.Wall => "\u001b[90m",  // bright black / dark gray
        IWorld.CellContent.Empty => "\u001b[34m",  // blue
        IWorld.CellContent.Goal => "\u001b[93m",  // bright yellow
        IWorld.CellContent.Robot => "\u001b[92m",  // bright green
        _ => "\u001b[95m",  // bright magenta
    };

    public void Draw(IWorld world)
    {
        // Buffer the entire frame in memory, then write it in one call.
        var sb = new System.Text.StringBuilder((int)((world.WorldWidth + 20) * world.WorldHeight));

        // Home the cursor instead of clearing the screen - avoids the blank flash.
        sb.Append("\u001b[H");

        IWorld.CellContent lastContent = (IWorld.CellContent)(-1); // force first color emit
        for (uint y = 0; y < world.WorldHeight; ++y)
        {
            for (uint x = 0; x < world.WorldWidth; ++x)
            {
                IWorld.CellContent content = world.ScanCell(x, y);
                // Only emit a color escape when the color actually changes - keeps the buffer small.
                if (content != lastContent)
                {
                    sb.Append(AnsiColor(content));
                    lastContent = content;
                }

                char c = content switch
                {
                    IWorld.CellContent.Wall => '#',
                    IWorld.CellContent.Empty => '.',
                    IWorld.CellContent.Goal => '$',
                    IWorld.CellContent.Robot => '@',
                    _ => '?'
                };
                sb.Append(c);
            }
            sb.Append('\n');
        }


        sb.Append("\u001b[0m"); // reset colors
        sb.Append(GameStatus);

        Console.Out.Write(sb.ToString());
    }
}
