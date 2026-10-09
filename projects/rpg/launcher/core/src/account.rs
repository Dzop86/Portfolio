//! What the launcher remembers between two launches: the account name, in a small settings file,
//! and, if the player asks, the password, in the system's credential store (Windows Credential
//! Manager, macOS Keychain, the Secret Service on Linux), never in a file.

use std::fs;
use std::path::Path;

use serde::{Deserialize, Serialize};

use crate::{server, Error, Result};

/// The settings file, in the launcher's configuration folder.
pub const FILE_NAME: &str = "launcher.json";

/// The service name under which passwords are stored in the credential store.
pub const SERVICE: &str = "osmose-launcher";

/// The launcher's settings: the server (hidden from the player, for tests and the next servers)
/// and the account name to fill in.
#[derive(Debug, Clone, Default, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Settings {
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub server: Option<String>,
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub name: Option<String>,
    /// The page's language, "fr" (the default) or "en", as the player last chose it.
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub lang: Option<String>,
}

impl Settings {
    /// Reads the settings; a missing or unreadable file gives the defaults.
    pub fn load(dir: &Path) -> Self {
        fs::read_to_string(dir.join(FILE_NAME))
            .ok()
            .and_then(|t| serde_json::from_str(&t).ok())
            .unwrap_or_default()
    }

    pub fn save(&self, dir: &Path) -> Result<()> {
        fs::create_dir_all(dir)?;
        let text =
            serde_json::to_string_pretty(self).map_err(|e| Error::Unreadable(e.to_string()))?;
        fs::write(dir.join(FILE_NAME), text)?;
        Ok(())
    }

    /// The server to use: `RPG_SERVER`, else the settings file, else the local one.
    pub fn server(&self) -> String {
        std::env::var("RPG_SERVER")
            .ok()
            .filter(|s| !s.is_empty())
            .or_else(|| self.server.clone())
            .unwrap_or_else(|| "http://localhost:8002".into())
    }
}

/// Where passwords are kept: the system's credential store, or memory in the tests.
pub trait Secrets {
    fn get(&self, name: &str) -> Option<String>;
    fn set(&self, name: &str, password: &str) -> Result<()>;
    /// Forgets a password; forgetting one that was never kept is fine.
    fn delete(&self, name: &str) -> Result<()>;
}

/// The system's credential store, through the keyring crate (feature `system-secrets`).
#[cfg(feature = "system-secrets")]
pub struct SystemSecrets;

#[cfg(feature = "system-secrets")]
impl Secrets for SystemSecrets {
    fn get(&self, name: &str) -> Option<String> {
        keyring::Entry::new(SERVICE, name).ok()?.get_password().ok()
    }

    fn set(&self, name: &str, password: &str) -> Result<()> {
        keyring::Entry::new(SERVICE, name)
            .and_then(|e| e.set_password(password))
            .map_err(|e| Error::Secrets(e.to_string()))
    }

    fn delete(&self, name: &str) -> Result<()> {
        match keyring::Entry::new(SERVICE, name).and_then(|e| e.delete_credential()) {
            Ok(()) | Err(keyring::Error::NoEntry) => Ok(()),
            Err(e) => Err(Error::Secrets(e.to_string())),
        }
    }
}

/// What the player asked on the form.
#[derive(Debug, Clone)]
pub struct Login<'a> {
    pub name: &'a str,
    /// Empty: the password remembered for this name.
    pub password: &'a str,
    pub remember_name: bool,
    pub remember_password: bool,
}

/// Creates an account on the launcher's server; the player then signs in, as with any account.
pub fn sign_up(dir: &Path, name: &str, password: &str) -> Result<()> {
    server::sign_up(&Settings::load(dir).server(), name, password)
}

/// Signs in, then remembers what the player chose to remember and
/// forgets the rest. A credential store that refuses does not stop the sign-in: the token comes
/// back with the store's complaint, for the window to show.
pub fn sign_in(
    dir: &Path,
    secrets: &dyn Secrets,
    login: &Login,
) -> Result<(String, Option<Error>)> {
    let mut settings = Settings::load(dir);
    let server = settings.server();
    let saved;
    let password = if login.password.is_empty() {
        saved = secrets.get(login.name).ok_or(Error::MissingPassword)?;
        saved.as_str()
    } else {
        login.password
    };
    let token = server::sign_in(&server, login.name, password)?;

    // Another account remembered before: its password goes, whatever happens to this one.
    let mut complaint = None;
    if let Some(previous) = settings.name.clone().filter(|p| p != login.name) {
        complaint = secrets.delete(&previous).err();
    }
    let kept = if login.remember_password {
        secrets.set(login.name, password)
    } else {
        secrets.delete(login.name)
    };
    complaint = kept.err().or(complaint);
    settings.name = (login.remember_name || login.remember_password).then(|| login.name.to_owned());
    settings.save(dir)?;
    Ok((token, complaint))
}

/// The name to fill in and whether a password is remembered for it.
pub fn remembered(dir: &Path, secrets: &dyn Secrets) -> (Option<String>, bool) {
    let name = Settings::load(dir).name;
    let saved = name.as_deref().is_some_and(|n| secrets.get(n).is_some());
    (name, saved)
}
