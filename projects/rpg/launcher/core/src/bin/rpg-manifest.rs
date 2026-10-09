//! Writes the manifest of an exported game folder, for the server to publish:
//!   rpg-manifest godot/build/linux 1.0.42
use std::path::Path;
use std::process::ExitCode;

fn main() -> ExitCode {
    let args: Vec<String> = std::env::args().skip(1).collect();
    let [dir, version] = args.as_slice() else {
        eprintln!("usage: rpg-manifest FOLDER VERSION");
        return ExitCode::from(2);
    };
    let dir = Path::new(dir);
    match rpg_launcher_core::manifest::build(dir, version) {
        Ok(manifest) => {
            let text =
                serde_json::to_string_pretty(&manifest).expect("a manifest is always serializable");
            if let Err(e) = std::fs::write(dir.join(rpg_launcher_core::manifest::FILE_NAME), text) {
                eprintln!("{e}");
                return ExitCode::FAILURE;
            }
            let bytes: u64 = manifest.files.iter().map(|f| f.size).sum();
            println!("{version}: {} file(s), {bytes} bytes", manifest.files.len());
            ExitCode::SUCCESS
        }
        Err(e) => {
            eprintln!("{e}");
            ExitCode::FAILURE
        }
    }
}
