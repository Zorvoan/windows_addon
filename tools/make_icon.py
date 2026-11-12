"""Vygeneruje vlastní ikonu aplikace (src/BetterWindows/Assets/app.ico).

Spuštění:  python tools/make_icon.py   (potřebuje Pillow)
Motiv: zaoblený čtverec s barevným přechodem, nahoře lišta, dole "dock".
"""
from pathlib import Path
from PIL import Image, ImageDraw

SIZES = [16, 20, 24, 32, 40, 48, 64, 128, 256]
OUT = Path(__file__).resolve().parent.parent / "src" / "BetterWindows" / "Assets" / "app.ico"


def render(size: int) -> Image.Image:
    s = 1024  # kreslíme velké a zmenšíme = hladké hrany
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))

    # Přechod shora dolů: modrofialová -> tyrkysová
    grad = Image.new("RGBA", (s, s))
    top, bottom = (88, 86, 214), (40, 170, 190)
    px = grad.load()
    for y in range(s):
        t = y / (s - 1)
        c = tuple(round(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,)
        for x in range(s):
            px[x, y] = c

    mask = Image.new("L", (s, s), 0)
    ImageDraw.Draw(mask).rounded_rectangle((32, 32, s - 32, s - 32), radius=230, fill=255)
    img.paste(grad, (0, 0), mask)

    # Prvky kreslíme do samostatné vrstvy a skládáme přes alpha_composite,
    # aby se poloprůhledné barvy správně smíchaly s podkladem.
    layer = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    white = (255, 255, 255, 235)
    # horní lišta
    d.rounded_rectangle((170, 190, s - 170, 260), radius=35, fill=white)
    # dock
    d.rounded_rectangle((250, 700, s - 250, 830), radius=65, fill=(255, 255, 255, 80))
    img = Image.alpha_composite(img, layer)
    layer = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for cx in (390, 512, 634):
        d.ellipse((cx - 38, 727, cx + 38, 803), fill=white)
    img = Image.alpha_composite(img, layer)

    return img.resize((size, size), Image.LANCZOS)


def main() -> None:
    images = [render(sz) for sz in SIZES]
    OUT.parent.mkdir(parents=True, exist_ok=True)
    images[-1].save(OUT, format="ICO", sizes=[(sz, sz) for sz in SIZES], append_images=images[:-1])
    print(f"Uloženo: {OUT}")


if __name__ == "__main__":
    main()
