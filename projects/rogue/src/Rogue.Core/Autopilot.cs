namespace Rogue.Core;

/// <summary>
/// A simple player used for demonstrations and tests: it drinks when hurt, fights what stands next
/// to it, rests when hurt and alone, picks up what it has seen, explores, then takes the stairs.
/// It only uses what the player can know (explored tiles, visible monsters) and is deterministic.
/// </summary>
public static class Autopilot
{
    public static GameAction Choose(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        Player player = game.Player;
        if (player.Hp * 3 <= player.MaxHp && player.Potions > 0)
            return GameAction.Drink;
        foreach (Direction d in Directions.All)
            if (game.MonsterAt(player.Position.Step(d)) is not null)
                return ActionCodec.Move(d);
        bool threatened = game.Monsters.Any(m => game.IsVisible(m.Position));
        if (!threatened && player.Hp * 2 < player.MaxHp)
            return GameAction.Wait;

        Map map = game.Map;
        int[] distances = map.Distances(player.Position, map.IsExplored);
        Point? target = Nearest(map, distances, p => game.Items.ContainsKey(p))
            ?? Nearest(map, distances, p => Directions.All.Any(d => map.InBounds(p.Step(d)) && !map.IsExplored(p.Step(d))));
        if (target is null)
        {
            if (player.Position == game.Stairs)
                return GameAction.Descend;
            target = game.Stairs;
        }
        return FirstStep(map, player.Position, target.Value);
    }

    private static Point? Nearest(Map map, int[] distances, Func<Point, bool> wanted)
    {
        Point? best = null;
        int bestDistance = int.MaxValue;
        foreach (Point p in map.Points())
        {
            int d = distances[map.Index(p)];
            if (d > 0 && d < bestDistance && wanted(p))
            {
                best = p;
                bestDistance = d;
            }
        }
        return best;
    }

    /// <summary>The first move of a shortest path through explored tiles (monsters in the way get attacked).</summary>
    private static GameAction FirstStep(Map map, Point from, Point to)
    {
        int[] fromTarget = map.Distances(to, map.IsExplored);
        foreach (Direction d in Directions.All)
        {
            Point next = from.Step(d);
            int there = map.InBounds(next) ? fromTarget[map.Index(next)] : -1;
            if (there >= 0 && there < fromTarget[map.Index(from)])
                return ActionCodec.Move(d);
        }
        return GameAction.Wait;
    }
}
