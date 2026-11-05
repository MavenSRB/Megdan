"""Prepare visual-only bush/mushroom cutouts from Veljko's four grass tiles.

The original grass PNGs are never modified. Private cutouts, angle-view clean tiles and
small JSON placement catalogs are regenerated under Resources/Private/Megdan.
"""
import json
from collections import deque
from pathlib import Path

from PIL import Image, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/Resources/Private/Megdan/Grass"
OUTPUT = ROOT / "Assets/Resources/Private/Megdan/GrassStandees"

# Crop bounds are deliberately bounded to the supplied painted bushes and mushrooms. Small
# grass strokes, stones and tools remain part of the flat tile rather than becoming visual noise.
CROPS = {
    1: [(258, 51, 393, 185), (34, 181, 124, 260), (38, 323, 244, 453)],
    2: [(68, 80, 139, 150), (207, 181, 264, 273), (276, 39, 359, 113), (102, 389, 267, 512)],
    3: [(292, 17, 368, 77), (212, 181, 258, 276), (363, 306, 435, 430)],
    4: [(43, 40, 236, 155), (339, 10, 512, 113), (11, 225, 88, 306),
        (260, 337, 405, 467), (133, 418, 235, 496)],
}


def color_distance(left, right):
    return sum((int(a) - int(b)) ** 2 for a, b in zip(left, right)) ** 0.5


def median_ground(image, bounds):
    left, top, right, bottom = bounds
    margin = 14
    samples = []
    for y in range(max(0, top - margin), min(image.height, bottom + margin)):
        for x in range(max(0, left - margin), min(image.width, right + margin)):
            if left <= x < right and top <= y < bottom:
                continue
            red, green, blue, alpha = image.getpixel((x, y))
            if alpha > 200 and green > red * 1.15 and green > blue * 1.2:
                samples.append((red, green, blue))
    if not samples:
        return (86, 165, 78)
    return tuple(sorted(value[channel] for value in samples)[len(samples) // 2]
                 for channel in range(3))


def largest_component(mask):
    width, height = mask.size
    source = mask.load()
    seen = set()
    best = []
    for start_y in range(height):
        for start_x in range(width):
            if source[start_x, start_y] == 0 or (start_x, start_y) in seen:
                continue
            queue = deque([(start_x, start_y)])
            seen.add((start_x, start_y))
            component = []
            while queue:
                x, y = queue.popleft()
                component.append((x, y))
                for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                    if (0 <= nx < width and 0 <= ny < height and
                            source[nx, ny] and (nx, ny) not in seen):
                        seen.add((nx, ny))
                        queue.append((nx, ny))
            if len(component) > len(best):
                best = component
    result = Image.new("L", mask.size)
    pixels = result.load()
    for x, y in best:
        pixels[x, y] = 255
    return result


def extract(image, bounds):
    crop = image.crop(bounds)
    ground = median_ground(image, bounds)
    raw = Image.new("L", crop.size)
    alpha = raw.load()
    for y in range(crop.height):
        for x in range(crop.width):
            red, green, blue, source_alpha = crop.getpixel((x, y))
            if source_alpha > 200 and color_distance((red, green, blue), ground) > 34:
                alpha[x, y] = 255
    # Painted fills and their black outlines form one connected component. Closing joins tiny
    # antialias gaps without admitting unrelated grass marks elsewhere in the bounded crop.
    mask = largest_component(raw.filter(ImageFilter.MaxFilter(3))).filter(ImageFilter.MinFilter(3))
    object_bounds = mask.getbbox()
    if object_bounds is None:
        raise RuntimeError(f"No decoration extracted from {bounds}")
    cutout = crop.crop(object_bounds)
    cutout.putalpha(mask.crop(object_bounds))
    absolute = (bounds[0] + object_bounds[0], bounds[1] + object_bounds[1],
                bounds[0] + object_bounds[2], bounds[1] + object_bounds[3])
    return cutout, mask, absolute, ground


def clean_tile(image, extracted):
    result = image.copy()
    for bounds, mask, ground in extracted:
        # A softly feathered, lightly textured local grass patch is visible only in angled mode,
        # beneath the upright cutout. Top-down mode restores the untouched original tile.
        fill = Image.new("RGBA", mask.size, ground + (255,))
        pixels = fill.load()
        for y in range(fill.height):
            for x in range(fill.width):
                noise = ((x * 17 + y * 29) % 9) - 4
                red, green, blue, _ = pixels[x, y]
                pixels[x, y] = (max(0, min(255, red + noise)),
                                max(0, min(255, green + noise)),
                                max(0, min(255, blue + noise)), 255)
        feather = mask.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(3))
        result.paste(fill, (bounds[0], bounds[1]), feather)
    return result


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for variant, crop_bounds in CROPS.items():
        image = Image.open(SOURCE / f"grass-{variant}.png").convert("RGBA")
        catalog = {"items": []}
        cleaned = []
        for index, bounds in enumerate(crop_bounds, 1):
            cutout, mask, absolute, ground = extract(image, bounds)
            name = f"grass-{variant}-{index}"
            cutout.save(OUTPUT / f"{name}.png")
            left, top, right, bottom = absolute
            catalog["items"].append({
                "resource": f"Private/Megdan/GrassStandees/{name}",
                "x": ((left + right) / image.width) - 1.0,
                "z": 1.0 - (2.0 * bottom / image.height),
                "width": 2.0 * (right - left) / image.width,
                "height": max(0.15, 2.0 * (bottom - top) / image.height),
            })
            cleaned.append((bounds, mask, ground))
        clean_tile(image, cleaned).save(OUTPUT / f"grass-{variant}-clean.png")
        (OUTPUT / f"grass-{variant}.json").write_text(
            json.dumps(catalog, separators=(",", ":")), encoding="utf-8")
        print(f"grass-{variant}: {len(catalog['items'])} standees and clean angle tile")


if __name__ == "__main__":
    main()
