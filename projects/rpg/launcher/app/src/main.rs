//! The launcher's window (Tauri): the page in `launcher/ui` calls these commands. Signing in,
//! updating and starting the game are `rpg-launcher-core`'s; the token stays on this side and is
//! handed to the game through its environment.
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use std::path::PathBuf;
use std::sync::Mutex;

use rpg_launcher_core::{launch, manifest, server, update, Error};
use serde::Serialize;
use tauri::{Emitter, Manager, State};

/// The session: the player's token once signed in.
#[derive(Default)]
struct Session {
    token: Mutex<Option<String>>,
}

/// A refusal the page translates: a code and, for the curious, the details.
#[derive(Debug, Serialize)]
struct Problem {
    code: &'static str,
    detail: String,
}

impl From<Error> for Problem {
    fn from(e: Error) -> Self {
        let code = match e {
            Error::Unreachable(_) => "unreachable",
            Error::WrongPassword => "wrong-password",
            Error::TooManyTries => "too-many-tries",
            Error::Server(_) => "server",
            Error::Unreadable(_) => "unreadable",
            Error::Corrupt(_) => "corrupt",
            Error::UnsafePath(_) => "unsafe-path",
            Error::Io(_) => "io",
        };
        Problem {
            code,
            detail: e.to_string(),
        }
    }
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
struct Settings {
    server: String,
    folder: String,
    installed: Option<String>,
    /// "fr" when the system speaks French (LANG, LC_ALL), else "en".
    lang: &'static str,
}

fn game_folder(app: &tauri::AppHandle) -> PathBuf {
    app.path()
        .app_local_data_dir()
        .unwrap_or_else(|_| PathBuf::from("."))
        .join("game")
}

#[tauri::command]
fn settings(app: tauri::AppHandle) -> Settings {
    let folder = game_folder(&app);
    Settings {
        server: std::env::var("RPG_SERVER").unwrap_or_else(|_| "http://localhost:8002".into()),
        installed: manifest::installed(&folder).map(|m| m.version),
        folder: folder.display().to_string(),
        lang: system_lang(),
    }
}

fn system_lang() -> &'static str {
    let french = ["LC_ALL", "LC_MESSAGES", "LANG", "LANGUAGE"]
        .iter()
        .filter_map(|v| std::env::var(v).ok())
        .find(|v| !v.is_empty())
        .is_some_and(|v| v.starts_with("fr"));
    if french {
        "fr"
    } else {
        "en"
    }
}

#[tauri::command]
async fn sign_in(
    session: State<'_, Session>,
    server: String,
    name: String,
    password: String,
) -> Result<(), Problem> {
    let token =
        tauri::async_runtime::spawn_blocking(move || server::sign_in(&server, &name, &password))
            .await
            .map_err(|e| Problem {
                code: "io",
                detail: e.to_string(),
            })??;
    *session
        .token
        .lock()
        .expect("the session lock is never poisoned") = Some(token);
    Ok(())
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
struct Updated {
    version: String,
    downloaded: usize,
    removed: usize,
    received: u64,
}

/// Brings the game folder to the server's version; "progress" events feed the page's bar.
#[tauri::command]
async fn update_game(app: tauri::AppHandle, server: String) -> Result<Updated, Problem> {
    let folder = game_folder(&app);
    let window = app.clone();
    let report = tauri::async_runtime::spawn_blocking(move || {
        let source = update::HttpSource::new(&server, update::platform());
        update::update(&source, &folder, &mut |p| {
            // A lost event only delays the bar.
            let _ = window.emit("progress", (p.done, p.total, p.file, p.files));
        })
    })
    .await
    .map_err(|e| Problem {
        code: "io",
        detail: e.to_string(),
    })??;
    Ok(Updated {
        version: report.version,
        downloaded: report.downloaded.len(),
        removed: report.removed.len(),
        received: report.received,
    })
}

/// Starts the game signed in (or offline before any sign-in), in the page's language.
#[tauri::command]
fn play(
    app: tauri::AppHandle,
    session: State<'_, Session>,
    server: String,
    lang: String,
) -> Result<(), Problem> {
    let token = session
        .token
        .lock()
        .expect("the session lock is never poisoned")
        .clone();
    launch::command(
        &game_folder(&app),
        update::platform(),
        &server,
        token.as_deref(),
        &lang,
    )
    .spawn()
    .map(|_| ())
    .map_err(|e| Problem::from(Error::Io(e)))
}

fn main() {
    tauri::Builder::default()
        .manage(Session::default())
        .invoke_handler(tauri::generate_handler![
            settings,
            sign_in,
            update_game,
            play
        ])
        .run(tauri::generate_context!())
        .expect("the launcher's window could not open");
}
