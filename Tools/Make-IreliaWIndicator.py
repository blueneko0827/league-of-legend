"""Draw the W (Defiant Dance) aiming indicator textures.

Usage: python Tools/Make-IreliaWIndicator.py
Requires Pillow.

LoL's skillshot indicator textures live in the game's shared UI files, which are not in
CharacterAssets, so these are drawn here after the in-game W indicator: the circle around
Irelia (teal glow inside a bright rim), the bright arrow with its target reticle, and the
blade-shaped arrowhead where the two side lines of the hit area meet. Writes
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


def save(image, name, size):
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


def stroke(draw, points, width0, width1, color, scale):
    """Polyline with a width that tapers from width0 to width1 (pixels at supersampled scale)."""
    count = len(points)
    for i in range(count - 1):
        t = i / max(1, count - 2)
        w = width0 + (width1 - width0) * t
        (x0, y0), (x1, y1) = points[i], points[i + 1]
        dx, dy = x1 - x0, y1 - y0
        length = math.hypot(dx, dy) or 1
        nx, ny = -dy / length * w / 2, dx / length * w / 2
        draw.polygon([(x0 + nx, y0 + ny), (x1 + nx, y1 + ny), (x1 - nx, y1 - ny), (x0 - nx, y0 - ny)], fill=color)
        draw.ellipse([x1 - w / 2, y1 - w / 2, x1 + w / 2, y1 + w / 2], fill=color)


def bezier(p0, p1, p2, steps=48):
    return [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0],
             (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1])
            for t in (i / steps for i in range(steps + 1))]


def head():
    """Blade-shaped arrowhead: the side lines bend into a pointed arch, a filled teal chevron
    inside, and the arrow splitting into two prongs that reach the tip."""
    w, h = 512 * SS, 384 * SS
    image = Image.new('RGBA', (w, h))
    draw = ImageDraw.Draw(image)

    def P(x, y):  # x across [-1, 1], y forward [0, 1] (tip at the top of the image)
        return ((x * .96 + 1) / 2 * w, (1 - y * .97) * h)

    for side in (-1, 1):
        # Outer arch: continues the side line and rounds off into the point.
        arch = [(side * (1 - y ** 3) ** .5, y) for y in (k / 64 for k in range(65))]
        stroke(draw, [P(*p) for p in arch], 9 * SS, 5 * SS, CYAN + (255,), SS)
        # Front chevron: a thin crescent (teal inside, bright front edge) pointing forward.
        front = bezier((side * .78, .14), (side * .74, .66), (0, .9))
        back = bezier((0, .73), (side * .58, .5), (side * .78, .14))
        draw.polygon([P(*p) for p in front + back], fill=TEAL + (140,))
        stroke(draw, [P(*p) for p in front], 10 * SS, 5 * SS, CYAN + (255,), SS)
        stroke(draw, [P(*p) for p in back], 4 * SS, 4 * SS, CYAN + (220,), SS)
        # A second, smaller chevron behind it.
        stroke(draw, [P(*p) for p in bezier((side * .52, .1), (side * .46, .46), (0, .62))], 7 * SS, 4 * SS, CYAN + (245,), SS)
        # The arrow's two prongs.
        stroke(draw, [P(*p) for p in bezier((side * .07, 0), (side * .09, .4), (side * .02, .86))], 11 * SS, 5 * SS, CYAN + (255,), SS)
    image = image.filter(ImageFilter.GaussianBlur(SS * .6))
    save(image, 'Head', (512, 384))


def line():
    """Bright arrow body: narrow near Irelia, full width at the reticle, soft sides."""
    w, h = 64, 512
    image = Image.new('RGBA', (w, h))
    px = image.load()
    for y in range(h):
        forward = 1 - (y + .5) / h
        half = .3 + .7 * forward ** .8
        for x in range(w):
            d = abs((x + .5) / w * 2 - 1) / half
            a = 1 - smooth(.7, 1.0, d)
            if a > 0:
                px[x, y] = CYAN + (int(255 * a),)
    save(image, 'Line', (64, 512))


def edge():
    """Thin side line of the hit area."""
    w, h = 16, 64
    image = Image.new('RGBA', (w, h))
    px = image.load()
    for y in range(h):
        for x in range(w):
            d = abs((x + .5) / w * 2 - 1)
            a = (1 - smooth(.35, 1.0, d)) * .75
            px[x, y] = CYAN + (int(255 * a),)
    save(image, 'Edge', (16, 64))


def reticle():
    """Target mark at the end of the arrow: cream rim, black ring, teal center, white pip."""
    n = 256 * SS
    image = Image.new('RGBA', (n, n))
    draw = ImageDraw.Draw(image)
    c = n / 2

    def disc(r, color):
        draw.ellipse([c - r * c, c - r * c, c + r * c, c + r * c], fill=color)

    disc(1.0, (236, 226, 196, 255))
    disc(.86, (10, 14, 16, 255))
    # Four notches in the black ring, as on LoL's reticle.
    for angle in (45, 135, 225, 315):
        a = math.radians(angle)
        x, y = c + math.cos(a) * .78 * c, c + math.sin(a) * .78 * c
        draw.ellipse([x - .09 * c, y - .09 * c, x + .09 * c, y + .09 * c], fill=(236, 226, 196, 255))
    disc(.66, (60, 190, 180, 255))
    disc(.27, (10, 14, 16, 255))
    disc(.15, (250, 250, 245, 255))
    image = image.filter(ImageFilter.GaussianBlur(SS * .5))
    save(image, 'Reticle', (128, 128))


if __name__ == '__main__':
    circle(); head(); line(); edge(); reticle()
