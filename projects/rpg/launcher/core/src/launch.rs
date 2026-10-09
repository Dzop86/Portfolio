//! Starting the game: its executable in the installed folder, the server and the player's token
//! in its environment (not on the command line, where other users of the machine could read it).

use std::path::{Path, PathBuf};
use std::process::Command;

/// The game's executable in a folder exported by Godot, for a platform. On macOS, the first
/// `.app` of the folder and the program in its `Contents/MacOS` (their names follow the export).
pub fn executable(dir: &Path, platform: &str) -> PathBuf {
    match platform {
        "windows" => dir.join("rpg.exe"),
        "macos" => first(dir, |p| p.extension().is_some_and(|e| e == "app"))
            .and_then(|app| first(&app.join("Contents/MacOS"), |p| p.is_file()))
            .unwrap_or_else(|| dir.join("rpg.app/Contents/MacOS/rpg")),
        _ => dir.join("rpg.x86_64"),
    }
}

fn first(dir: &Path, keep: impl Fn(&Path) -> bool) -> Option<PathBuf> {
    let mut found: Vec<PathBuf> = std::fs::read_dir(dir)
        .ok()?
        .filter_map(|e| e.ok().map(|e| e.path()))
        .filter(|p| keep(p))
        .collect();
    found.sort();
    found.into_iter().next()
}

/// The command that starts the game signed in: `RPG_SERVER` and `RPG_TOKEN` in its environment,
/// the language after Godot's `--`.
pub fn command(
    dir: &Path,
    platform: &str,
    server: &str,
    token: Option<&str>,
    lang: &str,
) -> Command {
    let mut command = Command::new(executable(dir, platform));
    command
        .current_dir(dir)
        .env("RPG_SERVER", server)
        .args(["--", "--lang", lang]);
    match token {
        Some(token) => command.env("RPG_TOKEN", token),
        None => command.env_remove("RPG_TOKEN"),
    };
    command
}
