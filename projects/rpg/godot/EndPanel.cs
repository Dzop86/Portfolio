using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The end-of-fight window (sprint 87, D68), as in the games of the genre: "Fight over", Victory or
/// Defeat; a Results tab with the winners and the losers, a line each (name, level), the player's own with
/// its experience bar, the experience and Éclats earned and its loot as icons; a Statistics tab; and a
/// single "Close", which leaves the player where they stood. What it shows is <see cref="FightSummary"/>.
/// </summary>
public partial class EndPanel : PanelContainer
{
    private Label _title = null!, _outcome = null!, _notes = null!;
    private VBoxContainer _results = null!;
    private GridContainer _stats = null!;
    private TabContainer _tabs = null!;
    private Button _close = null!;
    private Texts _texts = new("en");

    public event Action? ClosePressed;

    /// <summary>What is shown, for the self-test.</summary>
    public FightSummary? Summary { get; private set; }

    public string CloseText => _close.Text;

    /// <summary>The texts of the results tab's lines, in order (for the self-test).</summary>
    public List<string> Lines { get; } = [];

    /// <summary>The loot icons shown on the player's line.</summary>
    public int LootIcons { get; private set; }

    public void Build(Texts texts)
    {
        _texts = texts;
        AddThemeStyleboxOverride("panel", InventoryPanel.Background());
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 16);
        AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        margin.AddChild(column);
        _title = new Label { HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(1, 1, 1, 0.7f) };
        column.AddChild(_title);
        _outcome = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _outcome.AddThemeFontSizeOverride("font_size", 34);
        column.AddChild(_outcome);
        _tabs = new TabContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        column.AddChild(_tabs);
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _tabs.AddChild(scroll);
        _results = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _results.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_results);
        var statScroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _tabs.AddChild(statScroll);
        _stats = new GridContainer { Columns = 6, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _stats.AddThemeConstantOverride("h_separation", 18);
        statScroll.AddChild(_stats);
        _notes = new Label { HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _notes.AddThemeColorOverride("font_color", FighterView.PlayerColour);
        column.AddChild(_notes);
        _close = new Button { CustomMinimumSize = new Vector2(180, 48), SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
        _close.Pressed += () => ClosePressed?.Invoke();
        column.AddChild(_close);
    }

    /// <param name="problem">What went wrong with the server, if anything, under the notes.</param>
    public void Show(FightSummary summary, Texts texts, string? problem = null)
    {
        Summary = summary;
        _texts = texts;
        _title.Text = texts["end.title"];
        _outcome.Text = summary.Title;
        _tabs.SetTabTitle(0, texts["end.results"]);
        _tabs.SetTabTitle(1, texts["end.stats"]);
        _close.Text = texts["end.close"];
        _notes.Text = string.Join("\n", new[] { summary.Notes, problem ?? "" }.Where(t => t.Length > 0));
        _notes.Visible = _notes.Text.Length > 0;
        foreach (Node n in _results.GetChildren())
            n.QueueFree();
        Lines.Clear();
        LootIcons = 0;
        Section(texts["end.winners"], summary.Winners, FighterView.PlayerColour);
        Section(texts["end.losers"], summary.Losers, FighterView.EnemyColour);
        foreach (Node n in _stats.GetChildren())
            n.QueueFree();
        foreach (string head in new[] { "end.name", "end.dealt", "end.taken", "end.healing", "end.shielding", "end.kills" })
            _stats.AddChild(new Label { Text = texts[head], Modulate = new Color(1, 1, 1, 0.6f) });
        foreach ((string name, FighterStats s) in summary.Stats)
        {
            _stats.AddChild(new Label { Text = name });
            foreach (int value in new[] { s.DamageDealt, s.DamageTaken, s.Healing, s.Shielding, s.Kills })
                _stats.AddChild(new Label { Text = value.ToString(System.Globalization.CultureInfo.InvariantCulture), HorizontalAlignment = HorizontalAlignment.Right });
        }
    }

    private void Section(string title, IReadOnlyList<SummaryRow> rows, Color colour)
    {
        var head = new Label { Text = title };
        head.AddThemeColorOverride("font_color", colour);
        head.AddThemeFontSizeOverride("font_size", 18);
        _results.AddChild(head);
        foreach (SummaryRow row in rows)
        {
            var line = new HBoxContainer();
            line.AddThemeConstantOverride("separation", 12);
            _results.AddChild(line);
            line.AddChild(new Label { Text = row.Name, CustomMinimumSize = new Vector2(180, 0) });
            line.AddChild(new Label { Text = $"{_texts["end.level"]} {row.Level}", CustomMinimumSize = new Vector2(70, 0) });
            string text = $"{row.Name} {row.Level}";
            if (row.IsPlayer)
            {
                var xp = new VBoxContainer { CustomMinimumSize = new Vector2(150, 0) };
                xp.AddThemeConstantOverride("separation", 0);
                if (row.XpBar is double bar)
                {
                    var progress = new ProgressBar { MinValue = 0, MaxValue = 1, Step = 0.001, Value = bar, ShowPercentage = false, CustomMinimumSize = new Vector2(150, 10), TooltipText = row.XpText ?? "" };
                    progress.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = FighterView.PlayerColour });
                    xp.AddChild(progress);
                }
                xp.AddChild(new Label { Text = _texts["result.xp", row.Xp ?? 0] });
                line.AddChild(xp);
                var shards = new HBoxContainer();
                shards.AddChild(new ShardIcon { CustomMinimumSize = new Vector2(20, 20), SizeFlagsVertical = SizeFlags.ShrinkCenter, TooltipText = _texts["end.shards"] });
                shards.AddChild(new Label { Text = (row.Shards ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture) });
                line.AddChild(shards);
                foreach (ItemCount loot in row.Loot ?? [])
                {
                    if (!GameData.Embedded.Items.TryGetValue(loot.Item, out Item? item))
                        continue;
                    (Button tile, ItemIcon icon) = ItemIcon.Tile(40);
                    icon.Glyph = ItemStyle.GlyphOf(item);
                    icon.Item = item;
                    icon.Tint = new Color(ItemStyle.Colour(item));
                    icon.Count = loot.Count;
                    tile.TooltipText = _texts.Items([loot]);
                    line.AddChild(tile);
                    LootIcons++;
                }
                text += $" +{row.Xp} XP +{row.Shards} {_texts.Items(row.Loot ?? [])}";
            }
            Lines.Add(text.Trim());
        }
    }
}
