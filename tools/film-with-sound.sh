#!/usr/bin/env bash
# Films harness scenarios with the game's sound. BasHarness films (record-start/record-stop) have no
# audio track, so this records the game's audio and lays it under each film of the run:
#   tools/film-with-sound.sh harness/showcase/films/film-5-cry.txt
# -> next to every film <name>-<view>.mp4 a <name>-sound.mp4; the whole recording stays as
# artifacts/harness/sound-<time>.wav.
# The game's stream is moved into a PipeWire/PulseAudio null sink of its own and unmuted for the run
# (tools/film_route.py; muted again and moved back before the game quits), so nothing reaches the
# speakers and nothing else is recorded. The sink is removed again at the end. Film and sound are aligned by the wall clock: the record-start step's log time
# against the moment the recording started (a few tens of ms off at most; tools/film_sound.py prints
# where the logged sounds fall in each film, to check by ear).
# FILM_ROOT=<another checkout> films that checkout's build (e.g. an older version for a "before" film).
set -euo pipefail
ROOT=$(cd "$(dirname "$0")/.." && pwd)
FILM_ROOT=${FILM_ROOT:-$ROOT}
SINK=achilles_film
mkdir -p "$FILM_ROOT/artifacts/harness"
WAV="$FILM_ROOT/artifacts/harness/sound-$(date -u +%Y%m%d-%H%M%S).wav"

DEFAULT_SINK=$(pactl get-default-sink)
MODULE=$(pactl load-module module-null-sink "sink_name=$SINK" "sink_properties=device.description=AchillesFilm")
REC=
ROUTE=
cleanup() {
  if [ -n "$ROUTE" ]; then kill "$ROUTE" 2>/dev/null || true; wait "$ROUTE" 2>/dev/null || true; fi
  if [ -n "$REC" ]; then kill "$REC" 2>/dev/null || true; wait "$REC" 2>/dev/null || true; fi
  pactl unload-module "$MODULE" 2>/dev/null || true
  if [ "$(pactl get-default-sink)" != "$DEFAULT_SINK" ]; then pactl set-default-sink "$DEFAULT_SINK"; fi
}
trap cleanup EXIT
# A new sink must not take over the desktop's sound.
if [ "$(pactl get-default-sink)" != "$DEFAULT_SINK" ]; then pactl set-default-sink "$DEFAULT_SINK"; fi

START=$(date -u +%s.%N)
parecord -d "$SINK.monitor" --rate=48000 --channels=2 --format=s16le --file-format=wav "$WAV" &
REC=$!
python3 -I "$ROOT/tools/film_route.py" "$SINK" "$START" &
ROUTE=$!

status=0
"$FILM_ROOT/tools/harness.sh" "$@" || status=$?

kill "$ROUTE" 2>/dev/null || true
wait "$ROUTE" 2>/dev/null || true
ROUTE=

kill -INT "$REC" 2>/dev/null || true
wait "$REC" 2>/dev/null || true
REC=
RUN=$(ls -td "$FILM_ROOT"/artifacts/harness/*/ | sed -n 1p)
python3 -I "$ROOT/tools/film_sound.py" "$RUN" "$WAV" "$START"
exit $status
