#!/usr/bin/env python3
"""Generates every Jetpack mod texture (jetpack, Christmas set, item sprites)."""

import math
import os
import random

from PIL import Image

ASSETS = "src/main/resources/assets/jetpack/textures"
BLOCKS = os.path.join(ASSETS, "block")
ITEMS = os.path.join(ASSETS, "item")
ARMOR = os.path.join(ASSETS, "models", "armor")

CLEAR = (0, 0, 0, 0)


def img(w, h, colour=CLEAR):
    return Image.new("RGBA", (w, h), colour)


def px(im, x, y, c):
    if 0 <= x < im.width and 0 <= y < im.height:
        im.putpixel((x, y), c)


def fill(im, x, y, w, h, c):
    for iy in range(y, y + h):
        for ix in range(x, x + w):
            px(im, ix, iy, c)


def box(im, x, y, w, h, c):
    for ix in range(x, x + w):
        px(im, ix, y, c)
        px(im, ix, y + h - 1, c)
    for iy in range(y, y + h):
        px(im, x, iy, c)
        px(im, x + w - 1, iy, c)


def shade(base, amount):
    """Lighten (positive) or darken (negative) a colour tuple."""
    r, g, b = base[:3]
    a = base[3] if len(base) > 3 else 255
    if amount >= 0:
        r = int(r + (255 - r) * amount)
        g = int(g + (255 - g) * amount)
        b = int(b + (255 - b) * amount)
    else:
        k = 1.0 + amount
        r, g, b = int(r * k), int(g * k), int(b * k)
    return (max(0, min(255, r)), max(0, min(255, g)), max(0, min(255, b)), a)


def noise_fill(im, x, y, w, h, base, spread=0.10, seed=0):
    """Fill an area with gentle per-pixel colour variation."""
    rng = random.Random(seed)
    for iy in range(y, y + h):
        for ix in range(x, x + w):
            amount = rng.uniform(-spread, spread)
            px(im, ix, iy, shade(base, amount))


def gradient_fill(im, x, y, w, h, top, bottom):
    for iy in range(y, y + h):
        t = 0 if h <= 1 else (iy - y) / (h - 1)
        c = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,)
        for ix in range(x, x + w):
            px(im, ix, iy, c)


# ---------------------------------------------------------------- palette ----
PINE_DARK = (24, 68, 40, 255)
PINE = (34, 92, 52, 255)
PINE_LIGHT = (58, 124, 70, 255)
PINE_SNOW = (232, 244, 250, 255)
BARK = (74, 52, 36, 255)
BARK_DARK = (50, 34, 24, 255)

RED = (198, 42, 46, 255)
RED_DARK = (146, 24, 30, 255)
GREEN = (46, 140, 68, 255)
GREEN_DARK = (30, 100, 48, 255)
GOLD = (238, 184, 56, 255)
GOLD_DARK = (190, 138, 28, 255)
WHITE = (246, 248, 252, 255)
WHITE_DIM = (214, 222, 232, 255)
SNOW = (240, 246, 252, 255)
SNOW_DIM = (208, 220, 234, 255)
ICE = (150, 208, 238, 255)
COAL = (36, 36, 42, 255)
CARROT = (240, 132, 36, 255)
STEEL = (128, 138, 152, 255)


def ensure_dirs():
    for d in (BLOCKS, ITEMS, ARMOR, os.path.join(ASSETS, "item")):
        os.makedirs(d, exist_ok=True)


# ------------------------------------------------------------- block tex ----
def tree_textures():
    # Foliage: layered pine needles with snow dusting.
    im = img(16, 16)
    noise_fill(im, 0, 0, 16, 16, PINE, 0.16, seed=1)
    rng = random.Random(7)
    for _ in range(46):  # needle strokes
        x, y = rng.randrange(16), rng.randrange(16)
        length = rng.choice((1, 2, 2, 3))
        horizontal = rng.random() < 0.5
        for i in range(length):
            cx = x + i if horizontal else x
            cy = y if horizontal else y + i
            px(im, cx, cy, shade(PINE_LIGHT if rng.random() < 0.6 else PINE_DARK, rng.uniform(-0.1, 0.1)))
    for _ in range(22):  # snow resting on the needles
        x, y = rng.randrange(16), rng.randrange(16)
        px(im, x, y, SNOW)
        if rng.random() < 0.45:
            px(im, x + 1, y, WHITE_DIM)
    for _ in range(10):  # tiny ornaments baked into the foliage
        x, y = rng.randrange(1, 15), rng.randrange(1, 15)
        px(im, x, y, rng.choice((RED, GOLD, ICE)))
    im.save(os.path.join(BLOCKS, "christmas_tree_leaves.png"))

    # Trunk: striped bark.
    im = img(16, 16)
    noise_fill(im, 0, 0, 16, 16, BARK, 0.14, seed=3)
    for x in range(16):
        if x % 4 == 0:
            for y in range(16):
                px(im, x, y, BARK_DARK)
    im.save(os.path.join(BLOCKS, "christmas_tree_trunk.png"))

    # Star topper + bulb strip used by the model.
    im = img(16, 16)
    gradient_fill(im, 0, 0, 16, 16, (255, 246, 170, 255), (255, 196, 60, 255))
    for i in range(16):
        px(im, 7, i, (255, 255, 235, 255))
        px(im, 8, i, (255, 250, 205, 255))
    box(im, 3, 3, 10, 10, (255, 255, 250, 255))
    im.save(os.path.join(BLOCKS, "christmas_star_block.png"))

    im = img(16, 16, (30, 26, 24, 255))
    rng = random.Random(11)
    for _ in range(18):
        x, y = rng.randrange(16), rng.randrange(16)
        c = rng.choice(((255, 226, 120, 255), (255, 120, 120, 255), (150, 220, 255, 255), (170, 255, 170, 255)))
        px(im, x, y, c)
        if rng.random() < 0.5:
            px(im, x + 1, y, c)
    im.save(os.path.join(BLOCKS, "christmas_lights.png"))

    for name, c in (("bauble_red", RED), ("bauble_gold", GOLD)):
        im = img(16, 16, c)
        fill(im, 3, 3, 10, 10, shade(c, 0.35))
        fill(im, 5, 5, 6, 6, shade(c, -0.25))
        px(im, 7, 6, (255, 255, 255, 255))
        im.save(os.path.join(BLOCKS, f"{name}.png"))


def gift_textures():
    for name, (paper, ribbon) in {
        "gift_red": (RED, GOLD),
        "gift_green": (GREEN, WHITE),
        "gift_gold": (GOLD, RED),
    }.items():
        im = img(16, 16)
        noise_fill(im, 0, 0, 16, 16, paper, 0.08, seed=hash(name) % 1000)
        # Delicate polka dots so the paper does not look flat.
        rng = random.Random(hash(name) % 500)
        for _ in range(12):
            x, y = rng.randrange(16), rng.randrange(16)
            px(im, x, y, shade(paper, 0.22))
        box(im, 0, 0, 16, 16, shade(paper, -0.2))
        im.save(os.path.join(BLOCKS, f"{name}.png"))

        im = img(16, 16)
        noise_fill(im, 0, 0, 16, 16, ribbon, 0.10, seed=hash(name) % 777)
        # Woven fabric lines.
        for i in range(0, 16, 3):
            for x in range(16):
                px(im, x, i, shade(ribbon, 0.18))
        im.save(os.path.join(BLOCKS, f"{name}_ribbon.png"))


def snowman_textures():
    im = img(16, 16)
    noise_fill(im, 0, 0, 16, 16, SNOW, 0.05, seed=21)
    rng = random.Random(22)
    for _ in range(26):
        px(im, rng.randrange(16), rng.randrange(16), shade(SNOW, -0.10))
    im.save(os.path.join(BLOCKS, "snowman_snow.png"))

    im = img(16, 16, COAL)
    noise_fill(im, 0, 0, 16, 16, COAL, 0.25, seed=23)
    im.save(os.path.join(BLOCKS, "snowman_coal.png"))

    im = img(16, 16, CARROT)
    for y in range(16):
        for x in range(16):
            if (x + y) % 3 == 0:
                px(im, x, y, shade(CARROT, -0.18))
    im.save(os.path.join(BLOCKS, "snowman_carrot.png"))

    im = img(16, 16)
    for y in range(16):
        for x in range(16):
            px(im, x, y, RED if (x // 2 + y // 2) % 2 == 0 else GREEN)
    im.save(os.path.join(BLOCKS, "snowman_scarf.png"))

    im = img(16, 16, (28, 26, 30, 255))
    for y in range(16):
        for x in range(16):
            if (x + y) % 7 == 0:
                px(im, x, y, (58, 56, 62, 255))
    im.save(os.path.join(BLOCKS, "snowman_hat.png"))


def wreath_textures():
    im = img(16, 16)
    noise_fill(im, 0, 0, 16, 16, PINE, 0.17, seed=31)
    rng = random.Random(32)
    for _ in range(60):
        x, y = rng.randrange(16), rng.randrange(16)
        px(im, x, y, shade(rng.choice((PINE_LIGHT, PINE_DARK)), rng.uniform(-0.1, 0.1)))
    for _ in range(9):
        x, y = rng.randrange(16), rng.randrange(16)
        px(im, x, y, RED)
    im.save(os.path.join(BLOCKS, "wreath_leaves.png"))

    im = img(16, 16, RED)
    fill(im, 2, 2, 12, 12, shade(RED, 0.3))
    fill(im, 5, 5, 6, 6, RED_DARK)
    im.save(os.path.join(BLOCKS, "wreath_bow.png"))

    im = img(16, 16, (206, 32, 44, 255))
    for x, y in ((4, 4), (7, 5), (10, 8), (6, 10), (11, 3)):
        fill(im, x, y, 2, 2, (252, 90, 96, 255))
        px(im, x, y, (255, 190, 190, 255))
    im.save(os.path.join(BLOCKS, "wreath_berries.png"))


def snow_lantern_textures():
    im = img(16, 16)
    noise_fill(im, 0, 0, 16, 16, SNOW, 0.05, seed=41)
    box(im, 0, 0, 16, 16, SNOW_DIM)
    im.save(os.path.join(BLOCKS, "snow_lantern_snow.png"))

    im = img(16, 16)
    gradient_fill(im, 0, 0, 16, 16, (255, 240, 170, 255), (255, 196, 84, 255))
    box(im, 0, 0, 16, 16, ICE)
    # Frosted panes.
    for i in range(1, 16, 4):
        for y in range(1, 15, 4):
            fill(im, i, y, 2, 2, (255, 255, 232, 255))
    im.save(os.path.join(BLOCKS, "snow_lantern_glass.png"))


def candy_cane_textures():
    im = img(16, 16, WHITE)
    # Diagonal red stripes.
    for y in range(16):
        for x in range(16):
            if (x + y) % 8 < 3:
                px(im, x, y, RED)
            elif (x + y) % 8 == 3:
                px(im, x, y, RED_DARK)
    for y in range(16):
        px(im, 0, y, WHITE_DIM)
        px(im, 15, y, WHITE_DIM)
    im.save(os.path.join(BLOCKS, "candy_cane.png"))

    im = img(16, 16, WHITE)
    fill(im, 4, 4, 8, 8, WHITE_DIM)
    fill(im, 6, 6, 4, 4, WHITE)
    px(im, 7, 7, RED)
    im.save(os.path.join(BLOCKS, "candy_cane_top.png"))


# -------------------------------------------------------------- item tex ----
def star_sprite():
    im = img(16, 16)
    # Five pointed star, drawn as a filled polygon mask.
    pts = []
    cx = cy = 7.5
    for i in range(10):
        angle = -math.pi / 2 + i * math.pi / 5
        radius = 7.0 if i % 2 == 0 else 3.0
        pts.append((cx + math.cos(angle) * radius, cy + math.sin(angle) * radius))

    for y in range(16):
        for x in range(16):
            if _inside_polygon(x + 0.5, y + 0.5, pts):
                dist = math.hypot(x - cx, y - cy) / 7.0
                px(im, x, y, shade((255, 214, 74, 255), 0.45 - dist * 0.55))
    # Sparkle highlight.
    px(im, 7, 6, (255, 255, 250, 255))
    px(im, 8, 7, (255, 255, 235, 255))
    im.save(os.path.join(ITEMS, "christmas_star.png"))


def _inside_polygon(x, y, pts):
    inside = False
    j = len(pts) - 1
    for i in range(len(pts)):
        xi, yi = pts[i]
        xj, yj = pts[j]
        if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi) + xi:
            inside = not inside
        j = i
    return inside


def snow_globe_sprite():
    im = img(16, 16)
    # Glass dome.
    for y in range(2, 12):
        for x in range(2, 14):
            dx, dy = x - 7.5, y - 6.5
            if (dx / 5.6) ** 2 + (dy / 5.2) ** 2 <= 1.0:
                px(im, x, y, (188, 228, 250, 255))
    # Snow inside.
    for y in range(7, 12):
        for x in range(3, 13):
            dx, dy = x - 7.5, y - 6.5
            if (dx / 5.2) ** 2 + (dy / 4.6) ** 2 <= 1.0:
                px(im, x, y, SNOW)
    rng = random.Random(51)
    for _ in range(14):
        px(im, rng.randrange(4, 12), rng.randrange(4, 11), WHITE)
    # Miniature tree.
    fill(im, 7, 5, 2, 4, PINE_DARK)
    fill(im, 6, 4, 4, 2, PINE)
    px(im, 7, 3, GOLD)
    # Glass highlight.
    for i in range(4):
        px(im, 4 + i, 5 - i // 2, (255, 255, 255, 210))
    # Wooden base.
    fill(im, 3, 12, 10, 2, (120, 78, 46, 255))
    fill(im, 4, 12, 8, 1, (150, 100, 60, 255))
    fill(im, 2, 14, 12, 1, (96, 62, 36, 255))
    im.save(os.path.join(ITEMS, "snow_globe.png"))


def firework_sprite():
    im = img(16, 16)
    # Rocket body.
    fill(im, 7, 6, 2, 7, RED)
    fill(im, 7, 6, 1, 7, shade(RED, 0.28))
    fill(im, 6, 12, 4, 2, GOLD_DARK)
    fill(im, 6, 13, 4, 1, GOLD)
    # Stick.
    fill(im, 7, 13, 2, 3, (186, 150, 96, 255))
    # Burst above.
    for dx, dy, c in ((0, 0, GOLD), (-2, 1, RED), (2, 1, GREEN), (-1, -2, ICE),
                      (1, -2, WHITE), (-3, -1, GOLD), (3, -1, GOLD), (0, 2, WHITE)):
        px(im, 8 + dx, 3 + dy, c)
    px(im, 8, 3, (255, 255, 255, 255))
    im.save(os.path.join(ITEMS, "festive_firework.png"))


def cocoa_sprite():
    im = img(16, 16)
    # Mug.
    fill(im, 4, 6, 8, 8, RED)
    fill(im, 4, 6, 8, 1, shade(RED, 0.3))
    box(im, 4, 6, 8, 8, RED_DARK)
    # Handle.
    for y in range(8, 12):
        px(im, 12, y, RED_DARK)
        px(im, 13, y, RED)
    # Cocoa surface + cream.
    fill(im, 5, 7, 6, 2, (96, 58, 34, 255))
    fill(im, 5, 7, 6, 1, (240, 240, 245, 255))
    px(im, 6, 7, (255, 255, 255, 255))
    # Steam.
    px(im, 6, 4, (232, 232, 240, 200))
    px(im, 7, 3, (232, 232, 240, 170))
    px(im, 9, 4, (232, 232, 240, 200))
    px(im, 10, 2, (232, 232, 240, 170))
    # Saucer.
    fill(im, 3, 14, 10, 1, WHITE_DIM)
    im.save(os.path.join(ITEMS, "hot_cocoa.png"))


def santa_hat_sprite():
    im = img(16, 16)
    # White brim.
    fill(im, 2, 9, 12, 3, WHITE)
    box(im, 2, 9, 12, 3, WHITE_DIM)
    # Red cone leaning right.
    for y in range(2, 9):
        width = 12 - (y - 2)
        x0 = 2 + (y - 2) // 2
        fill(im, x0, y, max(2, width), 1, RED)
    fill(im, 2, 2, 4, 7, shade(RED, 0.12))
    # Pompom.
    fill(im, 11, 2, 3, 3, WHITE)
    px(im, 12, 2, (255, 255, 255, 255))
    im.save(os.path.join(ITEMS, "santa_hat.png"))


# ------------------------------------------------------------ armor layer ----
def santa_hat_layer():
    """64x64 layer for SantaHatModel (brim, three stepped cubes, pompom)."""
    im = img(64, 64)

    def cube(ux, uy, w, h, d, base, light, dark):
        faces = {
            "up": (ux + d, uy, w, d),
            "down": (ux + d + w, uy, w, d),
            "east": (ux, uy + d, d, h),
            "north": (ux + d, uy + d, w, h),
            "west": (ux + d + w, uy + d, d, h),
            "south": (ux + d + w + d, uy + d, w, h),
        }
        for label, (x, y, fw, fh) in faces.items():
            if label == "up":
                fill(im, x, y, fw, fh, light)
            elif label == "down":
                fill(im, x, y, fw, fh, dark)
            else:
                fill(im, x, y, fw, fh, base)
            box(im, x, y, fw, fh, shade(base, -0.3))

    # brim 9x2x9 at (0,0): footprint 2*9+2*9=36 wide, 11 tall
    cube(0, 0, 9, 2, 9, WHITE, (255, 255, 255, 255), WHITE_DIM)
    # cone 8x3x8 at (0,12)
    cube(0, 12, 8, 3, 8, RED, shade(RED, 0.2), RED_DARK)
    # cone 6x2x6 at (0,24)
    cube(0, 24, 6, 2, 6, RED, shade(RED, 0.2), RED_DARK)
    # cone 4x2x4 at (0,33)
    cube(0, 33, 4, 2, 4, RED, shade(RED, 0.2), RED_DARK)
    # tip 2x1x2 at (0,40)
    cube(0, 40, 2, 1, 2, RED_DARK, RED, RED_DARK)
    # pompom 3x3x3 at (10,40)
    cube(10, 40, 3, 3, 3, WHITE, (255, 255, 255, 255), WHITE_DIM)

    im.save(os.path.join(ARMOR, "santa_hat_layer_1.png"))


import PIL  # noqa: E402  (kept last so the constant above stays readable)

if __name__ == "__main__":
    ensure_dirs()
    tree_textures()
    gift_textures()
    snowman_textures()
    wreath_textures()
    snow_lantern_textures()
    candy_cane_textures()
    star_sprite()
    snow_globe_sprite()
    firework_sprite()
    cocoa_sprite()
    santa_hat_sprite()
    santa_hat_layer()
    print("all textures written")
