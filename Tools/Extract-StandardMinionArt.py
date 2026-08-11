#!/usr/bin/env python3
"""Extract the six reviewed circular standard-minion faces from the private atlas."""

from __future__ import annotations

import argparse
import io
import json
import sys
from collections import deque
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Docs/Guards of Atlantis II - Tabletopia Cards/extracted/text-assets/g713916_s799373.sprite0__45.jpg"
MANIFEST = ROOT / "Docs/Guards of Atlantis II - Tabletopia Cards/extracted/text-assets/manifest-bundled__42.json"
OUTPUT = ROOT / "Assets/Resources/Minions"

# Atlas coordinates come from the matching Image_0_Atlas circular-token sprites.
# Element 896369 is the reviewed Crystal/Blue melee face even though its source name is incorrectly Melee_Steam.
SPRITES = {
    "red-heavy": (896365, "Heavy_Steam", (1217, 0, 256, 256)),
    "red-melee": (896366, "Melee_Steam", (1747, 0, 256, 256)),
    "red-ranged": (8630841, "Ranged_Steam_v2", (3322, 0, 256, 256)),
    "blue-heavy": (896367, "Heavy_Crystal", (2521, 0, 256, 256)),
    "blue-melee": (896369, "Melee_Steam", (3052, 0, 256, 256)),
    "blue-ranged": (8630847, "Ranged_Crystal2", (432, 533, 256, 256)),
}


def load_manifest_entries() -> dict[int, dict]:
    entries = json.loads(MANIFEST.read_text(encoding="utf-8"))
    return {entry["elementId"]: entry for entry in entries if "elementId" in entry}


def verify_manifest() -> None:
    entries = load_manifest_entries()
    for output_name, (element_id, expected_name, rectangle) in SPRITES.items():
        entry = entries.get(element_id)
        if entry is None:
            raise RuntimeError(f"Manifest element {element_id} for {output_name} is unavailable.")
        if entry.get("elementName") != expected_name:
            raise RuntimeError(
                f"Manifest element {element_id} name changed from {expected_name!r} to {entry.get('elementName')!r}.")
        expected_sprite = {"x": rectangle[0], "y": rectangle[1], "height": rectangle[3], "width": rectangle[2]}
        first_atlas = entry.get("atlases", [{}])[0]
        if first_atlas.get("resource", {}).get("id") != "Image_0_Atlas" or expected_sprite not in first_atlas.get("sprites", []):
            raise RuntimeError(f"Manifest sprite coordinates changed for {output_name}.")


def clear_connected_atlas_background(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    width, height = rgba.size
    background = pixels[0, 0][:3]
    queued = deque()
    visited: set[tuple[int, int]] = set()
    for x in range(width):
        queued.append((x, 0))
        queued.append((x, height - 1))
    for y in range(height):
        queued.append((0, y))
        queued.append((width - 1, y))

    def is_background(rgb: tuple[int, int, int]) -> bool:
        return sum((rgb[index] - background[index]) ** 2 for index in range(3)) <= 32 ** 2

    while queued:
        x, y = queued.popleft()
        if (x, y) in visited or not is_background(pixels[x, y][:3]):
            continue
        visited.add((x, y))
        red, green, blue, _ = pixels[x, y]
        pixels[x, y] = (red, green, blue, 0)
        if x > 0: queued.append((x - 1, y))
        if x + 1 < width: queued.append((x + 1, y))
        if y > 0: queued.append((x, y - 1))
        if y + 1 < height: queued.append((x, y + 1))

    return rgba


def png_bytes(image: Image.Image) -> bytes:
    stream = io.BytesIO()
    image.save(stream, format="PNG", optimize=True)
    return stream.getvalue()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true", help="verify manifest mapping and committed outputs without writing")
    args = parser.parse_args()

    if not SOURCE.is_file() or not MANIFEST.is_file():
        raise RuntimeError("The private minion atlas and its manifest must remain at their documented local paths.")
    verify_manifest()
    with Image.open(SOURCE) as atlas:
        if atlas.size != (4096, 4096):
            raise RuntimeError(f"Expected a 4096x4096 atlas, found {atlas.size}.")
        expected_outputs = {}
        for name, (_, _, (x, y, width, height)) in SPRITES.items():
            expected_outputs[name] = png_bytes(clear_connected_atlas_background(atlas.crop((x, y, x + width, y + height))))

    if args.check:
        differences = [name for name, expected in expected_outputs.items()
                       if not (OUTPUT / f"{name}.png").is_file() or (OUTPUT / f"{name}.png").read_bytes() != expected]
        if differences:
            raise RuntimeError(f"Generated minion art differs or is missing: {', '.join(differences)}")
        print("Validated six reviewed standard-minion art crops and the known Crystal melee manifest-label correction.")
        return 0

    OUTPUT.mkdir(parents=True, exist_ok=True)
    for name, contents in expected_outputs.items():
        (OUTPUT / f"{name}.png").write_bytes(contents)
    print(f"Extracted {len(expected_outputs)} standard-minion sprites to {OUTPUT.relative_to(ROOT)}.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exception:
        print(f"ERROR: {exception}", file=sys.stderr)
        raise SystemExit(1)
