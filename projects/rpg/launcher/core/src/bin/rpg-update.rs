//! The launcher without its window, for scripts and the CI: brings a folder to the server's version.
//!   rpg-update http://localhost:8002 ./game [linux|windows|macos]
use std::path::Path;
use std::process::ExitCode;

use rpg_launcher_core::update::{self, HttpSource};

fn main() -> ExitCode {
    let args: Vec<String> = std::env::args().skip(1).collect();
    let (server, dir, platform) = match args.as_slice() {
        [s, d] => (s, d, update::platform()),
        [s, d, p] => (s, d, p.as_str()),
        _ => {
            eprintln!("usage: rpg-update SERVER FOLDER [PLATFORM]");
            return ExitCode::from(2);
        }
    };
    let source = HttpSource::new(server, platform);
    match update::update(&source, Path::new(dir), &mut |_| {}) {
        Ok(report) => {
            println!(
                "version {}: {} file(s) downloaded ({} bytes), {} removed",
                report.version,
                report.downloaded.len(),
                report.received,
                report.removed.len()
            );
            ExitCode::SUCCESS
        }
        Err(e) => {
            eprintln!("{e}");
            ExitCode::FAILURE
        }
    }
}
