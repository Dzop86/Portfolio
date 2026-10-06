"""Finds the shared libraries of lib-c and topologie (built by CMake), unless the environment points to them."""
import os
import pathlib

import pytest

ROOT = pathlib.Path(__file__).resolve().parents[2]  # projects/
DATA = ROOT / "lib-c" / "tests" / "data"
SAMPLES = ROOT / "topologie" / "samples"
LIBRARIES = {
    # environment variable: (project, file names on Linux, macOS, Windows)
    "MESHLIB_PATH": ("lib-c", ("libmesh.so", "libmesh.dylib", "mesh.dll")),
    "TOPOLIB_PATH": ("topologie", ("libtopoc.so", "libtopoc.dylib", "topoc.dll")),
}


def _find_library(project: str, names: tuple[str, ...]) -> str:
    for build in ROOT.joinpath(project).glob("build*"):
        for name in names:
            found = sorted(build.rglob(name))
            if found:
                return str(found[0])
    raise RuntimeError(f"{project} shared library not found: build it as shown in README.md")


for variable, (project, names) in LIBRARIES.items():
    if variable not in os.environ:
        os.environ[variable] = _find_library(project, names)
# The shape classifier exported by the ML project, committed in git.
os.environ.setdefault("MODEL_PATH", str(ROOT / "ml" / "export" / "pointnet.onnx"))


@pytest.fixture
def sample():
    return lambda name: (DATA / name).read_bytes()


@pytest.fixture
def topo_sample():
    return lambda name: (SAMPLES / name).read_bytes()
