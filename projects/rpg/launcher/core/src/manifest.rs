//! A version of the game as the server publishes it: every file, its size and its SHA-256.

use std::fs;
use std::io::{self, Read};
use std::path::{Component, Path, PathBuf};

use serde::{Deserialize, Serialize};
use sha2::{Digest, Sha256};

use crate::{Error, Result};

/// The name of the manifest, on the server and in the installed folder (the version installed).
pub const FILE_NAME: &str = "manifest.json";

/// A download not finished yet ends with this; it is resumed next time.
pub const PART: &str = ".part";

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
pub struct Manifest {
    pub version: String,
    pub files: Vec<FileEntry>,
}

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
pub struct FileEntry {
    /// Relative to the game folder, with '/' between folders.
    pub path: String,
    pub size: u64,
    /// Lower-case hexadecimal.
    pub sha256: String,
    /// The game's executable (and on macOS what sits beside it): made executable after a download.
    #[serde(default, skip_serializing_if = "std::ops::Not::not")]
    pub executable: bool,
}

/// What an update has to do: the files to download, the files no longer in the game.
#[derive(Debug, Default, PartialEq, Eq)]
pub struct Plan {
    pub fetch: Vec<FileEntry>,
    pub remove: Vec<String>,
}

impl Plan {
    pub fn bytes(&self) -> u64 {
        self.fetch.iter().map(|f| f.size).sum()
    }
}

/// The SHA-256 of a file, in lower-case hexadecimal.
pub fn hash_file(path: &Path) -> io::Result<String> {
    let mut file = fs::File::open(path)?;
    let mut hasher = Sha256::new();
    let mut buffer = vec![0u8; 64 * 1024];
    loop {
        let read = file.read(&mut buffer)?;
        if read == 0 {
            break;
        }
        hasher.update(&buffer[..read]);
    }
    Ok(hex(&hasher.finalize()))
}

pub(crate) fn hex(bytes: &[u8]) -> String {
    bytes.iter().map(|b| format!("{b:02x}")).collect()
}

/// The manifest of a game folder (an export of the Godot client), files in a fixed order.
pub fn build(dir: &Path, version: &str) -> Result<Manifest> {
    let mut files = Vec::new();
    for relative in game_files(dir)? {
        let path = dir.join(&relative);
        let metadata = fs::metadata(&path)?;
        files.push(FileEntry {
            size: metadata.len(),
            sha256: hash_file(&path)?,
            executable: is_executable(&metadata),
            path: relative,
        });
    }
    Ok(Manifest {
        version: version.to_owned(),
        files,
    })
}

/// The game's files in a folder, relative and sorted: neither the manifest nor unfinished downloads.
pub fn game_files(dir: &Path) -> Result<Vec<String>> {
    let mut files = Vec::new();
    walk(dir, dir, &mut files)?;
    files.retain(|f| f != FILE_NAME && !f.ends_with(PART));
    files.sort();
    Ok(files)
}

fn walk(root: &Path, dir: &Path, files: &mut Vec<String>) -> Result<()> {
    for entry in fs::read_dir(dir)? {
        let entry = entry?;
        if entry.file_type()?.is_dir() {
            walk(root, &entry.path(), files)?;
        } else {
            files.push(relative(root, &entry.path()));
        }
    }
    Ok(())
}

fn relative(root: &Path, path: &Path) -> String {
    let parts: Vec<String> = path
        .strip_prefix(root)
        .unwrap_or(path)
        .components()
        .map(|c| c.as_os_str().to_string_lossy().into_owned())
        .collect();
    parts.join("/")
}

#[cfg(unix)]
fn is_executable(metadata: &fs::Metadata) -> bool {
    use std::os::unix::fs::PermissionsExt;
    metadata.permissions().mode() & 0o111 != 0
}

#[cfg(not(unix))]
fn is_executable(_: &fs::Metadata) -> bool {
    false
}

/// Where a file of the manifest goes; refuses anything that would leave the game folder.
pub fn safe_path(dir: &Path, relative: &str) -> Result<PathBuf> {
    let unsafe_path = || Error::UnsafePath(relative.to_owned());
    if relative.is_empty() || relative.contains('\\') || relative.contains(':') {
        return Err(unsafe_path());
    }
    let path = Path::new(relative);
    if !path.components().all(|c| matches!(c, Component::Normal(_))) {
        return Err(unsafe_path());
    }
    Ok(dir.join(path))
}

/// Compares the installed folder with a manifest: what to download (missing, other size, other
/// hash) and what to remove (files the new version no longer has, unfinished downloads aside).
pub fn plan(dir: &Path, manifest: &Manifest) -> Result<Plan> {
    let mut plan = Plan::default();
    for entry in &manifest.files {
        let path = safe_path(dir, &entry.path)?;
        let same = match fs::metadata(&path) {
            Ok(m) if m.is_file() && m.len() == entry.size => hash_file(&path)? == entry.sha256,
            _ => false,
        };
        if !same {
            plan.fetch.push(entry.clone());
        }
    }
    if dir.is_dir() {
        for file in game_files(dir)? {
            if !manifest.files.iter().any(|f| f.path == file) {
                plan.remove.push(file);
            }
        }
    }
    Ok(plan)
}

/// The version installed in a folder, if a complete update ever finished there.
pub fn installed(dir: &Path) -> Option<Manifest> {
    let text = fs::read_to_string(dir.join(FILE_NAME)).ok()?;
    serde_json::from_str(&text).ok()
}
