using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// A guild's crest (sprint 156, question 82 a): its shape in the field's colour, framed faintly, and its pictogram in the
/// charge's (<see cref="GuildCrests"/>). The shapes are drawings of mine (<c>assets/icons/crest-shapes</c>), the pictograms
/// game-icons.net's (CC BY 3.0, authors in <c>assets/icons/crest/credits.json</c>); the art direction may redraw both under
/// the same ids. On the map, the same two drawings as sprites beside the guild's name (<see cref="Sprites"/>).
/// </summary>
public partial class CrestView : Control
{
    // Kept for the whole game, as the items' (a texture held by no node was freed, D67).
    private static readonly Dictionary<string, Texture2D> Textures = [];

    private GuildCrest? _crest;

    /// <summary>The crest drawn, or none.</summary>
    public GuildCrest? Crest
    {
        get => _crest;
        set
        {
            _crest = value;
            QueueRedraw();
        }
    }

    public static Texture2D Shape(string id) => Load($"res://assets/icons/crest-shapes/{id}.svg");

    public static Texture2D Pictogram(string id) => Load($"res://assets/icons/crest/{id}.svg");

    private static Texture2D Load(string path) => Textures.TryGetValue(path, out Texture2D? known) ? known : Textures[path] = GD.Load<Texture2D>(path);

    /// <summary>Lets the textures go, before the game quits.</summary>
    public static void Forget() => Textures.Clear();

    public override void _Draw()
    {
        if (_crest is null)
            return;
        float s = Mathf.Min(Size.X, Size.Y);
        var whole = new Rect2((Size - new Vector2(s, s)) / 2, new Vector2(s, s));
        Texture2D shape = Shape(_crest.Shape);
        DrawTextureRect(shape, whole, false, new Color(1, 1, 1, 0.4f));
        DrawTextureRect(shape, whole.Grow(-s * 0.04f), false, new Color(GuildPage.Colour(_crest.Field)));
        DrawTextureRect(Pictogram(_crest.Icon), whole.Grow(-s * 0.25f), false, new Color(GuildPage.Colour(_crest.Charge)));
    }

    /// <summary>
    /// The crest on the map, beside a guild's name of <paramref name="width"/> world units centred at <paramref name="at"/>:
    /// its shape and its pictogram as two sprites facing the camera, shifted left in the camera's plane.
    /// </summary>
    public static Node3D Sprites(GuildCrest crest, Vector3 at, float width)
    {
        ArgumentNullException.ThrowIfNull(crest);
        const float Height = 0.2f, PixelSize = Height / 512;
        var root = new Node3D { Name = "Crest", Position = at };
        float shift = -((width / 2) + (Height * 0.7f)) / PixelSize;
        root.AddChild(Sprite(Shape(crest.Shape), new Color(GuildPage.Colour(crest.Field)), PixelSize, shift, 1));
        root.AddChild(Sprite(Pictogram(crest.Icon), new Color(GuildPage.Colour(crest.Charge)), PixelSize / 2, shift * 2, 2));
        return root;
    }

    private static Sprite3D Sprite(Texture2D texture, Color tint, float pixelSize, float shift, int priority) => new()
    {
        Texture = texture,
        Modulate = tint,
        PixelSize = pixelSize,
        Offset = new Vector2(shift, 0),
        Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
        NoDepthTest = true,
        RenderPriority = priority,
        Shaded = false,
    };
}
