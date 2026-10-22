"""Trace the W (Defiant Dance) arrowhead from a frame of in-game footage.

Usage: python Tools/Trace-IreliaWArrowhead.py frame.png
Requires Pillow and NumPy.

LoL's skillshot indicator textures are not in CharacterAssets, so the arrowhead is taken from a
1434x964 frame of the game with W held (the reference clip sent by the client, 5.4 s): the
ground under the indicator is unprojected to a top-down view (the circle around Irelia becomes
round: center 660,476, radii 162 x 132 px, aim 18.6 degrees), the cyan of the indicator is
keyed out of the ground, the half facing the camera is mirrored across the arrow's axis (the far
half is the more foreshortened one), smoothed, and recolored in the two tones of the original:
bright cyan strokes and teal fill. The target reticle is drawn on its own (it faces the camera);
under it the arrow body continues. Writes Assets/Resources/Models/IreliaOfficial/WAim/Head.png:
far end at the top, 1.4 R long (from 1.55 R to 2.95 R ahead of Irelia) and 0.84 R wide, R being
the radius of the circle (Tools/Make-IreliaWIndicator.py, IreliaWIndicator.cs).
"""
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / 'Assets/Resources/Models/IreliaOfficial/WAim/Head.png'
CENTER, RADII, AIM = (660, 476), (162, 132), math.radians(18.6)
AXIS = .75 - 200 / 300                      # the arrow's axis, measured across the aim (R)
START, END, HALF, PPR = 1.55, 2.95, .42, 900  # region in R units, pixels per R
RETICLE, BODY = 2.217, .031                  # reticle position and body half width (R)


def unproject(frame):
    image = np.asarray(Image.open(frame).convert('RGB')).astype(np.float32)
    h, w = image.shape[:2]
    along = START + (np.arange(int((END - START) * PPR)) + .5) / PPR
    across = AXIS + HALF - (np.arange(int(2 * HALF * PPR)) + .5) / PPR
    l, a = np.meshgrid(along, across)
    x = CENTER[0] + RADII[0] * (l * math.cos(AIM) - a * math.sin(AIM))
    y = CENTER[1] - RADII[1] * (l * math.sin(AIM) + a * math.cos(AIM))
    x0 = np.clip(np.floor(x).astype(int), 0, w - 2); y0 = np.clip(np.floor(y).astype(int), 0, h - 2)
    fx = (x - x0)[..., None]; fy = (y - y0)[..., None]
    return (image[y0, x0] * (1 - fx) * (1 - fy) + image[y0, x0 + 1] * fx * (1 - fy) +
            image[y0 + 1, x0] * (1 - fx) * fy + image[y0 + 1, x0 + 1] * fx * fy)


def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def main(frame):
    top_down = unproject(frame)
    key = np.clip((np.minimum(top_down[..., 1], top_down[..., 2]) - top_down[..., 0] - 8) / 150, 0, 1)
    middle = key.shape[0] // 2
    near = key[middle:]                                   # the half nearer the camera
    key = np.vstack([near[::-1], near])
    up = Image.fromarray((key * 255).astype(np.uint8)).resize((key.shape[1] * 2, key.shape[0] * 2), Image.BICUBIC)
    k = np.asarray(up.filter(ImageFilter.GaussianBlur(9))).astype(np.float32) / 255
    h, w = k.shape
    ys, xs = np.mgrid[0:h, 0:w]
    ppr = PPR * 2
    # The reticle (drawn apart) hides the footage here; the body runs on under it.
    hole = np.hypot(xs - (RETICLE - START) * ppr, ys - h / 2) < .112 * ppr
    body = (np.abs(ys - h / 2) < BODY * ppr) & (xs < (RETICLE - START + .15) * ppr)
    alpha = smooth(.20, .30, k)
    bright = smooth(.60, .70, k)
    alpha[hole] = 0; bright[hole] = 0
    alpha[hole & body] = 1; bright[hole & body] = 1
    teal, cyan = np.array([60, 150, 142.]), np.array([96, 255, 246.])
    color = teal * (1 - bright[..., None]) + cyan * bright[..., None]
    rgba = np.dstack([color, alpha * 255]).clip(0, 255).astype(np.uint8)
    image = Image.fromarray(rgba, 'RGBA').transpose(Image.ROTATE_90)  # far end at the top
    image = image.resize((768, 1280), Image.LANCZOS)
    image.save(TARGET)
    print('wrote', TARGET.relative_to(ROOT))


if __name__ == '__main__':
    main(sys.argv[1])
