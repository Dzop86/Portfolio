//! Osmose's launcher, without a window: it signs in or signs up ([`server`]), remembers the account
//! ([`account`]: the name in a settings file, the password in the system's credential store), compares
//! the installed game with the server's manifest ([`manifest`]), downloads only the files that
//! changed, resuming a cut download and checking each file's SHA-256 ([`update`]), then starts the
//! game with the player's token ([`launch`]). The Tauri window (`launcher/app`) only draws it.

pub mod account;
pub mod launch;
pub mod manifest;
pub mod server;
pub mod update;

/// Everything that can go wrong, in words the window can show.
#[derive(Debug, thiserror::Error)]
pub enum Error {
    #[error("the server cannot be reached: {0}")]
    Unreachable(String),
    #[error("unknown name or wrong password")]
    WrongPassword,
    #[error("this account name is already taken")]
    NameTaken,
    #[error("refused by the server: {0}")]
    Invalid(String),
    #[error("no password given, and none remembered for this account")]
    MissingPassword,
    #[error("the system's credential store: {0}")]
    Secrets(String),
    #[error("too many tries: wait a minute")]
    TooManyTries,
    #[error("the server answered {0}")]
    Server(u16),
    #[error("unreadable answer from the server: {0}")]
    Unreadable(String),
    #[error("the file {0} does not match its SHA-256: downloaded again next time")]
    Corrupt(String),
    #[error("the manifest names a file outside the game folder: {0}")]
    UnsafePath(String),
    #[error("file system: {0}")]
    Io(#[from] std::io::Error),
}

pub type Result<T> = std::result::Result<T, Error>;
