#!/usr/bin/env bash
# Small GIFs for Nexus from harness films (BasHarness record-start/record-stop). The runner's own GIFs
# are 960 px wide and 17-185 MB per film; these are 540 px, 12 fps (unless given), 128 colours.
#   tools/film-gifs.sh <out folder> <film.mp4>[:speed[:fps]] ...
# speed > 1 is a time-lapse (e.g. bleed-npc.mp4:6 plays the 33 s bleed-out in 5.5 s, 6.8 MB; at :4 it was
# 10.4 MB, over the 8 MB per image that Nexus takes according to a third-party Nexus tool, not official).
# fps below 12 keeps a long film under that limit (hunt-npc.mp4:1:11, 6.7 s, was 8.2 MB at 12 fps).
# The GIF is named after the film: cut-npc.mp4 -> cut.gif, fates-first.mp4 -> fates.gif.
set -euo pipefail
out=$1
shift
mkdir -p "$out"
for arg in "$@"; do
  IFS=: read -r src speed fps <<< "$arg"
  speed=${speed:-1}
  fps=${fps:-12}
  name=$(basename "$src" .mp4)
  name=${name%-npc}
  name=${name%-first}
  name=${name%-side}
  ffmpeg -loglevel error -y -i "$src" -vf "setpts=PTS/$speed,fps=$fps,scale=540:-2:flags=lanczos,split[a][b];[a]palettegen=max_colors=128:stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=4:diff_mode=rectangle" "$out/$name.gif"
  echo "$out/$name.gif $(du -k "$out/$name.gif" | cut -f1) KB"
done
