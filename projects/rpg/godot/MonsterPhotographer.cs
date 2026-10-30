using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The monsters' portraits (sprint 172, D88, <see cref="MonsterPortraits"/>): each monster's model as in a fight
/// (<see cref="Looks.Model"/>), at the first frame of its idle, turned a little to the side, framed on its own size
/// (<see cref="Framing"/>), rendered on nothing in a viewport of its own, written as WebP with its silhouette and the index
/// of the looks.
/// Run once when a monster or its look changes, on a virtual screen:
///   godot --path godot -- --render-portraits "$PWD/godot/assets/bestiary/portraits"
/// </summary>
public partial class MonsterPhotographer : Node
{
    public async Task Run(string folder)
    {
        System.IO.Directory.CreateDirectory(folder);
        GameData data = GameData.Embedded;
        int done = 0;
        foreach (MonsterSpec monster in MonsterPortraits.All(data))
        {
            var viewport = new SubViewport
            {
                OwnWorld3D = true,
                TransparentBg = true,
                Size = new Vector2I(MonsterPortraits.Size, MonsterPortraits.Size),
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
                Msaa3D = Viewport.Msaa.Msaa8X,
            };
            AddChild(viewport);
            viewport.AddChild(new WorldEnvironment
            {
                Environment = new Godot.Environment
                {
                    BackgroundMode = Godot.Environment.BGMode.ClearColor,
                    AmbientLightSource = Godot.Environment.AmbientSource.Color,
                    AmbientLightColor = Colors.White,
                    AmbientLightEnergy = 0.55f,
                },
            });
            viewport.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-40, 30, 0), LightEnergy = 0.9f });
            Node3D model = Looks.Model(monster.Look);
            // Turned a little to the side: the game's own monsters face the viewer at -15° (seen on the renders), Kenney's people at 30°.
            model.RotationDegrees = new Vector3(0, Creatures.Knows(monster.Look) ? -15 : 30, 0);
            viewport.AddChild(model);
            if (model.FindChild("AnimationPlayer", true, false) is AnimationPlayer anim && anim.HasAnimation("idle"))
            {
                anim.Play("idle");
                anim.Seek(0, true);
                anim.Pause();
            }
            var camera = new Camera3D { Near = 0.05f };
            viewport.AddChild(camera);
            Framing.Frame(camera, model.Transform * Framing.Bounds(model));
            camera.MakeCurrent();
            for (int i = 0; i < 4; i++)
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            Image image = viewport.GetTexture().GetImage();
            image.Convert(Image.Format.Rgba8);
            image.SaveWebp(System.IO.Path.Combine(folder, MonsterPortraits.File(monster.Id)), false);
            byte[] shadow = MonsterPortraits.Silhouette(image.GetData(), image.GetWidth(), image.GetHeight());
            Image.CreateFromData(image.GetWidth(), image.GetHeight(), false, Image.Format.Rgba8, shadow)
                .SaveWebp(System.IO.Path.Combine(folder, MonsterPortraits.Shadow(monster.Id)), false);
            viewport.QueueFree();
            done++;
        }
        System.IO.File.WriteAllText(System.IO.Path.Combine(folder, MonsterPortraits.Index), MonsterPortraits.WriteIndex(data));
        GD.Print($"PORTRAITS {done} {folder}");
        GetTree().Quit(0);
    }
}
