#!/usr/bin/env bash
# Builds the Linux AppImage from a Release build tree:
#   packaging/appimage.sh BUILD_DIR OUT_DIR      ->  OUT_DIR/Mesh_viewer-x86_64.AppImage
# linuxdeploy copies the program, its Qt libraries and plugins into an AppDir, then packs it.
set -euo pipefail
build=$(realpath "$1")
out=$(realpath -m "$2")
tools="$out/tools"
mkdir -p "$tools"

fetch() {
  [ -x "$tools/$2" ] || { curl -fsSL -o "$tools/$2" "$1"; chmod +x "$tools/$2"; }
}
# Pinned releases, not "continuous": the same tools on every build.
fetch https://github.com/linuxdeploy/linuxdeploy/releases/download/1-alpha-20251107-1/linuxdeploy-x86_64.AppImage linuxdeploy
fetch https://github.com/linuxdeploy/linuxdeploy-plugin-qt/releases/download/1-alpha-20250213-1/linuxdeploy-plugin-qt-x86_64.AppImage linuxdeploy-plugin-qt

rm -rf "$out/AppDir"
DESTDIR="$out/AppDir" cmake --install "$build" --prefix /usr

# Containers and CI runners have no FUSE: the tools unpack themselves instead of mounting.
export APPIMAGE_EXTRACT_AND_RUN=1
export QMAKE="${QMAKE:-$(command -v qmake6 || command -v qmake)}"
# A Qt installed outside the system (aqtinstall, install-qt-action) is not in the loader's paths,
# and `cmake --install` removed the build tree's RPATH.
export LD_LIBRARY_PATH="$("$QMAKE" -query QT_INSTALL_LIBS)${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
export OUTPUT="$out/Mesh_viewer-x86_64.AppImage"
cd "$out"
PATH="$tools:$PATH" linuxdeploy --appdir AppDir --plugin qt --output appimage \
  --desktop-file AppDir/usr/share/applications/qtviewer.desktop \
  --icon-file AppDir/usr/share/icons/hicolor/256x256/apps/qtviewer.png
echo "$OUTPUT"
