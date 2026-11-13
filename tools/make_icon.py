"""Vygeneruje vlastní ikonu aplikace (src/BetterWindows/Assets/app.ico).

Spuštění:  python tools/make_icon.py   (potřebuje Pillow)
Motiv: plochá grafitová dlaždice, nahoře lišta, dole tři tečky (tečková matice = podpis shellu).
Žádné barevné přechody – stejný glyf jako Logo.svelte.
"""
from pathlib import Path
from PIL import Image, ImageDraw

SIZES = [16, 20, 24, 32, 40, 48, 64, 128, 256]
OUT = Path(__file__).resolve().parent.parent / "src" / "BetterWindows" / "Assets" / "app.ico"

TILE = (35, 35, 37, 255)       # #232325 – zvýšená plocha grafitové škály
EDGE = (237, 237, 237, 40)     # jemný okraj, aby byla dlaždice vidět i na tmavém pozadí
GLYPH = (237, 237, 237, 255)   # #ededed – primární text


def render(size: int) -> Image.Image:
    s = 1024  # kreslíme velké a zmenšíme = hladké hrany
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((32, 32, s - 32, s - 32), radius=224, fill=TILE, outline=EDGE, width=12)
    # lišta nahoře
    d.rounded_rectangle((216, 248, s - 216, 328), radius=40, fill=GLYPH)
    # dock: tři tečky
    for cx in (360, 512, 664):
        d.ellipse((cx - 60, 660, cx + 60, 780), fill=GLYPH)
    return img.resize((size, size), Image.LANCZOS)


def main() -> None:
    images = [render(sz) for sz in SIZES]
    OUT.parent.mkdir(parents=True, exist_ok=True)
    images[-1].save(OUT, format="ICO", sizes=[(sz, sz) for sz in SIZES], append_images=images[:-1])
    print(f"Uloženo: {OUT}")


if __name__ == "__main__":
    main()
