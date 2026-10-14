using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The world map, over the map the player is on: since sprint 70, one small square per map in its place
/// (<see cref="Town.MapAt"/>), the zones each in their colour with their name and level over them, the
/// map the player is on outlined. M or its icon opens and closes it. (The world seen from the sky is
/// sprint 71's.)
/// </summary>
public partial class WorldMapPanel : CanvasLayer
{
    private readonly Dictionary<string, Control> _maps = [];

    public string Current { get; private set; } = "";

    public void Build(GameData data, string current, Texts texts)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(texts);
        Current = current;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -420, OffsetRight = 420, OffsetTop = -260, OffsetBottom = 260 };
        panel.AddThemeStyleboxOverride("panel", InventoryPanel.Background());
        AddChild(panel);
        var area = new Control { CustomMinimumSize = new Vector2(800, 480) };
        panel.AddChild(area);
        var title = new Label { Text = texts["map.title"], Position = new Vector2(18, 10) };
        title.AddThemeFontSizeOverride("font_size", 24);
        area.AddChild(title);
        // The grid of maps, as large as fits under the title, centred.
        int minX = data.Towns.Values.Min(t => t.MapAt.X), minY = data.Towns.Values.Min(t => t.MapAt.Y);
        int columns = data.Towns.Values.Max(t => t.MapAt.X) - minX + 1, rows = data.Towns.Values.Max(t => t.MapAt.Y) - minY + 1;
        float step = Math.Min(760f / columns, 400f / rows);
        var corner = new Vector2(400 - columns * step / 2, 270 - rows * step / 2);
        Vector2 At(Town t) => corner + new Vector2(t.MapAt.X - minX, t.MapAt.Y - minY) * step;
        string[] zones = [.. data.Towns.Values.Select(t => t.ZoneId).Distinct().Order(StringComparer.Ordinal)];
        foreach (Town t in data.Towns.Values)
        {
            bool here = t.Id == current;
            Color colour = Color.FromHsv((float)Array.IndexOf(zones, t.ZoneId) / zones.Length, 0.35f, 0.45f);
            var square = new Panel { Position = At(t) + new Vector2(2, 2), Size = new Vector2(step - 4, step - 4), MouseFilter = Control.MouseFilterEnum.Ignore };
            square.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = colour,
                BorderColor = here ? FighterView.PlayerColour : new Color(1, 1, 1, 0.2f),
                BorderWidthLeft = here ? 3 : 1,
                BorderWidthRight = here ? 3 : 1,
                BorderWidthTop = here ? 3 : 1,
                BorderWidthBottom = here ? 3 : 1,
                CornerRadiusTopLeft = 3,
                CornerRadiusTopRight = 3,
                CornerRadiusBottomLeft = 3,
                CornerRadiusBottomRight = 3,
            });
            area.AddChild(square);
            _maps[t.Id] = square;
        }
        // Each zone's name and level over the middle of its maps; "you are here" on the player's.
        foreach (IGrouping<string, Town> zone in data.Towns.Values.GroupBy(t => t.ZoneId))
        {
            Town first = zone.First();
            Vector2 middle = zone.Select(t => At(t) + new Vector2(step, step) / 2).Aggregate(Vector2.Zero, (a, b) => a + b) / zone.Count();
            bool here = zone.Any(t => t.Id == current);
            var label = new Label
            {
                Text = $"{first.Name.In(texts.Lang)}\n{texts["map.level", first.Level]}{(here ? "\n" + texts["map.here"] : "")}",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Size = new Vector2(220, 90),
                Position = middle - new Vector2(110, 45),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            label.AddThemeConstantOverride("outline_size", 6);
            label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.8f));
            area.AddChild(label);
        }
    }

    /// <summary>The maps drawn, as the self-test counts them.</summary>
    public IReadOnlyCollection<string> Maps => _maps.Keys;
}
