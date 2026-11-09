"""Copy Veljko's six transparent minion illustrations and derive panel outlines.

The supplied PNGs and prepared PNG/JSON resources stay private and Git-ignored. The
source pixels are copied unchanged; only the edge geometry is derived here.
"""
import json
import shutil
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Art/Supplied20261109/MinionStandees"
OUTPUT = ROOT / "Assets/Resources/Private/Megdan/MinionStandees"
NAMES = (
    "blue-heavy",
    "blue-melee",
    "blue-ranged",
    "red-heavy",
    "red-melee",
    "red-ranged",
)


def derive_outline(source: Path) -> dict:
    with Image.open(source) as image:
        alpha = image.convert("RGBA").getchannel("A")
        bounds = alpha.point(lambda value: 255 if value >= 96 else 0).getbbox()
        if bounds is None:
            raise ValueError(f"{source.name} has no opaque standee pixels")
        left, top, right, bottom = bounds
        mask = alpha.crop(bounds).resize((128, 128), Image.Resampling.NEAREST)

        def solid(x: int, y: int) -> bool:
            return 0 <= x < 128 and 0 <= y < 128 and mask.getpixel((x, y)) >= 96

        edges = []
        for y in range(128):
            for x in range(128):
                if not solid(x, y):
                    continue
                for dx, dy, edge in (
                    (0, -1, (x, y, x + 1, y)),
                    (1, 0, (x + 1, y, x + 1, y + 1)),
                    (0, 1, (x + 1, y + 1, x, y + 1)),
                    (-1, 0, (x, y + 1, x, y)),
                ):
                    if not solid(x + dx, y + dy):
                        edges.extend(
                            (edge[0] / 128, 1 - edge[1] / 128,
                             edge[2] / 128, 1 - edge[3] / 128)
                        )

        return {
            "left": left / image.width,
            "bottom": 1 - bottom / image.height,
            "width": (right - left) / image.width,
            "height": (bottom - top) / image.height,
            "aspect": (right - left) / (bottom - top),
            "edges": edges,
        }


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for name in NAMES:
        source = SOURCE / f"{name}.png"
        if not source.exists():
            raise FileNotFoundError(source)
        shutil.copyfile(source, OUTPUT / f"{name}.png")
        (OUTPUT / f"{name}.json").write_text(
            json.dumps(derive_outline(source)), encoding="utf-8"
        )
        print(f"{name}: original PNG and panel outline prepared")


if __name__ == "__main__":
    main()
