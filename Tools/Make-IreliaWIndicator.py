"""Draw the W (Defiant Dance) aiming indicator textures.

Usage: python Tools/Make-IreliaWIndicator.py
Requires Pillow.

LoL's skillshot indicator textures live in the game's shared UI files, which are not in
CharacterAssets, so these are drawn here after the in-game W indicator, measured on a top-down
view of the reference footage: the circle around Irelia (teal glow inside a thin bright rim),
the arrow body, the dashed side lines of the hit area and the target reticle. The arrowhead is
traced from the footage itself (Tools/Trace-IreliaWArrowhead.py). Writes
Assets/Resources/Models/IreliaOfficial/WAim/*.png (row 0 = far end, as Unity maps v=1).
"""
import hashlib
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Resources/Models/IreliaOfficial/WAim'
TEMPLATE = ROOT / 'Assets/Resources/Models/IreliaOfficial/R/AimTip.png.meta'
SS = 4  # supersampling

CYAN = (92, 255, 246)
TEAL = (40, 150, 140)
RIM = (205, 255, 245)
GLOW = (150, 235, 205)


def guid(name):
    return hashlib.md5(('MidLane/IreliaWAim/' + name).encode('utf-8')).hexdigest()


def save(image, name, size, repeat=False):
    OUT.mkdir(parents=True, exist_ok=True)
    folder_meta = Path(str(OUT) + '.meta')
    if not folder_meta.exists():
        folder_meta.write_text(f'fileFormatVersion: 2\nguid: {guid("folder")}\nfolderAsset: yes\nDefaultImporter:\n'
                               '  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',
                               encoding='utf-8')
    image = image.resize(size, Image.LANCZOS)
    target = OUT / (name + '.png')
    image.save(target)
    meta = Path(str(target) + '.meta')
    if not meta.exists():
        text = TEMPLATE.read_text(encoding='utf-8-sig')
        lines = text.splitlines()
        lines = [f'guid: {guid(name)}' if line.startswith('guid:') else line for line in lines]
        if repeat:
            lines = [line.replace('wrapU: 1', 'wrapU: 0').replace('wrapV: 1', 'wrapV: 0') for line in lines]
        meta.write_text('\n'.join(lines) + '\n', encoding='utf-8')
    print('wrote', target.relative_to(ROOT))


def smooth(a, b, x):
    t = min(1, max(0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


def circle():
    """Teal glow that rises toward a thin bright rim; transparent at the center."""
    n = 512
    image = Image.new('RGBA', (n, n))
    px = image.load()
    for y in range(n):
        for x in range(n):
            r = math.hypot((x + .5) / n * 2 - 1, (y + .5) / n * 2 - 1)
            # Thin bright rim; inside it a pale glassy band that fades out toward Irelia.
            rim = smooth(.972, .984, r) * (1 - smooth(.992, 1.0, r))
            glow = (.06 + .2 * smooth(.62, .975, r) ** 2) * (1 - smooth(.982, .99, r))
            a = max(rim, glow)
            if a <= 0:
                continue
            k = rim / a if a > 0 else 0
            c = tuple(int(RIM[i] * k + GLOW[i] * (1 - k)) for i in range(3))
            px[x, y] = c + (int(255 * min(1, a)),)
    save(image, 'Circle', (512, 512))


def line():
    """Arrow body: an even bright band with soft sides (the same width all along, as in LoL)."""
    w, h = 64, 16
    image = Image.new('RGBA', (w, h))
    px = image.load()
    for y in range(h):
        for x in range(w):
            d = abs((x + .5) / w * 2 - 1)
            a = 1 - smooth(.78, 1.0, d)
            core = 1 - smooth(0, .8, d)  # a touch brighter along the middle
            c = tuple(int(CYAN[i] * (.92 + .08 * core)) for i in range(3))
            px[x, y] = c + (int(255 * a),)
    save(image, 'Line', (64, 16))


def edge():
    """Dashed side line of the hit area: one dash per tile (the material repeats it)."""
    w, h = 16, 64
    image = Image.new('RGBA', (w, h))
    px = image.load()
    for y in range(h):
        along = (y + .5) / h
        dash = .35 + .65 * smooth(.0, .1, along) * (1 - smooth(.7, .8, along))
        for x in range(w):
            d = abs((x + .5) / w * 2 - 1)
            a = (1 - smooth(.3, 1.0, d)) * dash * .6
            px[x, y] = CYAN + (int(255 * a),)
    save(image, 'Edge', (16, 64), repeat=True)


def reticle():
    """Target mark (faces the camera): black outer ring, cream ring with four small notches
    pointing in, a see-through dark center that lets the arrow body show, and a cream pip in a
    black dot."""
    n = 256 * SS
    image = Image.new('RGBA', (n, n))
    draw = ImageDraw.Draw(image)
    c = n / 2
    cream, black = (240, 230, 206, 255), (8, 10, 12, 255)

    def disc(r, color):
        draw.ellipse([c - r * c, c - r * c, c + r * c, c + r * c], fill=color)

    disc(1.0, black)
    disc(.86, cream)
    disc(.72, black)
    for angle in (0, 90, 180, 270):
        a = math.radians(angle)
        x, y = c + math.cos(a) * .7 * c, c + math.sin(a) * .7 * c
        tip_x, tip_y = c + math.cos(a) * .52 * c, c + math.sin(a) * .52 * c
        side = .1 * c
        draw.polygon([(x - math.sin(a) * side, y + math.cos(a) * side), (tip_x, tip_y),
                      (x + math.sin(a) * side, y - math.cos(a) * side)], fill=cream)
    disc(.6, (10, 30, 34, 120))  # see-through: the body shows under it
    disc(.26, black)
    disc(.13, cream)
    image = image.filter(ImageFilter.GaussianBlur(SS * .5))
    save(image, 'Reticle', (128, 128))


if __name__ == '__main__':
    circle(); line(); edge(); reticle()
