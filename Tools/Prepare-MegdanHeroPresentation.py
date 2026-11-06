"""Prepare W-161's private hero portraits for Unity presentation surfaces.

The concept portraits remain the source. This script performs only deterministic crop, resize,
and masking work; it does not synthesize, repaint, or alter their content.
"""

from pathlib import Path
import argparse

from PIL import Image, ImageDraw, ImageOps


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Art" / "ConceptPortraits"
ORIGINAL_SOURCE = ROOT / "Art" / "Inputs"
CARD_OUTPUT = ROOT / "Assets" / "Resources" / "Private" / "Megdan" / "HeroPortraits"
TOKEN_OUTPUT = ROOT / "Assets" / "Resources" / "Private" / "Heroes"
SELECTION_OUTPUT = ROOT / "Assets" / "Resources" / "Private" / "HeroSelection"

MAPPINGS = {
    "brogan": ("ram-warrior-portrait-v1.png", "codex-clipboard-65b29d30-6149-452d-b91c-17c1af29df33.jpg"),
    "misa": ("cat-duelist-portrait-v1.png", "codex-clipboard-20569685-3938-48b7-bfa2-0acc719a0233.jpg"),
    "rowenna": ("knight-portrait-v1.png", "codex-clipboard-4bd36c08-4288-4ee7-a515-b8d5eca211da.jpg"),
    "tigerclaw": ("goblin-portrait-v1.png", "codex-clipboard-9d90c5f0-fcb0-45fe-9956-c462b4710d69.jpg"),
    "gydion": ("grandmother-cat-portrait-v1.png", "codex-clipboard-452ad7ee-c4b9-4fe2-a3a1-abf1918d984c.jpg"),
    "tali": ("frog-wizard-portrait-v1.png", "codex-clipboard-d52c7fbd-f0a1-4f9f-b4ba-3db329d053f5.jpg"),
}


def save_png(image: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, format="PNG", optimize=True)


def prepare_card_art(content_id: str, portrait: Image.Image) -> None:
    # W-168: reproduce the approved preview's transparent, uncropped character panel.
    character = portrait.crop(portrait.getbbox())
    character = ImageOps.contain(character, (988, 810), Image.Resampling.LANCZOS)
    card_art = Image.new("RGBA", (1024, 840))
    card_art.alpha_composite(character, ((1024 - character.width) // 2, 832 - character.height))
    save_png(card_art, CARD_OUTPUT / f"{content_id}.png")
    metadata = CARD_OUTPUT / f"{content_id}.png.meta"
    if metadata.exists():
        text = metadata.read_text(encoding="utf-8")
        text = text.replace("  nPOTScale: 1", "  nPOTScale: 0")
        text = text.replace("  alphaIsTransparency: 0", "  alphaIsTransparency: 1")
        metadata.write_text(text, encoding="utf-8")


def prepare(content_id: str, portrait_filename: str, original_filename: str) -> None:
    with Image.open(SOURCE / portrait_filename) as opened:
        selection_source = ImageOps.exif_transpose(opened).convert("RGBA")
    supplied = ROOT / "Art/Supplied20261106/Portraits" / (content_id + ".jpg")
    with Image.open(supplied if supplied.exists() else SOURCE / portrait_filename) as opened:
        portrait = ImageOps.exif_transpose(opened).convert("RGBA")

    prepare_card_source(content_id, portrait_filename)
    selection = ImageOps.fit(
        selection_source,
        (372, 260),
        method=Image.Resampling.LANCZOS,
        centering=(0.5, 0.40),
    )
    save_png(selection, SELECTION_OUTPUT / f"hero-{content_id}.png")

    token_art = ImageOps.fit(
        portrait,
        (480, 480),
        method=Image.Resampling.LANCZOS,
        centering=(0.5, 0.40),
    )
    circle_mask = Image.new("L", (512, 512), 0)
    ImageDraw.Draw(circle_mask).ellipse((16, 16, 496, 496), fill=255)
    token = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    ivory = Image.new("RGBA", (512, 512), (239, 227, 192, 255))
    token.alpha_composite(ivory)
    token.alpha_composite(token_art, (16, 16))
    token.putalpha(circle_mask)
    save_png(token, TOKEN_OUTPUT / f"{content_id}.png")


def prepare_card_source(content_id: str, portrait_filename: str) -> None:
    illustration = ROOT / "Art/Supplied20261106/Standees" / (content_id + ".png")
    with Image.open(illustration if illustration.exists() else SOURCE / portrait_filename) as opened:
        prepare_card_art(content_id, ImageOps.exif_transpose(opened).convert("RGBA"))


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cards-only", action="store_true",
                        help="Refresh only the six card panels; preserve token and selector assets.")
    args = parser.parse_args()
    for content_id, (portrait_filename, original_filename) in MAPPINGS.items():
        if args.cards_only:
            prepare_card_source(content_id, portrait_filename)
        else:
            prepare(content_id, portrait_filename, original_filename)
    print(f"Prepared {len(MAPPINGS)} hero mappings.")


if __name__ == "__main__":
    main()
