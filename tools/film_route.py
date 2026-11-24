"""Routes the harness game's sound to the film sink while tools/film-with-sound.sh records.

    film_route.py <film sink name> <recording start, UTC epoch s>

The harness game's audio stream (application.name "BladeAndSorcery") ignores PULSE_SINK under Proton
and the Steam runtime, and the sound server restores it muted (the stored setting for that stream,
read 2026-11-24 in ~/.local/state/wireplumber/stream-properties). So each new game stream is moved
to the film sink and unmuted; when its scenario logs its RESULT (just before the game quits), the
stream is muted again and moved back to the sink it came from, so the stored setting ends as it was.
SIGTERM does the same for a stream still open.
"""
import json
import pathlib
import signal
import subprocess
import sys
import time

GAME = "BladeAndSorcery"
# The running game's own log (the runner copies it into the artifacts only after the game quits).
LIVE_LOG = pathlib.Path.home() / ".local/share/bas-harness/compatdata/pfx/drive_c/users/steamuser/AppData/LocalLow/Warpfrog/BladeAndSorcery/Player.log"


def game_streams():
    out = subprocess.run(["pactl", "-f", "json", "list", "sink-inputs"], capture_output=True, text=True).stdout
    try:
        inputs = json.loads(out or "[]")
    except json.JSONDecodeError:
        return []
    return [i for i in inputs if i.get("properties", {}).get("application.name") == GAME]


def pactl(*args):
    subprocess.run(["pactl", *args], capture_output=True)


def main():
    sink, start = sys.argv[1], float(sys.argv[2])
    routed = {}  # sink-input index -> (original sink, original mute)
    finished = set()

    def restore(index):
        original_sink, original_mute = routed.pop(index)
        finished.add(index)
        pactl("set-sink-input-mute", str(index), "1" if original_mute else "0")
        pactl("move-sink-input", str(index), str(original_sink))
        print(f"route: stream {index} back to sink {original_sink}, mute {original_mute}", flush=True)

    def stop(*_):
        for index in list(routed):
            restore(index)
        sys.exit(0)

    signal.signal(signal.SIGTERM, stop)
    signal.signal(signal.SIGINT, stop)
    while True:
        for stream in game_streams():
            index = stream["index"]
            if index not in routed and index not in finished and stream.get("sink") is not None:
                routed[index] = (stream["sink"], bool(stream.get("mute")))
                pactl("move-sink-input", str(index), sink)
                pactl("set-sink-input-mute", str(index), "0")
                print(f"route: stream {index} (sink {stream['sink']}, mute {stream.get('mute')}) -> {sink}", flush=True)
        # The scenario's RESULT line comes just before the game quits (Unity starts a new log per launch).
        if routed and LIVE_LOG.exists() and LIVE_LOG.stat().st_mtime >= start:
            if "[BasHarness] RESULT" in LIVE_LOG.read_text(encoding="utf-8", errors="replace"):
                for index in list(routed):
                    restore(index)
        time.sleep(0.2)


if __name__ == "__main__":
    main()
