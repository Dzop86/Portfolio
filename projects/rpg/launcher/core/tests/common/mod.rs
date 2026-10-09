//! A small HTTP server that answers like the game's server, for the tests.

use std::collections::BTreeMap;
use std::sync::{Arc, Mutex};
use std::thread;

use tiny_http::{Header, Response, Server};

/// Serves `files` under `/updates/linux/`, honouring `Range: bytes=N-` (206), and signs in
/// "ada"/"correct horse battery" and the accounts created meanwhile (`POST /api/accounts`: "taken"
/// is taken, "x" is refused as invalid); "busy" is always refused with 429. Records the Range
/// headers seen.
pub fn serve(files: BTreeMap<String, Vec<u8>>) -> (String, Arc<Mutex<Vec<String>>>) {
    let server = Server::http("127.0.0.1:0").unwrap();
    let url = format!("http://{}", server.server_addr().to_ip().unwrap());
    let ranges = Arc::new(Mutex::new(Vec::new()));
    let seen = Arc::clone(&ranges);
    let accounts = Mutex::new(vec![("ada".to_owned(), "correct horse battery".to_owned())]);
    thread::spawn(move || {
        for mut request in server.incoming_requests() {
            let path = request.url().trim_start_matches('/').to_owned();
            if path == "api/accounts" || path == "api/tokens" {
                let mut body = String::new();
                request.as_reader().read_to_string(&mut body).unwrap();
                let form: serde_json::Value = serde_json::from_str(&body).unwrap();
                let (name, password) = (
                    form["name"].as_str().unwrap().to_owned(),
                    form["password"].as_str().unwrap().to_owned(),
                );
                let mut known = accounts.lock().unwrap();
                let response = if name == "busy" {
                    Response::from_string("{}").with_status_code(429)
                } else if path == "api/accounts" {
                    if name == "taken" || known.iter().any(|(n, _)| *n == name) {
                        Response::from_string(r#"{"title":"This name is already taken."}"#)
                            .with_status_code(409)
                    } else if name == "x" {
                        Response::from_string(r#"{"title":"One or more validation errors occurred.","errors":{"password":["10 to 128 characters."],"name":["3 to 20 characters."]}}"#).with_status_code(400)
                    } else {
                        known.push((name, password));
                        Response::from_string(r#"{"name":"created"}"#).with_status_code(201)
                    }
                } else if known.iter().any(|(n, p)| *n == name && *p == password) {
                    Response::from_string(format!(
                        r#"{{"token":"jwt.for.{name}","expiresAt":"2026-10-10T00:00:00Z"}}"#
                    ))
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
