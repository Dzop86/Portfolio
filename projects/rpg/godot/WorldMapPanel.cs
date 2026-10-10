using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The world map, over the zone: each zone in its place (<see cref="Town.MapAt"/>) with its name and
/// level, a line for each way between two zones, the zone the player is in outlined. M or the map
/// button opens and closes it.
/// </summary>
public partial class WorldMapPanel : CanvasLayer
{
    private const float Step = 220;
    private readonly Dictionary<string, PanelContainer> _zones = [];

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
        // The zones' grid centred under the title.
        int minX = data.Towns.Values.Min(t => t.MapAt.X), minY = data.Towns.Values.Min(t => t.MapAt.Y);
        int maxX = data.Towns.Values.Max(t => t.MapAt.X), maxY = data.Towns.Values.Max(t => t.MapAt.Y);
        var middle = new Vector2(400, 260) - new Vector2(maxX - minX, (maxY - minY) * 0.75f) * Step / 2;
        Vector2 Centre(Town t) => middle + new Vector2(t.MapAt.X - minX, (t.MapAt.Y - minY) * 0.75f) * Step;
        // The ways first, under the zones.
        foreach (Town t in data.Towns.Values)
        {
            foreach (ZoneLink l in t.Links ?? [])
            {
                if (string.CompareOrdinal(t.Id, l.To) < 0 && data.Towns.TryGetValue(l.To, out Town? to))
                    area.AddChild(new Line2D { Points = [Centre(t), Centre(to)], Width = 4, DefaultColor = new Color(1, 1, 1, 0.45f) });
            }
        }
        foreach (Town t in data.Towns.Values)
        {
            bool here = t.Id == current;
            var box = new PanelContainer { Position = Centre(t) - new Vector2(100, 45), CustomMinimumSize = new Vector2(200, 90) };
            box.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = new Color(0.16f, 0.16f, 0.16f),
                BorderColor = here ? FighterView.PlayerColour : new Color(1, 1, 1, 0.35f),
                BorderWidthLeft = here ? 3 : 1,
                BorderWidthRight = here ? 3 : 1,
                BorderWidthTop = here ? 3 : 1,
                BorderWidthBottom = here ? 3 : 1,
                CornerRadiusTopLeft = 6,
                CornerRadiusTopRight = 6,
                CornerRadiusBottomLeft = 6,
                CornerRadiusBottomRight = 6,
            });
            var label = new Label
            {
                Text = $"{t.Name.In(texts.Lang)}\n{texts["map.level", t.Level]}{(here ? "\n" + texts["map.here"] : "")}",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            box.AddChild(label);
            area.AddChild(box);
            _zones[t.Id] = box;
        }
    }

    /// <summary>The zones drawn, as the self-test counts them.</summary>
    public IReadOnlyCollection<string> Zones => _zones.Keys;
}
