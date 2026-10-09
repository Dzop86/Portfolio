//! Signing in to the game's server (`POST /api/tokens`), as the game itself does.

use std::time::Duration;

use serde::Deserialize;

use crate::update::http_error;
use crate::{Error, Result};

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct AccessToken {
    token: String,
}

/// The player's access token (a JWT, 12 hours), given to the game when it starts.
pub fn sign_in(server: &str, name: &str, password: &str) -> Result<String> {
    let agent = ureq::AgentBuilder::new()
        .timeout(Duration::from_secs(15))
        .build();
    let url = format!("{}/api/tokens", server.trim_end_matches('/'));
    let response = agent
        .post(&url)
        .send_json(serde_json::json!({ "name": name, "password": password }))
        .map_err(|e| match e {
            ureq::Error::Status(401, _) => Error::WrongPassword,
            ureq::Error::Status(429, _) => Error::TooManyTries,
            other => http_error(other),
        })?;
    let token: AccessToken = response
        .into_json()
        .map_err(|e| Error::Unreadable(e.to_string()))?;
    Ok(token.token)
}
