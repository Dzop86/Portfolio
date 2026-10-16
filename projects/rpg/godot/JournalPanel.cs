using Godot;
using Rpg.Client;

namespace Rpg.Desktop;

/// <summary>
/// The quest journal (sprint 88, key Q): two tabs, the quests under way and those done, zone by zone;
/// each quest with its giver, its words and its objectives, those reached ticked. What it shows is
/// <see cref="QuestBook.Journal"/>.
/// </summary>
public partial class JournalPanel : CanvasLayer
{
    private TabContainer _tabs = null!;
    private VBoxContainer _active = null!, _done = null!;

    public event Action? ClosePressed;

    /// <summary>The quests' names shown under way and done (for the self-test).</summary>
    public List<string> ActiveNames { get; } = [];
    public List<string> DoneNames { get; } = [];

    /// <summary>Every objective line shown (for the self-test).</summary>
    public List<string> ObjectiveLines { get; } = [];

    public void Build(QuestBook book, Texts texts)
    {
        Layer = 15;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -320, OffsetRight = 320, OffsetTop = -270, OffsetBottom = 270 };
        panel.AddThemeStyleboxOverride("panel", InventoryPanel.Background());
        AddChild(panel);
        WindowDrag.Make(panel, "journal");
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 16);
        panel.AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        margin.AddChild(column);
        var title = new Label { Text = texts["journal.title"], HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 24);
        column.AddChild(title);
        _tabs = new TabContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        column.AddChild(_tabs);
        _active = Tab(texts["journal.active"]);
        _done = Tab(texts["journal.done"]);
        var close = new Button { Text = texts["craft.close"], CustomMinimumSize = new Vector2(160, 44), SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        close.Pressed += () => ClosePressed?.Invoke();
        column.AddChild(close);
        Fill(book, texts);
    }

    private VBoxContainer Tab(string name)
    {
        var scroll = new ScrollContainer { Name = name, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _tabs.AddChild(scroll);
        var box = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        box.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(box);
        return box;
    }

    private void Fill(QuestBook book, Texts texts)
    {
        IReadOnlyList<JournalZone> zones = book.Journal(texts);
        foreach ((VBoxContainer box, bool done) in new[] { (_active, false), (_done, true) })
        {
            bool any = false;
            foreach (JournalZone zone in zones)
            {
                IReadOnlyList<JournalQuest> quests = done ? zone.Done : zone.Active;
                if (quests.Count == 0)
                    continue;
                any = true;
                var head = new Label { Text = zone.Name };
                head.AddThemeColorOverride("font_color", FighterView.PlayerColour);
                head.AddThemeFontSizeOverride("font_size", 18);
                box.AddChild(head);
                foreach (JournalQuest q in quests)
                    box.AddChild(Entry(q, texts));
                (done ? DoneNames : ActiveNames).AddRange(quests.Select(q => q.Name));
            }
            if (!any)
                box.AddChild(new Label { Text = texts["journal.empty"], Modulate = new Color(1, 1, 1, 0.6f), AutowrapMode = TextServer.AutowrapMode.WordSmart });
        }
    }

    private VBoxContainer Entry(JournalQuest q, Texts texts)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 2);
        var name = new Label { Text = q.Name };
        name.AddThemeFontSizeOverride("font_size", 17);
        box.AddChild(name);
        box.AddChild(new Label { Text = q.Ready ? texts["journal.ready", q.Giver] : texts["journal.giver", q.Giver], Modulate = new Color(1, 1, 1, 0.6f) });
        // Where to go next (sprint 105, D75).
        if (!q.Done && q.Where is string where)
            box.AddChild(new Label { Text = texts["journal.where", where], Modulate = new Color("ffd85a") });
        if (!q.Done)
        {
            box.AddChild(new Label { Text = q.Text, AutowrapMode = TextServer.AutowrapMode.WordSmart, Modulate = new Color(1, 1, 1, 0.85f), CustomMinimumSize = new Vector2(560, 0) });
            foreach ((string line, bool reached) in q.Objectives)
            {
                var l = new Label { Text = "– " + line };
                if (reached)
                    l.AddThemeColorOverride("font_color", FighterView.PlayerColour);
                box.AddChild(l);
                ObjectiveLines.Add(line);
            }
        }
        return box;
    }
}
