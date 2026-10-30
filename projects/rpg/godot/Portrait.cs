using Godot;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// A character's model in a small 3D world of its own (a viewport inside the interface): the
/// portraits of the character cards, and the turning preview of the creation panel.
/// </summary>
public partial class Portrait : SubViewportContainer
{
    private SubViewport _viewport = null!;
    private Camera3D _camera = null!;
    private Node3D? _model;
    private bool _fit, _framed;
    private Hero? _shown;

    /// <summary>Turns slowly on itself (the creation preview); the cards stand still.</summary>
    public bool Turning { get; set; }

    public string Look { get; private set; } = "";
    public int Colour { get; private set; }

    /// <param name="distance">From the camera to the model: farther for a large preview.</param>
    public static Portrait Make(Vector2 size, bool turning, float distance = 2.1f)
    {
        var portrait = new Portrait { CustomMinimumSize = size, Stretch = true, Turning = turning, MouseFilter = MouseFilterEnum.Ignore };
        portrait._viewport = new SubViewport { OwnWorld3D = true, TransparentBg = true, Size = (Vector2I)size, Msaa3D = Viewport.Msaa.Msaa2X };
        portrait.AddChild(portrait._viewport);
        portrait._viewport.AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.ClearColor,
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = Colors.White,
                AmbientLightEnergy = 0.55f,
            },
        });
        portrait._viewport.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-40, 30, 0), LightEnergy = 0.9f });
        // Outside the scene tree yet: the camera's transform is computed, not looked at.
        portrait._camera = new Camera3D { Fov = 32, Transform = new Transform3D(Basis.Identity, new Vector3(0, 0.62f, distance)).LookingAt(new Vector3(0, 0.45f, 0), Vector3.Up) };
        portrait._viewport.AddChild(portrait._camera);
        return portrait;
    }

    /// <summary>
    /// Framed on the model's own size rather than at a set distance (sprint 172, D88): the bestiary's monsters, from a piglet
    /// to a king, each filling the picture whichever way it has turned (<see cref="Framing.Turning"/>).
    /// </summary>
    public Portrait Fit()
    {
        _fit = true;
        _framed = false;
        return this;
    }

    /// <summary>
    /// Shows a character as its appearance says, facing the viewer, idle; without
    /// <paramref name="paint"/>, in the model's own colours (monsters, people of the scenarios).
    /// </summary>
    public void Show(Hero hero, bool paint = true)
    {
        ArgumentNullException.ThrowIfNull(hero);
        Hero shown = hero with { Name = paint ? "" : "unpainted" };
        if (shown == _shown && _model is not null)
            return;
        _shown = shown;
        Look = hero.Look;
        Colour = hero.Colour;
        _model?.QueueFree();
        _model = Looks.Model(hero.Look);
        if (paint)
            Looks.Paint(_model, hero);
        _model.RotationDegrees = new Vector3(0, 20, 0);
        _viewport.AddChild(_model);
        _framed = false;
        if (_model.FindChild("AnimationPlayer", true, false) is AnimationPlayer anim && anim.HasAnimation("idle"))
        {
            anim.GetAnimation("idle").LoopMode = Animation.LoopModeEnum.Linear;
            anim.Play("idle");
        }
    }

    public override void _Process(double delta)
    {
        // Framed once in the scene tree, where the camera can measure its picture.
        if (_fit && !_framed && _model is not null)
        {
            Framing.Frame(_camera, Framing.Turning(Framing.Bounds(_model)));
            _framed = true;
        }
        if (Turning)
            _model?.RotateY((float)delta * 0.6f);
    }
}
