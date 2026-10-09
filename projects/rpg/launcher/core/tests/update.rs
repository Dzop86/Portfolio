use std::cell::Cell;
use std::collections::BTreeMap;
use std::fs;
use std::io::{self, Read};
use std::path::Path;

use rpg_launcher_core::manifest::{self, FileEntry, Manifest};
use rpg_launcher_core::update::{self, Progress, Source};
use rpg_launcher_core::Error;

/// The game on a stand-in server: files in memory; a download can be cut after some bytes, and
/// the server can ignore where to resume from.
struct Memory {
    version: String,
    files: BTreeMap<String, Vec<u8>>,
    cut: Cell<Option<(&'static str, usize)>>,
    honours_range: bool,
    opened: Cell<usize>,
}

impl Memory {
    fn new(files: &[(&str, &[u8])]) -> Self {
        Memory {
            version: "1.0.0".into(),
            files: files
                .iter()
                .map(|(p, b)| ((*p).to_owned(), b.to_vec()))
                .collect(),
            cut: Cell::new(None),
            honours_range: true,
            opened: Cell::new(0),
        }
    }
}

/// Gives `bytes`, then fails as a cut connection would.
struct Cut {
    bytes: io::Cursor<Vec<u8>>,
    fails: bool,
}

impl Read for Cut {
    fn read(&mut self, buf: &mut [u8]) -> io::Result<usize> {
        match self.bytes.read(buf)? {
            0 if self.fails => Err(io::Error::new(io::ErrorKind::ConnectionReset, "cut")),
            n => Ok(n),
        }
    }
}

impl Source for Memory {
    fn manifest(&self) -> rpg_launcher_core::Result<Manifest> {
        Ok(Manifest {
            version: self.version.clone(),
            files: self
                .files
                .iter()
                .map(|(path, bytes)| FileEntry {
                    path: path.clone(),
                    size: bytes.len() as u64,
                    sha256: sha(bytes),
                    executable: path.ends_with(".x86_64"),
                })
                .collect(),
        })
    }

    fn open(&self, path: &str, from: u64) -> rpg_launcher_core::Result<(Box<dyn Read>, bool)> {
        self.opened.set(self.opened.get() + 1);
        let all = &self.files[path];
        let start = if self.honours_range { from as usize } else { 0 };
        let mut bytes = all[start..].to_vec();
        let fails = match self.cut.get() {
            Some((file, n)) if file == path => {
                self.cut.set(None);
                bytes.truncate(n);
                true
            }
            _ => false,
        };
        Ok((
            Box::new(Cut {
                bytes: io::Cursor::new(bytes),
                fails,
            }),
            from > 0 && self.honours_range,
        ))
    }
}

fn sha(bytes: &[u8]) -> String {
    let dir = tempfile::tempdir().unwrap();
    let path = dir.path().join("f");
    fs::write(&path, bytes).unwrap();
    manifest::hash_file(&path).unwrap()
}

fn quiet(_: Progress) {}

fn game() -> Memory {
    Memory::new(&[
        ("rpg.x86_64", b"the executable"),
        ("rpg.pck", &[7u8; 200_000]),
        ("data_Rpg.Godot_linuxbsd_x86_64/Rpg.Core.dll", b"rules"),
    ])
}

#[test]
fn a_known_file_has_its_known_sha256() {
    assert_eq!(
        sha(b"abc"),
        "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"
    );
}

#[test]
fn the_first_update_downloads_everything_the_next_one_nothing() {
    let dir = tempfile::tempdir().unwrap();
    let source = game();
    let first = update::update(&source, dir.path(), &mut quiet).unwrap();
    assert_eq!(first.downloaded.len(), 3);
    assert_eq!(first.received, 200_000 + 14 + 5);
    assert_eq!(
        fs::read(
            dir.path()
                .join("data_Rpg.Godot_linuxbsd_x86_64/Rpg.Core.dll")
        )
        .unwrap(),
        b"rules"
    );
    assert_eq!(manifest::installed(dir.path()).unwrap().version, "1.0.0");

    let second = update::update(&source, dir.path(), &mut quiet).unwrap();
    assert!(second.downloaded.is_empty() && second.removed.is_empty());
    assert_eq!(source.opened.get(), 3);
}

#[test]
fn only_the_changed_files_are_downloaded_and_the_old_ones_removed() {
    let dir = tempfile::tempdir().unwrap();
    update::update(&game(), dir.path(), &mut quiet).unwrap();
    let mut next = game();
    next.version = "1.0.1".into();
    next.files.insert("rpg.pck".into(), vec![8u8; 150_000]);
    next.files
        .remove("data_Rpg.Godot_linuxbsd_x86_64/Rpg.Core.dll");
    next.files.insert("new.txt".into(), b"new".to_vec());
    let report = update::update(&next, dir.path(), &mut quiet).unwrap();
    assert_eq!(report.downloaded, ["new.txt", "rpg.pck"]);
    assert_eq!(
        report.removed,
        ["data_Rpg.Godot_linuxbsd_x86_64/Rpg.Core.dll"]
    );
    assert_eq!(
        manifest::game_files(dir.path()).unwrap(),
        ["new.txt", "rpg.pck", "rpg.x86_64"]
    );
    // A file damaged on the disk is downloaded again, whatever its date or size.
    fs::write(dir.path().join("rpg.x86_64"), b"the executablE").unwrap();
    assert_eq!(
        update::update(&next, dir.path(), &mut quiet)
            .unwrap()
            .downloaded,
        ["rpg.x86_64"]
    );
}

#[test]
fn a_cut_download_resumes_where_it_stopped() {
    let dir = tempfile::tempdir().unwrap();
    let source = game();
    source.cut.set(Some(("rpg.pck", 120_000)));
    let error = update::update(&source, dir.path(), &mut quiet).unwrap_err();
    assert!(matches!(error, Error::Unreachable(_)), "{error}");
    assert_eq!(
        fs::metadata(dir.path().join("rpg.pck.part")).unwrap().len(),
        120_000
    );
    assert!(manifest::installed(dir.path()).is_none());

    let report = update::update(&source, dir.path(), &mut quiet).unwrap();
    // The rules' library came whole the first time; the pack resumes; the executable follows.
    assert_eq!(report.downloaded, ["rpg.pck", "rpg.x86_64"]);
    assert_eq!(report.received, 80_000 + 14);
    assert_eq!(
        fs::read(dir.path().join("rpg.pck")).unwrap(),
        vec![7u8; 200_000]
    );
    assert!(!dir.path().join("rpg.pck.part").exists());
}

#[test]
fn a_server_that_cannot_resume_sends_the_file_again() {
    let dir = tempfile::tempdir().unwrap();
    let mut source = game();
    source.honours_range = false;
    source.cut.set(Some(("rpg.pck", 50_000)));
    assert!(update::update(&source, dir.path(), &mut quiet).is_err());
    let report = update::update(&source, dir.path(), &mut quiet).unwrap();
    assert_eq!(report.received, 200_000 + 14);
    assert_eq!(
        fs::read(dir.path().join("rpg.pck")).unwrap(),
        vec![7u8; 200_000]
    );
}

/// A source whose bytes do not match its manifest (a broken mirror, a tampered file).
struct Liar(Memory);

impl Source for Liar {
    fn manifest(&self) -> rpg_launcher_core::Result<Manifest> {
        self.0.manifest()
    }

    fn open(&self, path: &str, from: u64) -> rpg_launcher_core::Result<(Box<dyn Read>, bool)> {
        let mut bytes = self.0.files[path].clone();
        bytes[0] ^= 1;
        Ok((
            Box::new(io::Cursor::new(bytes[from as usize..].to_vec())),
            from > 0,
        ))
    }
}

#[test]
fn a_file_that_does_not_match_its_sha256_never_replaces_the_installed_one() {
    let dir = tempfile::tempdir().unwrap();
    fs::write(dir.path().join("rpg.x86_64"), b"old").unwrap();
    let error = update::update(
        &Liar(Memory::new(&[("rpg.x86_64", b"the executable")])),
        dir.path(),
        &mut quiet,
    )
    .unwrap_err();
    assert!(
        matches!(error, Error::Corrupt(ref p) if p == "rpg.x86_64"),
        "{error}"
    );
    assert_eq!(fs::read(dir.path().join("rpg.x86_64")).unwrap(), b"old");
    assert!(!dir.path().join("rpg.x86_64.part").exists());
}

#[test]
fn a_manifest_cannot_write_outside_the_game_folder() {
    let base = tempfile::tempdir().unwrap();
    let dir = base.path().join("game");
    for path in [
        "../evil",
        "/etc/evil",
        "a/../../evil",
        "a\\evil",
        "C:evil",
        "",
    ] {
        let error = update::update(&Memory::new(&[(path, b"x")]), &dir, &mut quiet).unwrap_err();
        assert!(matches!(error, Error::UnsafePath(_)), "{path}: {error}");
    }
    assert!(!base.path().join("evil").exists());
    assert_eq!(
        manifest::safe_path(Path::new("g"), "a/b.txt").unwrap(),
        Path::new("g").join("a/b.txt")
    );
}

#[test]
fn the_progress_ends_with_every_byte() {
    let dir = tempfile::tempdir().unwrap();
    let mut seen = Vec::new();
    update::update(&game(), dir.path(), &mut |p| seen.push(p)).unwrap();
    let last = *seen.last().unwrap();
    assert_eq!(
        (last.done, last.total, last.file, last.files),
        (200_019, 200_019, 3, 3)
    );
    assert!(seen.windows(2).all(|w| w[0].done <= w[1].done));
}

#[cfg(unix)]
#[test]
fn the_executable_can_be_run_after_its_download() {
    use std::os::unix::fs::PermissionsExt;
    let dir = tempfile::tempdir().unwrap();
    update::update(&game(), dir.path(), &mut quiet).unwrap();
    let mode = |p: &str| {
        fs::metadata(dir.path().join(p))
            .unwrap()
            .permissions()
            .mode()
    };
    assert_ne!(mode("rpg.x86_64") & 0o111, 0);
    assert_eq!(mode("rpg.pck") & 0o111, 0);
}

#[test]
fn the_manifest_of_a_folder_lists_its_files_in_order() {
    let dir = tempfile::tempdir().unwrap();
    fs::create_dir_all(dir.path().join("b")).unwrap();
    fs::write(dir.path().join("b/z.txt"), b"z").unwrap();
    fs::write(dir.path().join("a.txt"), b"abc").unwrap();
    fs::write(dir.path().join("c.bin.part"), b"unfinished").unwrap();
    fs::write(dir.path().join(manifest::FILE_NAME), b"{}").unwrap();
    let m = manifest::build(dir.path(), "2.0").unwrap();
    assert_eq!(
        m.files.iter().map(|f| f.path.as_str()).collect::<Vec<_>>(),
        ["a.txt", "b/z.txt"]
    );
    assert_eq!(
        m.files[0].sha256,
        "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"
    );
    assert_eq!(m.files[0].size, 3);
}
