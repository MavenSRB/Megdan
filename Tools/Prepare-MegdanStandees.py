"""Copy original transparent portraits unchanged and derive thin-panel edge geometry.

Private PNG/JSON outputs stay excluded from Git. No artwork generation or repainting.
"""
import json
import shutil
import sys
from pathlib import Path
from PIL import Image
import importlib.util

sys.dont_write_bytecode = True

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("hero_preparation", ROOT / "Tools/Prepare-MegdanHeroPresentation.py")
heroes = importlib.util.module_from_spec(spec)
spec.loader.exec_module(heroes)
OUTPUT = ROOT / "Assets/Resources/Private/Megdan/Standees"


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for identity, (filename, _) in heroes.MAPPINGS.items():
        source = ROOT / "Art/ConceptPortraits" / filename
        shutil.copyfile(source, OUTPUT / (identity + ".png"))
        with Image.open(source) as image:
            alpha = image.convert("RGBA").getchannel("A")
            bounds = alpha.point(lambda value: 255 if value >= 96 else 0).getbbox()
            left, top, right, bottom = bounds
            mask = alpha.crop(bounds).resize((128, 128), Image.Resampling.NEAREST)
            def solid(x, y):
                return 0 <= x < 128 and 0 <= y < 128 and mask.getpixel((x, y)) >= 96
            edges = []
            for y in range(128):
                for x in range(128):
                    if not solid(x, y):
                        continue
                    for dx, dy, edge in (
                        (0, -1, (x, y, x+1, y)),
                        (1, 0, (x+1, y, x+1, y+1)),
                        (0, 1, (x+1, y+1, x, y+1)),
                        (-1, 0, (x, y+1, x, y)),
                    ):
                        if not solid(x+dx, y+dy):
                            edges.extend((edge[0]/128, 1-edge[1]/128,
                                          edge[2]/128, 1-edge[3]/128))
            data = dict(left=left/image.width, bottom=1-bottom/image.height,
                        width=(right-left)/image.width, height=(bottom-top)/image.height,
                        aspect=(right-left)/(bottom-top), edges=edges)
            (OUTPUT / (identity + ".json")).write_text(json.dumps(data), encoding="utf-8")
        print(identity + ": original PNG and panel outline prepared")


if __name__ == "__main__":
    main()
