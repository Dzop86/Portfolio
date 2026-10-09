//! Osmose's launcher window (Tauri): the page in `launcher/ui` calls these commands. At start it
//! updates the game by itself; "Sign in" signs in (or up), waits for the update, then starts the
//! game. The work is `rpg-launcher-core`'s; the token and the password stay on this side.
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use std::path::PathBuf;
use std::sync::Mutex;

use rpg_launcher_core::account::{self, Login, Settings, SystemSecrets};
use rpg_launcher_core::{launch, manifest, update, Error};
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

fn code(e: &Error) -> &'static str {
    match e {
        Error::Unreachable(_) => "unreachable",
        Error::WrongPassword => "wrong-password",
        Error::NameTaken => "name-taken",
        Error::Invalid(_) => "invalid",
        Error::MissingPassword => "missing-password",
        Error::Secrets(_) => "secrets",
        Error::TooManyTries => "too-many-tries",
        Error::Server(_) => "server",
        Error::Unreadable(_) => "unreadable",
        Error::Corrupt(_) => "corrupt",
        Error::UnsafePath(_) => "unsafe-path",
        Error::Io(_) => "io",
    }
}

impl From<Error> for Problem {
    fn from(e: Error) -> Self {
        Problem {
            code: code(&e),
            detail: e.to_string(),
        }
    }
}

fn joined(e: tauri::Error) -> Problem {
    Problem {
        code: "io",
        detail: e.to_string(),
    }
}

/// Where the game is installed.
fn game_folder(app: &tauri::AppHandle) -> PathBuf {
    app.path()
        .app_local_data_dir()
        .unwrap_or_else(|_| PathBuf::from("."))
        .join("game")
}

/// Where the launcher keeps its settings (server, name to fill in).
fn config_folder(app: &tauri::AppHandle) -> PathBuf {
    app.path()
        .app_config_dir()
        .unwrap_or_else(|_| PathBuf::from("."))
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
struct Start {
    lang: &'static str,
    name: Option<String>,
    password_saved: bool,
    installed: Option<String>,
}

#[tauri::command]
fn start(app: tauri::AppHandle) -> Start {
    let folder = config_folder(&app);
    let (name, password_saved) = account::remembered(&folder, &SystemSecrets);
    // French by default, as the player last chose otherwise.
    let lang = if Settings::load(&folder).lang.as_deref() == Some("en") {
        "en"
    } else {
        "fr"
    };
    Start {
        lang,
        name,
        password_saved,
        installed: manifest::installed(&game_folder(&app)).map(|m| m.version),
    }
}

/// Keeps the language the player chose, for the next launch.
#[tauri::command]
fn set_lang(app: tauri::AppHandle, lang: String) -> Result<(), Problem> {
    let folder = config_folder(&app);
    let mut settings = Settings::load(&folder);
    settings.lang = Some(if lang == "en" { "en" } else { "fr" }.to_owned());
    settings.save(&folder).map_err(Problem::from)
}

/// Creates an account; the page then comes back to the sign-in form.
#[tauri::command]
async fn create_account(
    app: tauri::AppHandle,
    name: String,
    password: String,
) -> Result<(), Problem> {
    let folder = config_folder(&app);
    tauri::async_runtime::spawn_blocking(move || account::sign_up(&folder, &name, &password))
        .await
        .map_err(joined)??;
    Ok(())
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
struct Updated {
    version: String,
    downloaded: usize,
    received: u64,
}

/// Brings the game folder to the server's version; "progress" events feed the page's bar.
#[tauri::command]
async fn update_game(app: tauri::AppHandle) -> Result<Updated, Problem> {
    let folder = game_folder(&app);
    let server = Settings::load(&config_folder(&app)).server();
    let window = app.clone();
    let report = tauri::async_runtime::spawn_blocking(move || {
        let source = update::HttpSource::new(&server, update::platform());
        update::update(&source, &folder, &mut |p| {
            // A lost event only delays the bar.
            let _ = window.emit("progress", (p.done, p.total, p.file, p.files));
        })
    })
    .await
    .map_err(joined)??;
    Ok(Updated {
        version: report.version,
        downloaded: report.downloaded.len(),
        received: report.received,
    })
}

/// Signs in and remembers what the player ticked; the answer says
/// whether the credential store refused to keep the password.
#[tauri::command]
async fn connect(
    app: tauri::AppHandle,
    session: State<'_, Session>,
    name: String,
    password: String,
    remember_name: bool,
    remember_password: bool,
) -> Result<Option<&'static str>, Problem> {
    let folder = config_folder(&app);
    let (token, complaint) = tauri::async_runtime::spawn_blocking(move || {
        account::sign_in(
            &folder,
            &SystemSecrets,
            &Login {
                name: &name,
                password: &password,
                remember_name,
                remember_password,
            },
        )
    })
    .await
    .map_err(joined)??;
    *session
        .token
        .lock()
        .expect("the session lock is never poisoned") = Some(token);
    Ok(complaint.as_ref().map(code))
}

/// Starts the game signed in, in the page's language.
#[tauri::command]
fn play(app: tauri::AppHandle, session: State<'_, Session>, lang: String) -> Result<(), Problem> {
    let token = session
        .token
        .lock()
        .expect("the session lock is never poisoned")
        .clone();
    let server = Settings::load(&config_folder(&app)).server();
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
            start,
            set_lang,
            create_account,
            update_game,
            connect,
            play
        ])
        .run(tauri::generate_context!())
        .expect("the launcher's window could not open");
}
