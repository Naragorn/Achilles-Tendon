#!/usr/bin/env bash
# Builds Achilles (Release) and packs a zip for testers:
#   tools/package.sh beta      -> release/AchillesTendon-<ModVersion>-beta.zip
#   tools/package.sh           -> release/AchillesTendon-<ModVersion>.zip
# The zip holds the AchillesTendon folder (DLL + manifest, no pdb) to copy into the game's Mods folder,
# and README.md. Refuses uncommitted changes in src or README.md, so a zip is always a commit.
set -euo pipefail
ROOT=$(cd "$(dirname "$0")/.." && pwd)
cd "$ROOT"
if [ -n "$(git status --porcelain -- src README.md)" ]; then
  echo "package: src or README.md has uncommitted changes; commit first" >&2
  exit 1
fi
export DOTNET_ROOT=${DOTNET_ROOT:-$HOME/.dotnet}
export PATH="$DOTNET_ROOT:$PATH" DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
dotnet build src/AchillesTendon/AchillesTendon.csproj -c Release -nologo -v q
DIST=dist/AchillesTendon
VERSION=$(python3 -I -c 'import json,sys; print(json.load(open(sys.argv[1], encoding="utf-8-sig"))["ModVersion"])' "$DIST/manifest.json")
NAME="AchillesTendon-$VERSION${1:+-$1}"
STAGE=$(mktemp -d)
trap 'rm -rf "$STAGE"' EXIT
mkdir -p "$STAGE/AchillesTendon" release
cp "$DIST/AchillesTendon.dll" "$DIST/manifest.json" "$STAGE/AchillesTendon/"
cp README.md "$STAGE/"
if strings -n 6 "$STAGE/AchillesTendon/AchillesTendon.dll" | grep -qE "/run/|/home/"; then
  echo "package: the DLL still names a local folder" >&2
  exit 1
fi
rm -f "release/$NAME.zip"
(cd "$STAGE" && zip -q -X -r "$ROOT/release/$NAME.zip" AchillesTendon README.md)
echo "packed release/$NAME.zip from $(git rev-parse --short HEAD) (AchillesTendon.dll md5 $(md5sum "$DIST/AchillesTendon.dll" | cut -d' ' -f1), zip sha256 $(sha256sum "release/$NAME.zip" | cut -d' ' -f1))"
