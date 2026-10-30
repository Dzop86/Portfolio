using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The world map's photographs (sprint 169, D88, <see cref="MapPhotos"/>): each map of the world built as the player sees it
/// (<see cref="TownView"/>: its ground, trees, houses, water, rocks, inhabitants), without the names, rings and the player,
/// seen from the sky by an orthographic camera leaning a little so that the houses show their fronts, rendered in a viewport
/// of its own, its sides without a neighbour faded out, written as WebP with the index of what each one shows. Run once when a
/// map changes, in a window (a virtual screen will do):
///   godot --path godot -- --render-maps "$PWD/godot/assets/world/photos"
/// </summary>
public partial class MapPhotographer : Node
{
    /// <summary>How far the camera leans from looking straight down, in degrees.</summary>
    private const float Lean = 30;

    public async Task Run(string folder)
    {
        System.IO.Directory.CreateDirectory(folder);
        GameData data = GameData.Embedded;
        int done = 0;
        foreach (Town map in MapPhotos.World(data))
        {
            int width = map.Width, depth = map.Rows.Count;
            float seen = depth * Mathf.Cos(Mathf.DegToRad(Lean));
            var viewport = new SubViewport
            {
                OwnWorld3D = true,
                Size = new Vector2I(MapPhotos.Width, Mathf.RoundToInt(MapPhotos.Width * seen / width)),
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
                Msaa3D = Viewport.Msaa.Msaa4X,
            };
            AddChild(viewport);
            viewport.AddChild(new WorldEnvironment
            {
                Environment = new Godot.Environment
                {
                    BackgroundMode = Godot.Environment.BGMode.Color,
                    BackgroundColor = new Color(ZoneStyle.Ground(map.Zone).Dark),
                    AmbientLightSource = Godot.Environment.AmbientSource.Color,
                    AmbientLightColor = Colors.White,
                    AmbientLightEnergy = 0.5f,
                    TonemapMode = Godot.Environment.ToneMapper.Filmic,
                },
            });
            viewport.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-55, -30, 0), ShadowEnabled = true, LightColor = new Color(1f, 0.96f, 0.88f), LightEnergy = 0.9f });
            var view = new TownView { PlainGround = true };
            viewport.AddChild(view);
            view.Build(new TownController(data, map.Id), "fr", new Hero("", "female-d"));
            Hide(view);
            view.Player.Visible = false;
            // Looking down the north-south axis, the map's middle in the middle of the picture.
            var centre = new Vector3((width - 1) / 2f, 0, (depth - 1) / 2f);
            var camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Size = seen, RotationDegrees = new Vector3(Lean - 90, 0, 0), Far = 400 };
            viewport.AddChild(camera);
            camera.Position = centre + camera.Transform.Basis.Z * 100;
            camera.MakeCurrent();
            for (int i = 0; i < 4; i++)
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            Image image = viewport.GetTexture().GetImage();
            image.Resize(MapPhotos.Width, MapPhotos.Height, Image.Interpolation.Lanczos);
            image.Convert(Image.Format.Rgba8);
            var open = MapPhotos.Open(data, map);
            for (int y = 0; y < MapPhotos.Height; y++)
                for (int x = 0; x < MapPhotos.Width; x++)
                {
                    double a = MapPhotos.Alpha(open, x, y);
                    if (a < 1)
                    {
                        Color c = image.GetPixel(x, y);
                        image.SetPixel(x, y, new Color(c.R, c.G, c.B, (float)a));
                    }
                }
            image.SaveWebp(System.IO.Path.Combine(folder, MapPhotos.File(map.Id)), true, 0.82f);
            viewport.QueueFree();
            done++;
        }
        System.IO.File.WriteAllText(System.IO.Path.Combine(folder, MapPhotos.Index), MapPhotos.WriteIndex(data));
        GD.Print($"PHOTOS {done} {folder}");
        GetTree().Quit(0);
    }

    /// <summary>The names, the rings of the ways and doors: what is for playing, not for a map.</summary>
    private static void Hide(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is Label3D || child is MeshInstance3D { Mesh: TorusMesh })
                ((Node3D)child).Visible = false;
            Hide(child);
        }
    }
}
