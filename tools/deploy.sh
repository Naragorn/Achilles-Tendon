#!/usr/bin/env bash
# Builds Achilles (Release) and copies AchillesTendon.dll + manifest.json into BOTH game installs: the Linux
# Steam copy and the Windows Steam copy (Vortex manages that Mods folder; the mod lies next to the others).
# Refuses uncommitted src changes, then checks that all copies have the same md5. Template: DMCS tools/deploy.sh.
#   tools/deploy.sh
set -euo pipefail
ROOT=$(cd "$(dirname "$0")/.." && pwd)
LINUX_MODS="${ACHILLES_LINUX_MODS:-$HOME/.local/share/Steam/steamapps/common/Blade & Sorcery/BladeAndSorcery_Data/StreamingAssets/Mods}"
WINDOWS_MODS="${ACHILLES_WINDOWS_MODS:-/run/media/nadi/Programs/Steam/steamapps/common/Blade & Sorcery/BladeAndSorcery_Data/StreamingAssets/Mods}"
cd "$ROOT"
if [ -n "$(git status --porcelain -- src)" ]; then
  echo "deploy: src has uncommitted changes; commit first" >&2
  exit 1
fi
export DOTNET_ROOT=${DOTNET_ROOT:-$HOME/.dotnet}
export PATH="$DOTNET_ROOT:$PATH" DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
dotnet build src/AchillesTendon/AchillesTendon.csproj -c Release -nologo -v q
DIST=dist/AchillesTendon
for mods in "$LINUX_MODS" "$WINDOWS_MODS"; do
  if [ ! -d "$mods" ]; then
    echo "deploy: Mods folder not found: $mods" >&2
    exit 1
  fi
  mkdir -p "$mods/AchillesTendon"
  cp "$DIST/AchillesTendon.dll" "$DIST/manifest.json" "$mods/AchillesTendon/"
done
want=$(md5sum "$DIST/AchillesTendon.dll" | cut -d' ' -f1)
for mods in "$LINUX_MODS" "$WINDOWS_MODS"; do
  got=$(md5sum "$mods/AchillesTendon/AchillesTendon.dll" | cut -d' ' -f1)
  [ "$got" = "$want" ] || { echo "deploy: md5 mismatch in $mods" >&2; exit 1; }
done
echo "deployed $(git rev-parse --short HEAD) (AchillesTendon.dll md5 $want) to Linux and Windows"
