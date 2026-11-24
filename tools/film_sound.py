"""Lays the recorded game sound under each harness film of a run (see tools/film-with-sound.sh).

    film_sound.py <run dir> <recording.wav> <recording start, UTC epoch seconds>

For every film (a record-start step in a scenario's Player.log) it writes <name>-sound.mp4 next to the
film and prints where the logged sounds (COLLAPSE CRY, VOICE, SCREAM, GASP) fall in it, plus the
loudest jump in the recording around each of them, as a check that picture and sound line up.
"""
import array
import datetime
import pathlib
import re
import subprocess
import sys
import wave

STEP = re.compile(r"^(\S+) .*STEP \d+/\d+ PASS line \d+: (record-start (\S+)|record-stop)")
SOUND = re.compile(r"^(\S+) .*\[AchillesTendon\] ((COLLAPSE CRY|VOICE|SCREAM|GASP) \|.*)$")


def epoch(stamp):
    # Player.log times are UTC (checked against the wall clock, 2026-11-24).
    t = datetime.datetime.strptime(stamp, "%Y-%m-%dT%H:%M:%S.%f")
    return t.replace(tzinfo=datetime.timezone.utc).timestamp()


def films(log):
    """(name, start, stop) of every film in a Player.log."""
    found, name, start = [], None, None
    for line in log.read_text(encoding="utf-8", errors="replace").splitlines():
        m = STEP.match(line)
        if not m:
            continue
        if m.group(3):
            name, start = m.group(3), epoch(m.group(1))
        elif name:
            found.append((name, start, epoch(m.group(1))))
            name = None
    return found


def sounds(log, start, stop):
    out = []
    for line in log.read_text(encoding="utf-8", errors="replace").splitlines():
        m = SOUND.match(line)
        if m and start <= epoch(m.group(1)) <= stop:
            out.append((epoch(m.group(1)) - start, m.group(2)[:110]))
    return out


def loudness(wav_path):
    """RMS per 10 ms window of the recording (mono mix)."""
    with wave.open(str(wav_path)) as w:
        rate, channels = w.getframerate(), w.getnchannels()
        data = array.array("h", w.readframes(w.getnframes()))
    step = rate // 100 * channels
    rms = []
    for i in range(0, len(data) - step, step):
        chunk = data[i:i + step]
        rms.append((sum(s * s for s in chunk) / len(chunk)) ** 0.5)
    return rms


def jump_near(rms, at, before=0.3, after=0.6):
    """Time (s) of the biggest rise in loudness from one 10 ms window to the next around `at`."""
    lo, hi = max(1, int((at - before) * 100)), min(len(rms) - 1, int((at + after) * 100))
    if hi <= lo:
        return None
    best = max(range(lo, hi), key=lambda i: rms[i] - rms[i - 1])
    return best / 100, rms[best]


def main():
    run, wav_path, rec_start = pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2]), float(sys.argv[3])
    rms = loudness(wav_path)
    print(f"recording: {len(rms) / 100:.1f} s, loudest window RMS {max(rms, default=0):.0f}")
    for log in sorted(run.glob("*/Player.log")):
        for name, start, stop in films(log):
            folder = log.parent
            film = folder / f"{name}.mp4"
            if not film.exists():
                views = sorted(p for p in folder.glob(f"{name}-*.mp4") if not p.stem.endswith(("-slow", "-sound")))
                if not views:
                    print(f"{folder.name}/{name}: no film found")
                    continue
                film = views[0]
            offset = start - rec_start
            out = folder / f"{name}-sound.mp4"
            subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", str(film), "-ss", f"{offset:.3f}",
                            "-i", str(wav_path), "-map", "0:v", "-map", "1:a", "-c:v", "copy", "-c:a", "aac",
                            "-b:a", "192k", "-shortest", str(out)], check=True)
            print(f"{out}  (film {stop - start:.1f} s, sound from {offset:.3f} s into the recording)")
            for at, text in sounds(log, start, stop):
                jump = jump_near(rms, offset + at)
                heard = f" | loudest rise at {jump[0] - offset:.2f} s (RMS {jump[1]:.0f})" if jump else ""
                print(f"  {at:5.2f} s  {text}{heard}")


if __name__ == "__main__":
    main()
