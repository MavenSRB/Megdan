"""Prepare Veljko's resized minion illustrations and matching top-down tokens.

The supplied PNGs and prepared resources stay private and Git-ignored. Supplied
standee pixels are copied unchanged and only their edge geometry is derived. Token
sources are preserved unchanged, then cropped to their common visible circle and
downsampled to the existing 256-pixel runtime footprint.
"""
import json
import shutil
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Art/Supplied20261110/MinionResizes"
FALLBACK_SOURCE = ROOT / "Art/Supplied20261109/MinionStandees"
STANDEE_OUTPUT = ROOT / "Assets/Resources/Private/Megdan/MinionStandees"
TOKEN_OUTPUT = ROOT / "Assets/Resources/Minions"
STANDEE_SOURCES = {
    "blue-heavy": SOURCE / "heavy 1.png",
    # The 2026-11-10 transfer contains no blue Melee standee. Preserve W-187's
    # supplied source rather than inventing or recolouring a replacement.
    "blue-melee": FALLBACK_SOURCE / "blue-melee.png",
    "blue-ranged": SOURCE / "range 1.png",
    "red-heavy": SOURCE / "heavy 2.png",
    "red-melee": SOURCE / "meelee 2.png",
    "red-ranged": SOURCE / "range 2.png",
}
TOKEN_SOURCES = {
    "blue-heavy": SOURCE / "h token 2.PNG",
    "blue-melee": SOURCE / "m token 2.PNG",
    "blue-ranged": SOURCE / "r token 2.PNG",
    "red-heavy": SOURCE / "h token 1.PNG",
    "red-melee": SOURCE / "m token 1.PNG",
    "red-ranged": SOURCE / "r token 1.PNG",
}


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


def prepare_token(source: Path, destination: Path) -> None:
    with Image.open(source) as image:
        rgba = image.convert("RGBA")
        bounds = rgba.getchannel("A").getbbox()
        if bounds is None:
            raise ValueError(f"{source.name} has no visible token pixels")
        cropped = rgba.crop(bounds)
        if cropped.width != cropped.height:
            raise ValueError(
                f"{source.name} visible bounds must be square, got {cropped.size}"
            )
        cropped.resize((256, 256), Image.Resampling.LANCZOS).save(destination)


def main() -> None:
    STANDEE_OUTPUT.mkdir(parents=True, exist_ok=True)
    TOKEN_OUTPUT.mkdir(parents=True, exist_ok=True)
    for name, source in STANDEE_SOURCES.items():
        if not source.exists():
            raise FileNotFoundError(source)
        shutil.copyfile(source, STANDEE_OUTPUT / f"{name}.png")
        (STANDEE_OUTPUT / f"{name}.json").write_text(
            json.dumps(derive_outline(source)), encoding="utf-8"
        )
        print(f"{name}: standee PNG and panel outline prepared")
    for name, source in TOKEN_SOURCES.items():
        if not source.exists():
            raise FileNotFoundError(source)
        prepare_token(source, TOKEN_OUTPUT / f"{name}.png")
        print(f"{name}: matching 256x256 token prepared")


if __name__ == "__main__":
    main()
