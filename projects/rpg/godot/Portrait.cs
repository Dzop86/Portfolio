using Godot;

namespace Rpg.Desktop;

/// <summary>
/// A character's model in a small 3D world of its own (a viewport inside the interface): the
/// portraits of the character cards, and the turning preview of the creation panel.
/// </summary>
public partial class Portrait : SubViewportContainer
{
    private SubViewport _viewport = null!;
    private Node3D? _model;

    /// <summary>Turns slowly on itself (the creation preview); the cards stand still.</summary>
    public bool Turning { get; set; }

    public string Look { get; private set; } = "";
    public int Colour { get; private set; }

    public static Portrait Make(Vector2 size, bool turning)
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
        var camera = new Camera3D { Fov = 32, Transform = new Transform3D(Basis.Identity, new Vector3(0, 0.62f, 2.1f)).LookingAt(new Vector3(0, 0.4f, 0), Vector3.Up) };
        portrait._viewport.AddChild(camera);
        return portrait;
    }

    /// <summary>Shows a look in an outfit colour, facing the viewer, idle.</summary>
    public void Show(string look, int colour)
    {
        if (look == Look && colour == Colour && _model is not null)
            return;
        Look = look;
        Colour = colour;
        _model?.QueueFree();
        _model = Looks.Fighter(look).Instantiate<Node3D>();
        Looks.Paint(_model, colour);
        _model.RotationDegrees = new Vector3(0, 20, 0);
        _viewport.AddChild(_model);
        if (_model.FindChild("AnimationPlayer", true, false) is AnimationPlayer anim && anim.HasAnimation("idle"))
        {
            anim.GetAnimation("idle").LoopMode = Animation.LoopModeEnum.Linear;
            anim.Play("idle");
        }
    }

    public override void _Process(double delta)
    {
        if (Turning)
            _model?.RotateY((float)delta * 0.6f);
    }
}
