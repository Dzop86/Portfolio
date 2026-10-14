namespace Rpg.Client;

/// <summary>
/// The colour of each zone's grass, in two checks (sprint 74): the woods' green, the mouldy woods'
/// olive, the lake's lush green, the fields' yellow green. Dark for the 3D view, which the light
/// brightens; light for the world seen from the sky (<see cref="WorldPicture"/>). Hexadecimal.
/// </summary>
public static class ZoneStyle
{
    public static (string Light, string Dark) Ground(string? zone) => zone switch
    {
        "mouldy" => ("4c5432", "555e38"),
        "lake" => ("2e6b3c", "367543"),
        "fields" => ("6a7a2c", "748534"),
        _ => ("37602a", "406c31"),
    };

    public static (Rgb Light, Rgb Dark) Sky(string? zone) => zone switch
    {
        "mouldy" => (new(0x80, 0x8c, 0x56), new(0x76, 0x82, 0x4e)),
        "lake" => (new(0x5e, 0xc2, 0x6c), new(0x55, 0xb5, 0x62)),
        "fields" => (new(0xb6, 0xc4, 0x5a), new(0xab, 0xb9, 0x50)),
        _ => (WorldPicture.Grass, WorldPicture.GrassDark),
    };
}
