"""Prepare Veljko's four private grass hexes for the runtime point-top mesh.

The supplied JPEGs stay under Art/ and the derived PNGs stay under the ignored
Private Resources tree.  This script is tracked so the local preparation is
repeatable without publishing either the originals or the derived artwork.
"""

from __future__ import annotations

import hashlib
import json
import math
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Art" / "Inputs" / "Grass"
DESTINATION = ROOT / "Assets" / "Resources" / "Private" / "Megdan" / "Grass"
BIOME_DESTINATION = ROOT / "Assets" / "Resources" / "Private" / "Megdan" / "Biomes"
BIOME_SHEET = ROOT / "Art" / "Inputs" / "codex-clipboard-dab18d88-ada4-4c89-8cdf-48abaf2b9205.png"
SIZE = 512
ZONE_BORDERS = {
    "red-throne": "#eb2e1f",
    "red-approach": "#f57a14",
    "starting-battle-zone": "#f5c91f",
    "blue-approach": "#1aadeb",
    "blue-throne": "#1f4deb",
    "top-jungle": "#3dc24d",
    "bottom-jungle": "#0f8a6b",
}


def unique_sources() -> list[Path]:
    unique: dict[str, Path] = {}
    for path in sorted(SOURCE.glob("*.jpeg")):
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        unique.setdefault(digest, path)
    if len(unique) != 4:
        raise RuntimeError(f"Expected four unique grass JPEGs, found {len(unique)}")
    return list(unique.values())


def prepare(source_path: Path) -> Image.Image:
    # Crop inside the supplied cream perimeter: the runtime owns the one border the user kept,
    # around starting Battle Zones, so the texture itself must join cleanly at every other edge.
    source = Image.open(source_path).convert("RGB").crop((60, 45, 964, 838))
    mesh = []
    for y in range(SIZE):
        quad = []
        for yy, right in ((y, False), (y + 1, False), (y + 1, True), (y, True)):
            v = yy / SIZE
            source_inset = abs(v - 0.5) * 0.5
            target_inset = max(0.0, 0.5 - min(v, 1.0 - v) * 2.0)
            scale = (1.0 - 2.0 * source_inset) / max(0.001, 1.0 - 2.0 * target_inset)
            u = 0.5 + ((1.0 if right else 0.0) - 0.5) * scale
            quad.extend((u * source.width, v * source.height))
        mesh.append(((0, y, SIZE, y + 1), tuple(quad)))
    return source.transform((SIZE, SIZE), Image.Transform.MESH, mesh, Image.Resampling.BICUBIC)


def prepare_biomes() -> dict[str, Image.Image]:
    sheet = Image.open(BIOME_SHEET).convert("RGB")
    biomes = {}
    for name, left in (("water", 80), ("desert", 164), ("lava", 247)):
        biomes[name] = sheet.crop((left + 7, 20, left + 56, 72)).resize(
            (SIZE, SIZE), Image.Resampling.LANCZOS
        )
    return biomes


def mix(hash_value: int, value: int) -> int:
    value &= 0xFFFFFFFF
    for shift in (0, 8, 16, 24):
        hash_value = ((hash_value ^ ((value >> shift) & 0xFF)) * 16777619) & 0xFFFFFFFF
    return hash_value


def variant_index(map_id: str, revision: int, q: int, r: int) -> int:
    hash_value = 2166136261
    for character in map_id:
        hash_value = ((hash_value ^ ord(character)) * 16777619) & 0xFFFFFFFF
    return mix(mix(mix(hash_value, revision), q), r) % 4


def render_preview(tiles: list[Image.Image], biomes: dict[str, Image.Image]) -> Path:
    specification = json.loads(
        (ROOT / "Assets" / "Resources" / "Maps" / "official-single-lane.json").read_text()
    )
    radius = 35
    centers = []
    coordinates = {(space["q"], space["r"]) for space in specification["spaces"]}
    zones = {zone["zoneId"]: zone for zone in specification["zones"]}
    for space in specification["spaces"]:
        x = math.sqrt(3) * (space["q"] + space["r"] * 0.5) * radius
        y = 1.5 * space["r"] * radius
        centers.append((space, x, y))
    min_x = min(x for _, x, _ in centers) - radius
    max_x = max(x for _, x, _ in centers) + radius
    min_y = min(y for _, _, y in centers) - radius
    max_y = max(y for _, _, y in centers) + radius
    margin = 48
    canvas = Image.new(
        "RGB", (round(max_x - min_x) + margin * 2, round(max_y - min_y) + margin * 2), "#10161c"
    )
    draw = ImageDraw.Draw(canvas)
    diameter = radius * 2
    mask = Image.new("L", (diameter, diameter), 0)
    mask_draw = ImageDraw.Draw(mask)
    points = [
        (diameter * 0.5, 0),
        (diameter * 0.933, diameter * 0.25),
        (diameter * 0.933, diameter * 0.75),
        (diameter * 0.5, diameter),
        (diameter * 0.067, diameter * 0.75),
        (diameter * 0.067, diameter * 0.25),
    ]
    mask_draw.polygon(points, fill=255)
    # Draw from north to south so shared antialiased edges remain crisp and predictable.
    for space, x, y in sorted(centers, key=lambda item: (item[2], item[1])):
        cx = round(x - min_x) + margin
        cy = round(max_y - y) + margin
        if space["kind"] == "terrain":
            neighbors = (
                (space["q"] + 1, space["r"]),
                (space["q"] + 1, space["r"] - 1),
                (space["q"], space["r"] - 1),
                (space["q"] - 1, space["r"]),
                (space["q"] - 1, space["r"] + 1),
                (space["q"], space["r"] + 1),
            )
            tile = biomes["water"] if any(neighbor not in coordinates for neighbor in neighbors) else biomes["lava"]
        elif zones[space["zoneId"]]["kind"] == "jungle":
            tile = biomes["desert"]
        else:
            tile = tiles[variant_index(
                specification["mapId"], specification["revision"], space["q"], space["r"]
            )]
        tile = tile.resize((diameter, diameter), Image.Resampling.LANCZOS)
        canvas.paste(tile, (cx - radius, cy - radius), mask)
        if space["zoneId"] is not None and zones[space["zoneId"]]["kind"] == "lane":
            border = ZONE_BORDERS[space["zoneId"]]
            translated = [(cx - radius + px, cy - radius + py) for px, py in points]
            draw.line(translated + [translated[0]], fill=border, width=3, joint="curve")
    output = ROOT / "tmp" / "w160-static-grass-board-preview.png"
    output.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(output, optimize=True)
    return output


def main() -> None:
    DESTINATION.mkdir(parents=True, exist_ok=True)
    BIOME_DESTINATION.mkdir(parents=True, exist_ok=True)
    prepared = []
    for index, source in enumerate(unique_sources(), start=1):
        output = DESTINATION / f"grass-{index}.png"
        tile = prepare(source)
        tile.save(output, optimize=True)
        prepared.append(tile)
        print(f"{output.relative_to(ROOT)} <- {source.name}")
    biomes = prepare_biomes()
    for name, tile in biomes.items():
        output = BIOME_DESTINATION / f"{name}.png"
        tile.save(output, optimize=True)
        print(f"{output.relative_to(ROOT)} <- {BIOME_SHEET.name}")
    print(f"Preview: {render_preview(prepared, biomes).relative_to(ROOT)}")


if __name__ == "__main__":
    main()
