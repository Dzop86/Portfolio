//! The launcher against a small HTTP server: the same requests as against the game's server
//! (`/updates/{platform}/...` with `Range`, `POST /api/tokens`).

use std::collections::BTreeMap;
use std::fs;
use std::sync::{Arc, Mutex};
use std::thread;

use rpg_launcher_core::manifest::{self, Manifest};
use rpg_launcher_core::update::{self, HttpSource};
use rpg_launcher_core::{launch, server, Error};
use tiny_http::{Header, Response, Server};

/// Serves `files` under `/updates/linux/`, honouring `Range: bytes=N-` (206), and signs in
/// "ada"/"correct horse battery"; "busy" is always refused with 429. Records the Range headers seen.
fn serve(files: BTreeMap<String, Vec<u8>>) -> (String, Arc<Mutex<Vec<String>>>) {
    let server = Server::http("127.0.0.1:0").unwrap();
    let url = format!("http://{}", server.server_addr().to_ip().unwrap());
    let ranges = Arc::new(Mutex::new(Vec::new()));
    let seen = Arc::clone(&ranges);
    thread::spawn(move || {
        for mut request in server.incoming_requests() {
            let path = request.url().trim_start_matches('/').to_owned();
            if path == "api/tokens" {
                let mut body = String::new();
                request.as_reader().read_to_string(&mut body).unwrap();
                let response = if body.contains("\"busy\"") {
                    Response::from_string("{}").with_status_code(429)
                } else if body.contains("\"ada\"") && body.contains("correct horse battery") {
                    Response::from_string(
                        r#"{"token":"jwt.for.ada","expiresAt":"2026-10-10T00:00:00Z"}"#,
                    )
                } else {
                    Response::from_string(r#"{"title":"Unknown name or wrong password."}"#)
                        .with_status_code(401)
                };
                request.respond(response).unwrap();
                continue;
            }
            let Some(name) = path.strip_prefix("updates/linux/") else {
                request.respond(Response::empty(404)).unwrap();
                continue;
            };
            let name = name.replace("%20", " ");
            let Some(bytes) = files.get(&name) else {
                request.respond(Response::empty(404)).unwrap();
                continue;
            };
            let from = request
                .headers()
                .iter()
                .find(|h| h.field.equiv("Range"))
                .map(|h| h.value.as_str().to_owned());
            match from
                .as_deref()
                .and_then(|r| r.strip_prefix("bytes="))
                .and_then(|r| r.trim_end_matches('-').parse::<usize>().ok())
            {
                Some(start) => {
                    seen.lock().unwrap().push(format!("{name} from {start}"));
                    let range = Header::from_bytes(
                        "Content-Range",
                        format!("bytes {start}-{}/{}", bytes.len() - 1, bytes.len()),
                    )
                    .unwrap();
                    request
                        .respond(
                            Response::from_data(bytes[start..].to_vec())
                                .with_status_code(206)
                                .with_header(range),
                        )
                        .unwrap();
                }
                None => request.respond(Response::from_data(bytes.clone())).unwrap(),
            }
        }
    });
    (url, ranges)
}

fn published(game: &[(&str, &[u8])]) -> BTreeMap<String, Vec<u8>> {
    let dir = tempfile::tempdir().unwrap();
    for (path, bytes) in game {
        fs::write(dir.path().join(path), bytes).unwrap();
    }
    let m = manifest::build(dir.path(), "1.2.3").unwrap();
    let mut files: BTreeMap<String, Vec<u8>> = game
        .iter()
        .map(|(p, b)| ((*p).to_owned(), b.to_vec()))
        .collect();
    files.insert(manifest::FILE_NAME.into(), serde_json::to_vec(&m).unwrap());
    files
}

#[test]
fn the_game_is_installed_over_http_and_a_cut_file_resumes_with_a_range() {
    let (url, ranges) = serve(published(&[
        ("rpg.x86_64", b"game"),
        ("rpg data.pck", &[3u8; 100_000]),
    ]));
    let source = HttpSource::new(&url, "linux");
    let dir = tempfile::tempdir().unwrap();
    // As if the last launch had been cut in the middle of the pack.
    fs::write(dir.path().join("rpg data.pck.part"), vec![3u8; 30_000]).unwrap();
    let report = update::update(&source, dir.path(), &mut |_| {}).unwrap();
    assert_eq!(report.version, "1.2.3");
    assert_eq!(report.received, 70_000 + 4);
    assert_eq!(*ranges.lock().unwrap(), ["rpg data.pck from 30000"]);
    assert_eq!(
        fs::read(dir.path().join("rpg data.pck")).unwrap(),
        vec![3u8; 100_000]
    );
    let installed: Manifest = manifest::installed(dir.path()).unwrap();
    assert_eq!(installed.files.len(), 2);
}

#[test]
fn signing_in_gives_the_token_or_says_why_not() {
    let (url, _) = serve(BTreeMap::new());
    assert_eq!(
        server::sign_in(&url, "ada", "correct horse battery").unwrap(),
        "jwt.for.ada"
    );
    assert!(matches!(
        server::sign_in(&url, "ada", "wrong").unwrap_err(),
        Error::WrongPassword
    ));
    assert!(matches!(
        server::sign_in(&url, "busy", "x").unwrap_err(),
        Error::TooManyTries
    ));
    // Nobody listens on this port.
    assert!(matches!(
        server::sign_in("http://127.0.0.1:9", "ada", "x").unwrap_err(),
        Error::Unreachable(_)
    ));
    let missing = HttpSource::new(&url, "windows");
    assert!(matches!(
        update::update(&missing, tempfile::tempdir().unwrap().path(), &mut |_| {}).unwrap_err(),
        Error::Server(404)
    ));
}

#[test]
fn the_game_starts_with_the_token_in_its_environment_not_its_arguments() {
    let dir = std::path::Path::new("game");
    let command = launch::command(dir, "linux", "http://localhost:8002", Some("jwt"), "fr");
    assert_eq!(command.get_program(), dir.join("rpg.x86_64"));
    let args: Vec<_> = command
        .get_args()
        .map(|a| a.to_string_lossy().into_owned())
        .collect();
    assert_eq!(args, ["--", "--lang", "fr"]);
    let env: BTreeMap<_, _> = command
        .get_envs()
        .map(|(k, v)| {
            (
                k.to_string_lossy().into_owned(),
                v.map(|v| v.to_string_lossy().into_owned()),
            )
        })
        .collect();
    assert_eq!(env["RPG_TOKEN"].as_deref(), Some("jwt"));
    assert_eq!(env["RPG_SERVER"].as_deref(), Some("http://localhost:8002"));
    assert_eq!(launch::executable(dir, "windows"), dir.join("rpg.exe"));
    assert_eq!(
        launch::executable(dir, "macos"),
        dir.join("rpg.app/Contents/MacOS/rpg")
    );
    // On macOS, whatever the export named the application and its program.
    let mac = tempfile::tempdir().unwrap();
    fs::create_dir_all(mac.path().join("Rpg.app/Contents/MacOS")).unwrap();
    fs::write(mac.path().join("Rpg.app/Contents/MacOS/Rpg"), b"").unwrap();
    assert_eq!(
        launch::executable(mac.path(), "macos"),
        mac.path().join("Rpg.app/Contents/MacOS/Rpg")
    );
    let offline = launch::command(dir, "linux", "http://localhost:8002", None, "en");
    assert!(offline
        .get_envs()
        .any(|(k, v)| k == "RPG_TOKEN" && v.is_none()));
}
