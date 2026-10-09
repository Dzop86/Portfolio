using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>A fighter on the board: its animated model, a ring of its team's colour, its hit points above it.</summary>
public partial class FighterView : Node3D
{
    public static readonly Color PlayerColour = new("bef374");
    public static readonly Color EnemyColour = new("e05d44");

    private AnimationPlayer? _anim;
    private Label3D _hp = null!;
    private MeshInstance3D _ring = null!;
    private Node3D _model = null!;

    public Fighter Fighter { get; private set; } = null!;

    public void Setup(Fighter fighter, bool player)
    {
        Fighter = fighter;
        Name = $"Fighter{fighter.Id}";
        _model = Looks.Fighter(fighter.Spec.Look).Instantiate<Node3D>();
        AddChild(_model);
        _anim = _model.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
        foreach (string loop in new[] { "idle", "walk" })
        {
            if (_anim?.GetAnimation(loop) is Animation a)
                a.LoopMode = Animation.LoopModeEnum.Linear;
        }
        _ring = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 0.3f, OuterRadius = 0.4f, Rings = 24 },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = player ? PlayerColour : EnemyColour, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded },
            Scale = new Vector3(1, 0.15f, 1),
            Position = new Vector3(0, 0.03f, 0),
        };
        AddChild(_ring);
        _hp = new Label3D
        {
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true,
            FontSize = 40,
            OutlineSize = 10,
            PixelSize = 0.006f,
            Position = new Vector3(0, 1.05f, 0),
        };
        AddChild(_hp);
        Position = BoardView.ToWorld(fighter.Cell);
        Face(fighter.Team == 0 ? new Vector3(1, 0, 0) : new Vector3(-1, 0, 0));
        Refresh();
        Play("idle");
    }

    /// <summary>The player's outfit colour (see <see cref="Looks.Paint"/>).</summary>
    public void Paint(Hero hero) => Looks.Paint(_model, hero);

    public int? Painted => Looks.PaintedWith(_model);

    /// <summary>Hit points and the ring of the fighter whose turn it is.</summary>
    public void Refresh(bool current = false)
    {
        _hp.Text = $"{Fighter.Hp}/{Fighter.MaxHp}";
        _hp.Visible = Fighter.IsAlive;
        _ring.Visible = Fighter.IsAlive;
        _ring.Scale = current ? new Vector3(1.25f, 0.2f, 1.25f) : new Vector3(1, 0.15f, 1);
    }

    public string HpText => _hp.Text;

    /// <summary>Walks along the path, one cell per <paramref name="step"/> seconds (0: at once).</summary>
    public async Task Walk(IReadOnlyList<Cell> path, double step)
    {
        if (step <= 0)
        {
            Position = BoardView.ToWorld(path[^1]);
            return;
        }
        Play("walk");
        foreach (Cell c in path)
        {
            Vector3 to = BoardView.ToWorld(c);
            Face(to - Position);
            Tween tween = CreateTween();
            tween.TweenProperty(this, "position", to, step);
            await ToSignal(tween, Tween.SignalName.Finished);
        }
        Play("idle");
    }

    /// <summary>Turns to the target and plays a strike (in reach) or a throw (at range).</summary>
    public async Task Attack(Cell target, bool melee, double duration)
    {
        Face(BoardView.ToWorld(target) - Position);
        if (duration <= 0)
            return;
        Play(melee ? "attack-melee-right" : "holding-right-shoot");
        await ToSignal(GetTree().CreateTimer(duration), SceneTreeTimer.SignalName.Timeout);
        Play("idle");
    }

    /// <summary>The damage rises and fades above the fighter, in the colour of its element.</summary>
    public void ShowDamage(int amount, Element element, double duration) => ShowNumber($"-{amount}", new Color(ElementStyle.Colour(element)), duration);

    /// <summary>The last number shown above the fighter and its colour (the self-test reads it).</summary>
    public (string Text, Color Colour)? LastNumber { get; private set; }

    /// <summary>A number that rises and fades above the fighter: damage in its element's colour, healing, a shield.</summary>
    public void ShowNumber(string text, Color colour, double duration)
    {
        Refresh();
        LastNumber = (text, colour);
        if (duration <= 0)
            return;
        var label = new Label3D
        {
            Text = text,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true,
            FontSize = 56,
            OutlineSize = 12,
            PixelSize = 0.006f,
            Modulate = colour,
            Position = new Vector3(0, 1.3f, 0),
        };
        AddChild(label);
        Tween tween = CreateTween();
        tween.TweenProperty(label, "position", new Vector3(0, 1.9f, 0), duration);
        tween.Parallel().TweenProperty(label, "modulate:a", 0f, duration);
        tween.TweenCallback(Callable.From(label.QueueFree));
    }

    public void Die(bool animate)
    {
        Refresh();
        if (animate)
        {
            Play("die");
        }
        else
        {
            Visible = false;
        }
    }

    private void Face(Vector3 direction)
    {
        if (direction.LengthSquared() > 0.0001f)
            _model.Rotation = new Vector3(0, Mathf.Atan2(direction.X, direction.Z), 0);
    }

    private void Play(string name)
    {
        if (_anim is not null && _anim.HasAnimation(name))
            _anim.Play(name);
    }
}
