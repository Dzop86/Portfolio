//! What the launcher remembers: the name in a file, the password in a credential store (here in
//! memory), and only what the player asked for.

use std::collections::BTreeMap;
use std::fs;
use std::sync::Mutex;

use rpg_launcher_core::account::{self, Login, Secrets, Settings};
use rpg_launcher_core::{server, Error};

mod common;

/// A credential store in memory; can be told to refuse, as a locked keyring would.
#[derive(Default)]
struct Memory {
    kept: Mutex<BTreeMap<String, String>>,
    refuses: bool,
}

impl Secrets for Memory {
    fn get(&self, name: &str) -> Option<String> {
        self.kept.lock().unwrap().get(name).cloned()
    }

    fn set(&self, name: &str, password: &str) -> rpg_launcher_core::Result<()> {
        if self.refuses {
            return Err(Error::Secrets("locked".into()));
        }
        self.kept
            .lock()
            .unwrap()
            .insert(name.into(), password.into());
        Ok(())
    }

    fn delete(&self, name: &str) -> rpg_launcher_core::Result<()> {
        self.kept.lock().unwrap().remove(name);
        Ok(())
    }
}

/// A launcher folder whose settings point at the test server.
fn folder(url: &str) -> tempfile::TempDir {
    let dir = tempfile::tempdir().unwrap();
    Settings {
        server: Some(url.into()),
        ..Settings::default()
    }
    .save(dir.path())
    .unwrap();
    dir
}

fn login<'a>(
    name: &'a str,
    password: &'a str,
    remember_name: bool,
    remember_password: bool,
) -> Login<'a> {
    Login {
        name,
        password,
        remember_name,
        remember_password,
    }
}

#[test]
fn nothing_is_remembered_unless_asked() {
    let (url, _) = common::serve(BTreeMap::new());
    let dir = folder(&url);
    let secrets = Memory::default();
    let (token, complaint) = account::sign_in(
        dir.path(),
        &secrets,
        &login("ada", "correct horse battery", false, false),
    )
    .unwrap();
    assert_eq!((token.as_str(), complaint.is_none()), ("jwt.for.ada", true));
    assert_eq!(account::remembered(dir.path(), &secrets), (None, false));
    // The file keeps the server, never a password.
    let text = fs::read_to_string(dir.path().join(account::FILE_NAME)).unwrap();
    assert!(!text.contains("correct horse"));
}

#[test]
fn the_name_and_the_password_come_back_and_the_password_alone_signs_in() {
    let (url, _) = common::serve(BTreeMap::new());
    let dir = folder(&url);
    let secrets = Memory::default();
    account::sign_in(
        dir.path(),
        &secrets,
        &login("ada", "correct horse battery", true, true),
    )
    .unwrap();
    assert_eq!(
        account::remembered(dir.path(), &secrets),
        (Some("ada".into()), true)
    );
    assert!(!fs::read_to_string(dir.path().join(account::FILE_NAME))
        .unwrap()
        .contains("correct horse"));
    // Next launch: the password field left empty, the remembered one is used.
    let (token, _) = account::sign_in(dir.path(), &secrets, &login("ada", "", true, true)).unwrap();
    assert_eq!(token, "jwt.for.ada");
}

#[test]
fn unticking_forgets_the_password_and_another_account_forgets_the_first() {
    let (url, _) = common::serve(BTreeMap::new());
    let dir = folder(&url);
    let secrets = Memory::default();
    account::sign_in(
        dir.path(),
        &secrets,
        &login("ada", "correct horse battery", true, true),
    )
    .unwrap();
    account::sign_in(dir.path(), &secrets, &login("ada", "", true, false)).unwrap();
    assert_eq!(
        account::remembered(dir.path(), &secrets),
        (Some("ada".into()), false)
    );
    assert!(matches!(
        account::sign_in(dir.path(), &secrets, &login("ada", "", true, false)).unwrap_err(),
        Error::MissingPassword
    ));

    account::sign_in(
        dir.path(),
        &secrets,
        &login("ada", "correct horse battery", true, true),
    )
    .unwrap();
    account::sign_up(dir.path(), "grace", "another long password").unwrap();
    account::sign_in(
        dir.path(),
        &secrets,
        &login("grace", "another long password", true, true),
    )
    .unwrap();
    assert_eq!(
        secrets.kept.lock().unwrap().keys().collect::<Vec<_>>(),
        ["grace"]
    );
}

#[test]
fn a_wrong_password_remembers_nothing() {
    let (url, _) = common::serve(BTreeMap::new());
    let dir = folder(&url);
    let secrets = Memory::default();
    assert!(matches!(
        account::sign_in(
            dir.path(),
            &secrets,
            &login("ada", "wrong password", true, true)
        )
        .unwrap_err(),
        Error::WrongPassword
    ));
    assert_eq!(account::remembered(dir.path(), &secrets), (None, false));
}

#[test]
fn a_locked_credential_store_does_not_stop_the_game() {
    let (url, _) = common::serve(BTreeMap::new());
    let dir = folder(&url);
    let secrets = Memory {
        refuses: true,
        ..Memory::default()
    };
    let (token, complaint) = account::sign_in(
        dir.path(),
        &secrets,
        &login("ada", "correct horse battery", true, true),
    )
    .unwrap();
    assert_eq!(token, "jwt.for.ada");
    assert!(matches!(complaint, Some(Error::Secrets(_))));
}

#[test]
fn signing_up_creates_the_account_then_the_player_signs_in() {
    let (url, _) = common::serve(BTreeMap::new());
    let dir = folder(&url);
    let secrets = Memory::default();
    account::sign_up(dir.path(), "ondine", "a long new password").unwrap();
    // Signing up remembers nothing and signs nobody in: the form comes back.
    assert_eq!(account::remembered(dir.path(), &secrets), (None, false));
    assert_eq!(
        account::sign_in(
            dir.path(),
            &secrets,
            &login("ondine", "a long new password", true, false)
        )
        .unwrap()
        .0,
        "jwt.for.ondine"
    );
    assert_eq!(
        account::remembered(dir.path(), &secrets),
        (Some("ondine".into()), false)
    );
    assert!(matches!(
        server::sign_up(&url, "taken", "a long new password").unwrap_err(),
        Error::NameTaken
    ));
    assert!(
        matches!(server::sign_up(&url, "x", "short").unwrap_err(), Error::Invalid(fields) if fields == "name,password")
    );
    assert!(matches!(
        server::sign_up(&url, "busy", "a long new password").unwrap_err(),
        Error::TooManyTries
    ));
}

#[test]
fn the_server_is_the_environments_then_the_files_then_the_local_one() {
    let dir = tempfile::tempdir().unwrap();
    // RPG_SERVER is not set while the tests run (the CI does not set it).
    assert_eq!(Settings::load(dir.path()).server(), "http://localhost:8002");
    fs::write(dir.path().join(account::FILE_NAME), "not json").unwrap();
    assert_eq!(Settings::load(dir.path()), Settings::default());
    Settings {
        server: Some("http://osmose.example".into()),
        ..Settings::default()
    }
    .save(dir.path())
    .unwrap();
    assert_eq!(Settings::load(dir.path()).server(), "http://osmose.example");
}
