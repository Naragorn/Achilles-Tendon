#!/usr/bin/env bash
# Small GIFs for Nexus from harness films (BasHarness record-start/record-stop). The runner's own GIFs
# are 960 px wide and 17-185 MB per film; these are 540 px, 12 fps, 128 colours.
#   tools/film-gifs.sh <out folder> <film.mp4>[:speed] ...
# speed > 1 is a time-lapse (e.g. bleed-npc.mp4:6 plays the 33 s bleed-out in 5.5 s, 6.8 MB; at :4 it was
# 10.4 MB, over the 8 MB per image that Nexus takes according to a third-party Nexus tool, not official).
# The GIF is named after the film: cut-npc.mp4 -> cut.gif, fates-first.mp4 -> fates.gif.
set -euo pipefail
out=$1
shift
mkdir -p "$out"
for arg in "$@"; do
  src=${arg%%:*}
  speed=1
  if [[ $arg == *:* ]]; then speed=${arg##*:}; fi
  name=$(basename "$src" .mp4)
  name=${name%-npc}
  name=${name%-first}
  name=${name%-side}
  ffmpeg -loglevel error -y -i "$src" -vf "setpts=PTS/$speed,fps=12,scale=540:-2:flags=lanczos,split[a][b];[a]palettegen=max_colors=128:stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=4:diff_mode=rectangle" "$out/$name.gif"
  echo "$out/$name.gif $(du -k "$out/$name.gif" | cut -f1) KB"
done
