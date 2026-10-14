using Godot;
using Rpg.Client;

namespace Rpg.Desktop;

/// <summary>
/// The game's sound (sprint 69): a "Music" and an "Effects" bus under the general one, their volumes
/// from <see cref="AudioSettings"/> (kept in the player's settings file), the village's loop
/// (<c>tools/music_gen.py</c>) and Kenney's effects (CC0, <c>assets/kenney/sounds/</c>) by name.
/// Without a sound card (the self-tests, headless), the engine plays into nothing.
/// </summary>
public partial class Sound : Node
{
    public const string MusicBus = "Music", EffectsBus = "Effects";
    private static readonly string SettingsPath = "user://" + AudioSettings.FileName;

    private static AudioSettings? _settings;
    private readonly List<AudioStreamPlayer> _players = [];
    private AudioStreamPlayer? _music;
    private int _step;

    /// <summary>The settings in use, read from the settings file the first time.</summary>
    public static AudioSettings Settings
    {
        get
        {
            if (_settings is null)
            {
                using var file = Godot.FileAccess.Open(SettingsPath, Godot.FileAccess.ModeFlags.Read);
                _settings = AudioSettings.Parse(file?.GetAsText());
            }
            return _settings;
        }
    }

    public override void _Ready()
    {
        foreach (string bus in new[] { MusicBus, EffectsBus })
        {
            if (AudioServer.GetBusIndex(bus) >= 0)
                continue;
            AudioServer.AddBus();
            int index = AudioServer.BusCount - 1;
            AudioServer.SetBusName(index, bus);
            AudioServer.SetBusSend(index, "Master");
        }
        Apply(Settings, save: false);
    }

    /// <summary>The volumes to the buses at once; saved in the settings file unless told otherwise.</summary>
    public static void Apply(AudioSettings settings, bool save = true)
    {
        _settings = settings.Clamped();
        AudioServer.SetBusVolumeDb(0, (float)_settings.MasterDb);
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex(MusicBus), (float)_settings.MusicDb);
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex(EffectsBus), (float)_settings.EffectsDb);
        if (!save)
            return;
        using var file = Godot.FileAccess.Open(SettingsPath, Godot.FileAccess.ModeFlags.Write);
        file?.StoreString(_settings.ToText());
    }

    /// <summary>The village's music, played round and round.</summary>
    public void PlayMusic()
    {
        if (_music is not null || GD.Load<AudioStreamWav>("res://assets/music/village.wav") is not AudioStreamWav loop)
            return;
        loop.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
        loop.LoopBegin = 0;
        loop.LoopEnd = (int)(loop.GetLength() * loop.MixRate);
        _music = new AudioStreamPlayer { Stream = loop, Bus = MusicBus };
        AddChild(_music);
        _music.Play();
    }

    public bool MusicPlaying => _music?.Playing == true;

    /// <summary>An effect by its name in <c>assets/kenney/sounds/</c> ("click", "open", "hit"...); several can overlap.</summary>
    public void Play(string effect)
    {
        string path = $"res://assets/kenney/sounds/{effect}.ogg";
        if (!ResourceLoader.Exists(path))
            return;
        AudioStreamPlayer player = _players.FirstOrDefault(p => !p.Playing) ?? NewPlayer();
        player.Stream = GD.Load<AudioStream>(path);
        player.Play();
    }

    /// <summary>The next of the four footsteps.</summary>
    public void Step() => Play($"step-{_step++ % 4 + 1}");

    private AudioStreamPlayer NewPlayer()
    {
        var player = new AudioStreamPlayer { Bus = EffectsBus };
        AddChild(player);
        _players.Add(player);
        return player;
    }
}
