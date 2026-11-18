#!/usr/bin/env python3
"""Turns screenshot bursts from a harness run into GIFs.

  tools/make-gifs.py <run folder> <out folder> [--width 800]

A burst is every PNG named g<name>-<nn>.png (scenario lines `screenshot g<name>-<nn> ...`);
each burst becomes <out>/<name>.gif in frame order. The frame delay comes from the scenario's
`wait` step between shots (passed per burst below, default 80 ms); the game's real pace while
screenshotting may differ, so the GIF speed is approximate.
"""
import argparse
import re
from collections import defaultdict
from pathlib import Path

from PIL import Image

DELAY_MS = {"gcrawl": 150}  # the crawl burst is 0.3 s apart: shown at double speed
DEFAULT_DELAY_MS = 80


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("run", type=Path)
    ap.add_argument("out", type=Path)
    ap.add_argument("--width", type=int, default=640)
    a = ap.parse_args()
    bursts = defaultdict(list)
    for p in sorted(a.run.rglob("g*-*.png")):
        m = re.fullmatch(r"(g[a-z]+)-(\d+)\.png", p.name)
        if m:
            bursts[m.group(1)].append(p)
    a.out.mkdir(parents=True, exist_ok=True)
    for name, files in bursts.items():
        frames = []
        for f in files:
            im = Image.open(f).convert("RGB")
            h = round(im.height * a.width / im.width)
            frames.append(im.resize((a.width, h), Image.LANCZOS).quantize(colors=128, dither=Image.FLOYDSTEINBERG))
        dst = a.out / f"{name[1:]}.gif"
        frames[0].save(dst, save_all=True, append_images=frames[1:], loop=0,
                       duration=DELAY_MS.get(name, DEFAULT_DELAY_MS), optimize=True)
        print(f"{dst} {len(frames)} frames {dst.stat().st_size // 1024} KB")


if __name__ == "__main__":
    main()
