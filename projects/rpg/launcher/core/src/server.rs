//! Signing up (`POST /api/accounts`) and signing in (`POST /api/tokens`) on the game's server, as
//! the game itself does.

use std::time::Duration;

use serde::Deserialize;

use crate::update::http_error;
use crate::{Error, Result};

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct AccessToken {
    token: String,
}

fn post(
    server: &str,
    path: &str,
    name: &str,
    password: &str,
) -> std::result::Result<ureq::Response, Box<ureq::Error>> {
    ureq::AgentBuilder::new()
        .timeout(Duration::from_secs(15))
        .build()
        .post(&format!("{}/{path}", server.trim_end_matches('/')))
        .send_json(serde_json::json!({ "name": name, "password": password }))
        .map_err(Box::new)
}

/// Creates an account; the server checks the name (3 to 20 letters, digits, '-' or '_', free
/// whatever its case) and the password (10 to 128 characters).
pub fn sign_up(server: &str, name: &str, password: &str) -> Result<()> {
    post(server, "api/accounts", name, password).map_err(|e| match *e {
        ureq::Error::Status(409, _) => Error::NameTaken,
        ureq::Error::Status(429, _) => Error::TooManyTries,
        ureq::Error::Status(400, response) => Error::Invalid(fields(response)),
        other => http_error(other),
    })?;
    Ok(())
}

/// The fields a validation problem names ("name", "password"), joined by commas.
fn fields(response: ureq::Response) -> String {
    let problem: serde_json::Value = response.into_json().unwrap_or_default();
    let mut names: Vec<String> = problem["errors"]
        .as_object()
        .map(|o| o.keys().cloned().collect())
        .unwrap_or_default();
    names.sort();
    names.join(",")
}

/// The player's access token (a JWT, 12 hours), given to the game when it starts.
pub fn sign_in(server: &str, name: &str, password: &str) -> Result<String> {
    let response = post(server, "api/tokens", name, password).map_err(|e| match *e {
        ureq::Error::Status(401, _) => Error::WrongPassword,
        ureq::Error::Status(429, _) => Error::TooManyTries,
        other => http_error(other),
    })?;
    let token: AccessToken = response
        .into_json()
        .map_err(|e| Error::Unreadable(e.to_string()))?;
    Ok(token.token)
}
