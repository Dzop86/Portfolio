//! Brings an installed game to the server's version: only the files that changed are downloaded,
//! a cut download resumes where it stopped, every file is checked against its SHA-256 before it
//! replaces the old one.

use std::fs::{self, OpenOptions};
use std::io::{self, Read, Write};
use std::path::{Path, PathBuf};
use std::time::Duration;

use crate::manifest::{self, FileEntry, Manifest, Plan, PART};
use crate::{Error, Result};

/// Where the files come from: the server, or a stand-in in the tests.
pub trait Source {
    fn manifest(&self) -> Result<Manifest>;

    /// The file from byte `from` on; `true` if the source honoured `from`, `false` if it sends the
    /// whole file again (then the download starts over).
    fn open(&self, path: &str, from: u64) -> Result<(Box<dyn Read>, bool)>;
}

/// The server's `/updates/{platform}/` folder, over HTTP; resumes with a `Range` header.
pub struct HttpSource {
    base: String,
    agent: ureq::Agent,
}

impl HttpSource {
    /// `server` like `http://localhost:8002`, `platform` one of `linux`, `windows`, `macos`.
    pub fn new(server: &str, platform: &str) -> Self {
        HttpSource {
            base: format!("{}/updates/{platform}/", server.trim_end_matches('/')),
            agent: ureq::AgentBuilder::new()
                .timeout_connect(Duration::from_secs(10))
                .timeout_read(Duration::from_secs(30))
                .build(),
        }
    }

    fn get(&self, path: &str) -> ureq::Request {
        self.agent.get(&format!("{}{}", self.base, encode(path)))
    }
}

/// The platform this launcher was built for, as the update folders name it.
pub fn platform() -> &'static str {
    if cfg!(target_os = "windows") {
        "windows"
    } else if cfg!(target_os = "macos") {
        "macos"
    } else {
        "linux"
    }
}

/// Percent-encodes each segment of a path (spaces and the like in exported file names).
fn encode(path: &str) -> String {
    let mut out = String::new();
    for b in path.bytes() {
        if b.is_ascii_alphanumeric() || b"-._~/".contains(&b) {
            out.push(b as char);
        } else {
            out.push_str(&format!("%{b:02X}"));
        }
    }
    out
}

pub(crate) fn http_error(e: ureq::Error) -> Error {
    match e {
        ureq::Error::Status(code, _) => Error::Server(code),
        ureq::Error::Transport(t) => Error::Unreachable(t.to_string()),
    }
}

impl Source for HttpSource {
    fn manifest(&self) -> Result<Manifest> {
        let response = self.get(manifest::FILE_NAME).call().map_err(http_error)?;
        response
            .into_json()
            .map_err(|e| Error::Unreadable(e.to_string()))
    }

    fn open(&self, path: &str, from: u64) -> Result<(Box<dyn Read>, bool)> {
        let mut request = self.get(path);
        if from > 0 {
            request = request.set("Range", &format!("bytes={from}-"));
        }
        let response = request.call().map_err(http_error)?;
        let resumed = from > 0 && response.status() == 206;
        Ok((Box::new(response.into_reader()), resumed))
    }
}

/// Progress, for the window's bar: bytes received so far out of the bytes to download.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct Progress {
    pub done: u64,
    pub total: u64,
    pub file: usize,
    pub files: usize,
}

/// What an update did.
#[derive(Debug, PartialEq, Eq)]
pub struct Report {
    pub version: String,
    pub downloaded: Vec<String>,
    pub removed: Vec<String>,
    /// Bytes actually received: less than the files' sizes when a download resumed.
    pub received: u64,
}

/// Downloads one file into `dir`, resuming its `.part` if there is one, and checks it.
/// Returns the bytes received.
pub fn fetch(
    source: &dyn Source,
    dir: &Path,
    entry: &FileEntry,
    on_bytes: &mut dyn FnMut(u64),
) -> Result<u64> {
    let dest = manifest::safe_path(dir, &entry.path)?;
    if let Some(parent) = dest.parent() {
        fs::create_dir_all(parent)?;
    }
    let part = part_path(&dest);
    let mut from = fs::metadata(&part).map(|m| m.len()).unwrap_or(0);
    if from >= entry.size {
        from = 0;
    }
    let (mut reader, resumed) = source.open(&entry.path, from)?;
    if !resumed {
        from = 0;
    }
    let mut file = OpenOptions::new()
        .create(true)
        .append(from > 0)
        .write(true)
        .truncate(from == 0)
        .open(&part)?;
    let mut received = 0u64;
    let mut buffer = vec![0u8; 64 * 1024];
    loop {
        let read = match reader.read(&mut buffer) {
            Ok(0) => break,
            Ok(n) => n,
            Err(e) if e.kind() == io::ErrorKind::Interrupted => continue,
            Err(e) => return Err(Error::Unreachable(e.to_string())),
        };
        if from + received + read as u64 > entry.size {
            drop(file);
            fs::remove_file(&part)?;
            return Err(Error::Corrupt(entry.path.clone()));
        }
        file.write_all(&buffer[..read])?;
        received += read as u64;
        on_bytes(read as u64);
    }
    file.sync_all()?;
    drop(file);
    if manifest::hash_file(&part)? != entry.sha256 {
        fs::remove_file(&part)?;
        return Err(Error::Corrupt(entry.path.clone()));
    }
    fs::rename(&part, &dest)?;
    if entry.executable {
        make_executable(&dest)?;
    }
    Ok(received)
}

fn part_path(dest: &Path) -> PathBuf {
    let mut name = dest.as_os_str().to_owned();
    name.push(PART);
    PathBuf::from(name)
}

#[cfg(unix)]
fn make_executable(path: &Path) -> io::Result<()> {
    use std::os::unix::fs::PermissionsExt;
    let mut permissions = fs::metadata(path)?.permissions();
    permissions.set_mode(permissions.mode() | 0o755);
    fs::set_permissions(path, permissions)
}

#[cfg(not(unix))]
fn make_executable(_: &Path) -> io::Result<()> {
    Ok(())
}

/// Brings `dir` to the source's version; the local manifest is written last, so an update cut
/// halfway is simply taken up again next time.
pub fn update(
    source: &dyn Source,
    dir: &Path,
    on_progress: &mut dyn FnMut(Progress),
) -> Result<Report> {
    let manifest = source.manifest()?;
    fs::create_dir_all(dir)?;
    let Plan {
        fetch: files,
        remove,
    } = manifest::plan(dir, &manifest)?;
    for path in &remove {
        fs::remove_file(manifest::safe_path(dir, path)?)?;
    }
    let mut progress = Progress {
        done: 0,
        total: files.iter().map(|f| f.size).sum(),
        file: 0,
        files: files.len(),
    };
    on_progress(progress);
    let mut received = 0;
    for (i, entry) in files.iter().enumerate() {
        progress.file = i + 1;
        let before = progress.done;
        received += fetch(source, dir, entry, &mut |n| {
            progress.done += n;
            on_progress(progress);
        })?;
        // A resumed file counts whole.
        progress.done = before + entry.size;
        on_progress(progress);
    }
    let text =
        serde_json::to_string_pretty(&manifest).map_err(|e| Error::Unreadable(e.to_string()))?;
    fs::write(dir.join(manifest::FILE_NAME), text)?;
    Ok(Report {
        version: manifest.version,
        downloaded: files.into_iter().map(|f| f.path).collect(),
        removed: remove,
        received,
    })
}
