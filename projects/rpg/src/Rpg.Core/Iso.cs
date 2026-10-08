namespace Rpg.Core;

/// <summary>
/// Isometric projection of the board: cell (x, y) is a diamond twice as wide as it is high,
/// centred at ((x - y) * w/2, (x + y) * h/2). The client draws with it and turns a click back
/// into a cell; keeping it here lets both directions be tested without the engine.
/// </summary>
public static class Iso
{
    /// <summary>Screen position of the centre of <paramref name="cell"/>, for diamonds of the given size.</summary>
    public static (double X, double Y) ToScreen(Cell cell, double tileWidth, double tileHeight) =>
        ((cell.X - cell.Y) * tileWidth / 2, (cell.X + cell.Y) * tileHeight / 2);

    /// <summary>The cell whose diamond contains the screen point (the inverse of <see cref="ToScreen"/>).</summary>
    public static Cell FromScreen(double x, double y, double tileWidth, double tileHeight)
    {
        // In half diagonals, a = x' - y' and b = x' + y' for the board coordinates (x', y'); each
        // diamond is the unit square around its cell there, hence the rounding to the nearest.
        double a = x / (tileWidth / 2);
        double b = y / (tileHeight / 2);
        return new Cell((int)Math.Floor((a + b) / 2 + 0.5), (int)Math.Floor((b - a) / 2 + 0.5));
    }
}
