#!/usr/bin/env python3
"""Draws the 52-item Christmas catalogue as 16x16 icons.

Every icon is built from a small vocabulary of primitives (disc, ring, stick,
star, spiral, checker, fold) so the whole set shares one look: crisp 1px
outlines, two-tone shading and a highlight pixel in the top left.
"""

import math
import os
import random

from PIL import Image

OUT = "src/main/resources/assets/jetpack/textures/item"
BLOCK_OUT = "src/main/resources/assets/jetpack/textures/block"
CLEAR = (0, 0, 0, 0)


# ---------------------------------------------------------------- primitives

def new():
    return Image.new("RGBA", (16, 16), CLEAR)


def put(im, x, y, c):
    if 0 <= x < 16 and 0 <= y < 16 and c is not None:
        im.putpixel((x, y), c)


def rect(im, x, y, w, h, c):
    for iy in range(y, y + h):
        for ix in range(x, x + w):
            put(im, ix, iy, c)


def outline(im, x, y, w, h, c):
    for ix in range(x, x + w):
        put(im, ix, y, c)
        put(im, ix, y + h - 1, c)
    for iy in range(y, y + h):
        put(im, x, iy, c)
        put(im, x + w - 1, iy, c)


def box(im, x, y, w, h, fill, edge=None, hi=None):
    rect(im, x, y, w, h, fill)
    if hi:
        rect(im, x + 1, y + 1, max(1, w - 3), 1, hi)
    if edge:
        outline(im, x, y, w, h, edge)


def disc(im, cx, cy, r, fill, edge=None, hi=None):
    for y in range(16):
        for x in range(16):
            dx, dy = x + 0.5 - cx, y + 0.5 - cy
            if dx * dx + dy * dy <= r * r:
                put(im, x, y, fill)
    if edge:
        for y in range(16):
            for x in range(16):
                dx, dy = x + 0.5 - cx, y + 0.5 - cy
                d = math.hypot(dx, dy)
                if r - 1.0 < d <= r:
                    put(im, x, y, edge)
    if hi:
        put(im, int(cx - r * 0.35), int(cy - r * 0.35), hi)


def ring(im, cx, cy, r_out, r_in, fill, edge=None):
    for y in range(16):
        for x in range(16):
            dx, dy = x + 0.5 - cx, y + 0.5 - cy
            d = math.hypot(dx, dy)
            if r_in < d <= r_out:
                put(im, x, y, edge if (edge and d > r_out - 1) else fill)


def line(im, x0, y0, x1, y1, c):
    dx, dy = abs(x1 - x0), abs(y1 - y0)
    sx = 1 if x0 < x1 else -1
    sy = 1 if y0 < y1 else -1
    err = dx - dy
    while True:
        put(im, x0, y0, c)
        if x0 == x1 and y0 == y1:
            break
        e2 = 2 * err
        if e2 > -dy:
            err -= dy
            x0 += sx
        if e2 < dx:
            err += dx
            y0 += sy


def star(im, cx, cy, r_out, r_in, fill, edge=None, points=5, phase=-math.pi / 2):
    pts = []
    for i in range(points * 2):
        a = phase + i * math.pi / points
        r = r_out if i % 2 == 0 else r_in
        pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))

    def inside(x, y):
        hit = False
        j = len(pts) - 1
        for i in range(len(pts)):
            xi, yi = pts[i]
            xj, yj = pts[j]
            if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi) + xi:
                hit = not hit
            j = i
        return hit

    for y in range(16):
        for x in range(16):
            if inside(x + 0.5, y + 0.5):
                put(im, x, y, fill)
    if edge:
        for y in range(16):
            for x in range(16):
                if inside(x + 0.5, y + 0.5):
                    for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                        if not inside(nx + 0.5, ny + 0.5):
                            put(im, nx, ny, edge)


def shade(c, amount):
    r, g, b = c[:3]
    a = c[3] if len(c) > 3 else 255
    if amount >= 0:
        r = int(r + (255 - r) * amount)
        g = int(g + (255 - g) * amount)
        b = int(b + (255 - b) * amount)
    else:
        k = 1.0 + amount
        r, g, b = int(r * k), int(g * k), int(b * k)
    return (max(0, min(255, r)), max(0, min(255, g)), max(0, min(255, b)), a)


def spiral(im, cx, cy, r, colour, twist=2.6):
    steps = int(r * 26)
    for i in range(steps):
        t = i / steps
        a = t * math.pi * 2 * twist
        rr = t * r
        put(im, int(cx + math.cos(a) * rr), int(cy + math.sin(a) * rr), colour)


# ------------------------------------------------------------------ palette

RED = (206, 46, 50, 255)
RED_L = (240, 92, 92, 255)
RED_D = (140, 24, 30, 255)
GREEN = (58, 148, 76, 255)
GREEN_L = (104, 196, 112, 255)
GREEN_D = (30, 96, 50, 255)
GOLD = (240, 190, 62, 255)
GOLD_L = (255, 232, 140, 255)
GOLD_D = (178, 128, 24, 255)
WHITE = (248, 250, 254, 255)
WHITE_D = (206, 216, 230, 255)
SNOW = (238, 246, 254, 255)
ICE = (156, 214, 244, 255)
ICE_D = (86, 156, 208, 255)
BROWN = (140, 96, 58, 255)
BROWN_D = (94, 62, 36, 255)
COCOA = (92, 56, 34, 255)
CREAM = (250, 240, 216, 255)
STEEL = (150, 160, 176, 255)
STEEL_D = (96, 106, 122, 255)
PURPLE = (150, 96, 200, 255)
BLUE = (86, 138, 220, 255)


def save(im, name, out=OUT):
    os.makedirs(out, exist_ok=True)
    im.save(os.path.join(out, f"{name}.png"))


# --------------------------------------------------------------- decorations

def snowflake():
    im = new()
    line(im, 8, 2, 8, 13, WHITE)
    line(im, 2, 8, 13, 8, WHITE)
    line(im, 3, 3, 12, 12, ICE)
    line(im, 12, 3, 3, 12, ICE)
    for x, y in ((8, 2), (8, 13), (2, 8), (13, 8)):
        put(im, x, y, WHITE)
    for dx, dy in ((1, 2), (2, 1), (-1, 2), (-2, 1), (1, -2), (2, -1), (-1, -2), (-2, -1)):
        put(im, 8 + dx, 8 + dy, ICE)
    put(im, 8, 8, WHITE)
    save(im, "snowflake")


def bell():
    im = new()
    # body
    for y in range(4, 12):
        half = (y - 3) * 1.4
        for x in range(int(8 - half), int(8 + half)):
            put(im, x, y, GOLD if x < 8 else shade(GOLD, -0.16))
    rect(im, 3, 11, 10, 2, GOLD_D)
    rect(im, 4, 11, 8, 1, GOLD_L)
    # top loop and clapper
    put(im, 8, 2, GOLD_D)
    put(im, 7, 2, GOLD)
    put(im, 8, 3, GOLD)
    put(im, 7, 13, GOLD_D)
    put(im, 8, 13, GOLD_D)
    put(im, 6, 6, GOLD_L)
    save(im, "bell")


def tinsel():
    im = new()
    colours = (GOLD, RED, ICE, WHITE)
    for i in range(9):
        x = 1 + i * 1.6
        y = 4 + math.sin(i * 0.9) * 3.2
        c = colours[i % len(colours)]
        put(im, int(x), int(y), c)
        put(im, int(x) + 1, int(y), shade(c, 0.25))
        put(im, int(x), int(y) + 1, shade(c, -0.2))
    for i in range(5):
        put(im, 2 + i * 3, 11, WHITE_D)
    save(im, "tinsel")


def fairy_lights():
    im = new()
    pts = [(2, 6), (5, 8), (8, 6), (11, 8), (14, 6)]
    for i in range(len(pts) - 1):
        line(im, pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1], (60, 48, 40, 255))
    cols = (GOLD_L, RED_L, GREEN_L, ICE, GOLD_L)
    for (x, y), c in zip(pts, cols):
        put(im, x, y + 1, c)
        put(im, x, y + 2, shadow_of(c))
        put(im, x - 1, y + 1, shade(c, -0.1))
        put(im, x + 1, y + 1, shade(c, -0.1))
    save(im, "fairy_lights")


def shadow_of(c):
    return shade(c, -0.45)


def gingerbread_man():
    im = new()
    body_c = (176, 112, 58, 255)
    edge = (108, 62, 30, 255)
    rect(im, 7, 4, 3, 4, body_c)          # body
    rect(im, 5, 3, 7, 3, body_c)          # head
    outline(im, 5, 3, 7, 3, edge)
    outline(im, 7, 4, 3, 4, edge)
    rect(im, 3, 8, 3, 2, body_c)          # arms
    rect(im, 11, 8, 3, 2, body_c)
    outline(im, 3, 8, 3, 2, edge)
    outline(im, 11, 8, 3, 2, edge)
    rect(im, 6, 8, 5, 3, body_c)          # torso
    outline(im, 6, 8, 5, 3, edge)
    rect(im, 5, 11, 3, 4, body_c)         # legs
    rect(im, 9, 11, 3, 4, body_c)
    outline(im, 5, 11, 3, 4, edge)
    outline(im, 9, 11, 3, 4, edge)
    # icing
    put(im, 6, 4, WHITE)
    put(im, 10, 4, WHITE)
    put(im, 7, 6, RED)
    put(im, 9, 6, RED)
    put(im, 7, 8, WHITE)
    put(im, 9, 8, WHITE)
    put(im, 8, 9, WHITE)
    save(im, "gingerbread_man")


def gingerbread_house():
    im = new()
    wall = (176, 112, 58, 255)
    edge = (108, 62, 30, 255)
    rect(im, 2, 8, 12, 6, wall)
    outline(im, 2, 8, 12, 6, edge)
    # roof
    for i in range(6):
        for x in range(2 + i, 14 - i):
            put(im, x, 7 - i, WHITE if i % 2 == 0 else WHITE_D)
    # door and windows
    rect(im, 7, 10, 2, 4, COCOA)
    rect(im, 4, 10, 2, 2, GOLD_L)
    rect(im, 10, 10, 2, 2, GOLD_L)
    # candy trim
    for x in range(2, 14, 2):
        put(im, x, 8, RED)
    put(im, 12, 4, RED)
    put(im, 3, 3, GREEN_L)
    save(im, "gingerbread_house")


def nutcracker():
    im = new()
    rect(im, 5, 2, 6, 5, (240, 214, 190, 255))   # face
    outline(im, 5, 2, 6, 5, (168, 140, 118, 255))
    rect(im, 4, 1, 8, 2, (28, 26, 30, 255))      # hat
    rect(im, 6, 4, 1, 1, (40, 40, 48, 255))      # eyes
    rect(im, 9, 4, 1, 1, (40, 40, 48, 255))
    rect(im, 6, 6, 4, 1, WHITE)                  # beard
    rect(im, 4, 9, 1, 1, WHITE)
    rect(im, 11, 9, 1, 1, WHITE)
    rect(im, 4, 8, 8, 5, RED)                    # uniform
    outline(im, 4, 8, 8, 5, RED_D)
    rect(im, 7, 8, 2, 5, GOLD)                   # gold braid
    rect(im, 3, 12, 3, 2, GREEN_D)
    rect(im, 10, 12, 3, 2, GREEN_D)
    save(im, "nutcracker")


def rocking_horse():
    im = new()
    body = (176, 112, 60, 255)
    edge = (110, 66, 32, 255)
    rect(im, 4, 6, 7, 4, body)
    outline(im, 4, 6, 7, 4, edge)
    rect(im, 10, 4, 3, 3, body)                  # head
    outline(im, 10, 4, 3, 3, edge)
    rect(im, 12, 5, 1, 1, (40, 32, 28, 255))
    rect(im, 5, 10, 2, 3, body)                  # legs
    rect(im, 9, 10, 2, 3, body)
    rect(im, 6, 3, 4, 2, RED)                    # mane
    # rocker
    for x in range(2, 14):
        put(im, x, 14, RED_D)
    put(im, 2, 13, RED_D)
    put(im, 13, 13, RED_D)
    save(im, "rocking_horse")


def toy_train():
    im = new()
    rect(im, 2, 8, 6, 5, RED)                    # engine
    outline(im, 2, 8, 6, 5, RED_D)
    rect(im, 3, 4, 3, 4, (40, 44, 52, 255))      # chimney
    rect(im, 2, 3, 5, 2, GOLD_D)
    rect(im, 8, 10, 6, 3, GREEN)                 # wagon
    outline(im, 8, 10, 6, 3, GREEN_D)
    for x in (3, 6, 10, 13):                     # wheels
        put(im, x, 13, (56, 56, 62, 255))
        put(im, x, 14, (56, 56, 62, 255))
    put(im, 4, 9, GOLD_L)
    save(im, "toy_train")


def drum():
    im = new()
    rect(im, 5, 6, 7, 6, RED)
    outline(im, 5, 6, 7, 6, RED_D)
    rect(im, 5, 6, 7, 2, CREAM)                  # skin
    outline(im, 5, 6, 7, 2, WHITE_D)
    for y in range(7, 12):                       # zig-zag
        x = 6 + (y % 2) * 2
        put(im, x, y, GOLD_L)
        put(im, x + 2, y, GOLD_L)
    line(im, 3, 4, 8, 7, GOLD_D)                 # sticks
    line(im, 13, 4, 9, 7, GOLD_D)
    save(im, "drum")


def stocking():
    im = new()
    rect(im, 6, 2, 4, 7, RED)
    rect(im, 6, 9, 7, 4, RED)
    outline(im, 6, 2, 4, 7, RED_D)
    outline(im, 6, 9, 7, 4, RED_D)
    rect(im, 6, 2, 4, 2, WHITE)                  # cuff
    outline(im, 6, 2, 4, 2, WHITE_D)
    rect(im, 11, 11, 2, 2, RED_D)                # toe
    put(im, 8, 6, WHITE)
    put(im, 7, 8, GOLD_L)
    save(im, "stocking")


def mitten():
    im = new()
    rect(im, 5, 5, 5, 6, RED)
    outline(im, 5, 5, 5, 6, RED_D)
    rect(im, 9, 7, 3, 2, RED)                    # thumb
    outline(im, 9, 7, 3, 2, RED_D)
    rect(im, 5, 10, 5, 2, WHITE)                 # cuff
    outline(im, 5, 10, 5, 2, WHITE_D)
    put(im, 6, 6, RED_L)
    put(im, 8, 8, WHITE)
    save(im, "mitten")


def bauble(name, base, light, dark):
    im = new()
    disc(im, 8, 9.5, 5.4, base, dark, light)
    rect(im, 7, 2, 2, 3, GOLD_D)                 # cap
    rect(im, 7, 2, 2, 1, GOLD_L)
    put(im, 8, 1, GOLD)
    put(im, 6, 7, WHITE)                         # glint
    for y in range(10, 13):
        put(im, 11, y, shade(base, 0.22))
    save(im, name)


def star_garland():
    im = new()
    for i in range(4):
        x = 2 + i * 3.6
        y = 6 + (i % 2) * 3
        star(im, x + 1, y, 2.6, 1.1, GOLD if i % 2 == 0 else WHITE, GOLD_D, points=5)
    line(im, 1, 5, 15, 5, WHITE_D)
    for x in range(1, 16, 2):
        put(im, x, 5, ICE)
    save(im, "star_garland")


# -------------------------------------------------------------------- sweets

def peppermint_candy():
    im = new()
    disc(im, 8, 8.5, 5.6, WHITE, WHITE_D)
    for i in range(6):
        a = i * math.pi / 3
        for t in range(1, 6):
            x = int(8 + math.cos(a) * t * 0.85)
            y = int(8.5 + math.sin(a) * t * 0.85)
            put(im, x, y, RED)
    put(im, 7, 2, RED_D)
    put(im, 8, 2, RED_D)
    save(im, "peppermint_candy")


def candy_apple():
    im = new()
    disc(im, 8, 9.5, 5.2, RED, RED_D, RED_L)
    put(im, 7, 3, BROWN)
    put(im, 8, 3, BROWN)
    rect(im, 9, 2, 3, 2, GREEN_L)                # leaf
    outline(im, 9, 2, 3, 2, GREEN_D)
    put(im, 6, 7, WHITE)
    put(im, 5, 8, RED_L)
    save(im, "candy_apple")


def chocolate_bar():
    im = new()
    rect(im, 3, 4, 10, 9, (120, 72, 40, 255))
    outline(im, 3, 4, 10, 9, (78, 44, 24, 255))
    for y in range(5, 12, 3):                    # segments
        for x in range(4, 12, 3):
            rect(im, x, y, 2, 2, (146, 92, 52, 255))
            put(im, x, y, (176, 118, 70, 255))
    rect(im, 5, 6, 6, 4, RED)                   # wrapper band
    outline(im, 5, 6, 6, 4, RED_D)
    put(im, 7, 7, GOLD_L)
    put(im, 9, 8, GOLD_L)
    save(im, "chocolate_bar")


def gingerbread_cookie():
    im = new()
    disc(im, 8, 8.5, 5.6, (176, 112, 58, 255), (108, 62, 30, 255))
    put(im, 6, 7, WHITE)
    put(im, 9, 9, WHITE)
    put(im, 7, 10, WHITE)
    put(im, 10, 6, WHITE)
    put(im, 8, 2, RED)
    put(im, 5, 4, RED)
    save(im, "gingerbread_cookie")


def christmas_pudding():
    im = new()
    disc(im, 8, 9, 5.4, (88, 54, 32, 255), (52, 32, 20, 255))
    rect(im, 4, 4, 9, 3, CREAM)                 # icing drip
    outline(im, 4, 4, 9, 3, WHITE_D)
    for x in range(4, 13, 3):                   # drips
        put(im, x, 7, CREAM)
    put(im, 7, 3, RED)                          # berry
    put(im, 8, 3, RED_D)
    put(im, 9, 2, GREEN_L)
    save(im, "christmas_pudding")


def fruitcake():
    im = new()
    rect(im, 3, 5, 10, 8, (176, 132, 78, 255))
    outline(im, 3, 5, 10, 8, (126, 90, 48, 255))
    rect(im, 3, 5, 10, 2, CREAM)
    outline(im, 3, 5, 10, 2, WHITE_D)
    rng = random.Random(3)
    for _ in range(10):                          # candied fruit
        put(im, rng.randrange(4, 12), rng.randrange(8, 12), rng.choice((RED, GREEN_L, GOLD_L)))
    save(im, "fruitcake")


def roasted_chestnuts():
    im = new()
    for cx, cy, r in ((6, 8, 3.2), (10.5, 9, 2.8), (8, 12, 2.6)):
        disc(im, cx, cy, r, (128, 78, 40, 255), (80, 46, 22, 255), (170, 112, 62, 255))
    put(im, 6, 6, CREAM)
    line(im, 8, 12, 8, 13, (60, 36, 20, 255))
    save(im, "roasted_chestnuts")


def marshmallow():
    im = new()
    rect(im, 4, 5, 8, 7, CREAM)
    outline(im, 4, 5, 8, 7, WHITE_D)
    rect(im, 4, 9, 8, 2, (238, 226, 224, 255))
    rect(im, 5, 6, 3, 1, WHITE)
    save(im, "marshmallow")


def christmas_cake():
    im = new()
    rect(im, 2, 9, 12, 5, RED)                   # bottom tier
    outline(im, 2, 9, 12, 5, RED_D)
    rect(im, 4, 5, 8, 4, CREAM)                  # top tier
    outline(im, 4, 5, 8, 4, WHITE_D)
    for x in range(2, 14, 2):                    # frosting
        put(im, x, 8, WHITE)
    put(im, 7, 2, RED)                           # cherry
    put(im, 8, 2, RED_D)
    put(im, 8, 1, GREEN_L)
    put(im, 5, 11, GOLD_L)
    put(im, 10, 11, GOLD_L)
    save(im, "christmas_cake")


def sugar_plum():
    im = new()
    disc(im, 8, 9, 4.8, PURPLE, shade(PURPLE, -0.4), shade(PURPLE, 0.35))
    put(im, 7, 2, GREEN_D)
    put(im, 8, 2, GREEN)
    put(im, 9, 3, GREEN_L)
    put(im, 6, 7, WHITE)
    put(im, 5, 8, WHITE)
    save(im, "sugar_plum")


def snow_cone():
    im = new()
    for y in range(4, 14):                       # cone
        width = (y - 3) // 1
        for x in range(8 - width, 8 + width):
            put(im, x, y, (206, 168, 112, 255) if x % 3 else (176, 138, 86, 255))
    disc(im, 8, 5, 4.4, SNOW, WHITE_D)           # snow head
    for x in range(3, 14, 2):
        put(im, x, 3, ICE)
    put(im, 6, 4, WHITE)
    save(im, "snow_cone")


def sweet_cane():
    im = new()
    for y in range(4, 14):                       # shaft
        put(im, 7, y, WHITE)
        put(im, 8, y, WHITE_D)
        if y % 3 == 0:
            put(im, 7, y, RED)
    # hook
    for x in range(7, 12):
        put(im, x, 3, WHITE)
    for y in range(3, 5):
        put(im, 11, y, WHITE)
    for x in range(7, 12):
        if x % 2 == 0:
            put(im, x, 3, RED)
    put(im, 11, 3, RED)
    save(im, "sweet_cane")


def marzipan():
    im = new()
    disc(im, 8, 9, 4.6, CREAM, shade(CREAM, -0.22), WHITE)
    disc(im, 6, 6, 1.8, CREAM, shade(CREAM, -0.2))   # ears
    disc(im, 10.5, 6, 1.8, CREAM, shade(CREAM, -0.2))
    put(im, 6, 9, (120, 90, 70, 255))                # face
    put(im, 10, 9, (120, 90, 70, 255))
    put(im, 8, 10, (196, 120, 120, 255))             # snout
    put(im, 8, 12, WHITE)
    save(im, "marzipan")


def yule_log():
    im = new()
    rect(im, 2, 6, 12, 7, (120, 76, 44, 255))
    outline(im, 2, 6, 12, 7, (76, 46, 24, 255))
    disc(im, 3.5, 9.5, 3.0, (86, 54, 30, 255), (56, 34, 18, 255))
    spiral(im, 3.5, 9.5, 2.4, CREAM)
    rect(im, 5, 4, 7, 2, CREAM)                  # icing
    rect(im, 5, 4, 7, 1, WHITE)
    put(im, 7, 3, RED)
    put(im, 10, 3, GREEN_L)
    save(im, "yule_log")


# -------------------------------------------------------------------- drinks

def mug(name, liquid, foam=False, steam=True):
    im = new()
    rect(im, 3, 6, 9, 8, RED)                    # body
    outline(im, 3, 6, 9, 8, RED_D)
    rect(im, 4, 7, 3, 1, RED_L)
    for y in range(7, 10):                       # handle
        put(im, 12, y, RED_D)
        put(im, 13, y, RED)
    put(im, 12, 6, RED_D)
    put(im, 12, 10, RED_D)
    rect(im, 4, 6, 7, 2, liquid)                 # surface
    if foam:
        rect(im, 4, 5, 7, 1, CREAM)
    if steam:
        put(im, 6, 3, (235, 238, 246, 200))
        put(im, 7, 2, (235, 238, 246, 160))
        put(im, 9, 3, (235, 238, 246, 200))
    rect(im, 2, 14, 11, 1, WHITE_D)              # saucer
    save(im, name)
    return im


def eggnog():
    mug("eggnog", (240, 214, 150, 255), foam=True)


def mulled_cider():
    im = mug("mulled_cider", (188, 96, 42, 255))
    # cinnamon stick
    rect(im, 10, 2, 2, 5, (140, 88, 48, 255))
    outline(im, 10, 2, 2, 5, (96, 58, 30, 255))
    im.save(os.path.join(OUT, "mulled_cider.png"))


def peppermint_tea():
    im = mug("peppermint_tea", (206, 236, 224, 255))
    rect(im, 9, 2, 3, 2, GREEN_L)                # mint leaf
    outline(im, 9, 2, 3, 2, GREEN_D)
    im.save(os.path.join(OUT, "peppermint_tea.png"))


def christmas_punch():
    mug("christmas_punch", (212, 60, 72, 255))


def snowberry_smoothie():
    im = mug("snowberry_smoothie", (176, 128, 216, 255), foam=True)
    put(im, 6, 4, BLUE)
    put(im, 9, 4, ICE)
    im.save(os.path.join(OUT, "snowberry_smoothie.png"))


def warm_cocoa():
    im = mug("warm_cocoa", (108, 66, 40, 255), foam=True)
    for x in range(5, 11, 2):                    # marshmallows
        put(im, x, 6, WHITE)
    im.save(os.path.join(OUT, "warm_cocoa.png"))


# ------------------------------------------------------------------ trinkets

def ice_shard():
    im = new()
    pts = [(8, 1), (12, 7), (9, 14), (4, 10)]
    for y in range(16):
        for x in range(16):
            hit = False
            j = len(pts) - 1
            for i in range(len(pts)):
                xi, yi = pts[i]
                xj, yj = pts[j]
                if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi) + xi:
                    hit = not hit
                j = i
            if hit:
                put(im, x, y, ICE if x < 8 else shade(ICE, -0.22))
    line(im, 6, 4, 7, 11, WHITE)
    line(im, 7, 11, 10, 9, ICE_D)
    save(im, "ice_shard")


def frozen_heart():
    im = new()
    for y in range(16):
        for x in range(16):
            dx, dy = x + 0.5 - 8, y + 0.5 - 8.5
            # classic heart implicit curve
            a = (dx * dx + dy * dy - 16)
            if a * a * a - dx * dx * dy * dy * dy * 0.6 <= 0:
                put(im, x, y, ICE if y < 9 else shade(ICE, -0.2))
    put(im, 6, 6, WHITE)
    put(im, 5, 7, WHITE)
    line(im, 9, 5, 11, 10, ICE_D)
    line(im, 11, 10, 8, 12, ICE_D)
    save(im, "frozen_heart")


def snowy_pearl():
    im = new()
    disc(im, 8, 8.5, 5.2, (226, 236, 250, 255), (176, 192, 214, 255), WHITE)
    for x, y in ((5, 6), (10, 7), (7, 11), (11, 10), (6, 9)):
        put(im, x, y, ICE)
    put(im, 7, 6, WHITE)
    save(im, "snowy_pearl")


def lucky_snowball():
    im = new()
    disc(im, 8, 8.5, 5.4, SNOW, WHITE_D, WHITE)
    for x, y in ((5, 7), (10, 9), (7, 11), (9, 5)):
        put(im, x, y, ICE)
    star(im, 13, 3, 2.4, 0.9, GOLD_L, GOLD_D, points=4, phase=0)
    save(im, "lucky_snowball")


def star_dust():
    im = new()
    rng = random.Random(9)
    for _ in range(34):
        x, y = rng.randrange(3, 14), rng.randrange(3, 14)
        c = rng.choice((GOLD_L, GOLD, WHITE, ICE))
        put(im, x, y, c)
        if rng.random() < 0.3:
            put(im, x + 1, y, shade(c, -0.2))
    star(im, 8, 8, 3.6, 1.5, GOLD_L, GOLD_D, points=4, phase=0)
    save(im, "star_dust")


def mistletoe():
    im = new()
    for angle, stem in ((math.radians(-140), (6, 8)), (math.radians(-40), (10, 8))):
        ex = int(8 + math.cos(angle) * 6)
        ey = int(9 + math.sin(angle) * 5)
        line(im, stem[0], stem[1], ex, ey, GREEN_D)
        for t in (0.5, 0.8):
            lx = int(stem[0] + (ex - stem[0]) * t)
            ly = int(stem[1] + (ey - stem[1]) * t)
            put(im, lx, ly - 1, GREEN_L)
            put(im, lx + 1, ly - 1, GREEN)
            put(im, lx, ly + 1, GREEN)
    for x, y in ((7, 10), (9, 10), (8, 11), (8, 9)):
        put(im, x, y, WHITE)
    put(im, 8, 10, (252, 250, 244, 255))
    rect(im, 8, 3, 2, 4, RED)
    put(im, 8, 3, GOLD_L)
    save(im, "mistletoe")


def holly_berries():
    im = new()
    for x, y in ((5, 8), (9, 7), (7, 11), (11, 10)):
        disc(im, x, y, 2.2, RED, RED_D, RED_L)
    # leaves
    for lx, ly, dx in ((3, 5, 1), (11, 6, -1), (9, 12, -1)):
        for i in range(4):
            put(im, lx + dx * i, ly - i, GREEN)
            put(im, lx + dx * i + 1, ly - i, GREEN_L)
    save(im, "holly_berries")


def pine_cone():
    im = new()
    for i, (w, y) in enumerate(((3, 3), (5, 5), (6, 7), (6, 9), (5, 11), (3, 13))):
        half = w // 2
        for x in range(8 - half, 8 + half + 1):
            put(im, x, y, (120, 78, 44, 255) if (x + i) % 2 else (92, 58, 32, 255))
    put(im, 8, 2, GREEN_D)
    save(im, "pine_cone")


# --------------------------------------------------------------------- tools

def sword():
    im = new()
    for i in range(9):
        put(im, 4 + i, 11 - i, ICE if i % 2 else (206, 238, 252, 255))
        put(im, 5 + i, 11 - i, ICE_D)
    put(im, 12, 2, WHITE)
    rect(im, 3, 11, 4, 1, GOLD_D)                # guard
    rect(im, 4, 12, 1, 1, GOLD_D)
    line(im, 3, 13, 2, 14, BROWN_D)              # handle
    put(im, 3, 12, BROWN)
    save(im, "frost_sword")


def pickaxe():
    im = new()
    for i in range(4):
        line(im, 3 + i, 12 - i, 6 + i, 12 - i, ICE_D)
    for i in range(5):
        put(im, 5 + i, 2 + abs(2 - i), ICE)
        put(im, 5 + i, 3 + abs(2 - i), ICE_D)
    line(im, 3, 13, 10, 4, BROWN_D)
    line(im, 4, 13, 11, 4, BROWN)
    save(im, "frost_pickaxe")


def axe():
    im = new()
    line(im, 3, 14, 9, 4, BROWN)
    line(im, 4, 14, 10, 4, BROWN_D)
    for y in range(2, 10):
        w = 4 - abs(5 - y) // 2
        for x in range(9, 9 + w):
            put(im, x, y, ICE if x == 9 else ICE_D)
    put(im, 10, 4, WHITE)
    save(im, "frost_axe")


def shovel():
    im = new()
    line(im, 3, 14, 9, 5, BROWN)
    line(im, 4, 14, 10, 5, BROWN_D)
    for y in range(2, 8):
        w = 4 - abs(5 - y)
        for x in range(10 - w // 2, 12 + w // 2):
            put(im, x, y, ICE if x < 11 else ICE_D)
    put(im, 10, 3, WHITE)
    save(im, "frost_shovel")


# ----------------------------------------------------------------- materials

def wrapping_paper():
    im = new()
    rect(im, 1, 1, 14, 14, RED)
    outline(im, 1, 1, 14, 14, RED_D)
    for y in range(3, 14, 4):
        for x in range(3, 14, 4):
            put(im, x, y, GOLD_L)
            put(im, x + 1, y, GOLD)
    rect(im, 6, 1, 2, 14, GOLD_D)                # printed band
    save(im, "wrapping_paper")


def bow(name, colour, dark):
    im = new()
    disc(im, 5, 7, 3.0, colour, dark)
    disc(im, 11, 7, 3.0, colour, dark)
    disc(im, 8, 7.5, 1.9, shade(colour, -0.2), dark)
    line(im, 7, 9, 5, 13, colour)
    line(im, 9, 9, 11, 13, colour)
    put(im, 4, 5, shade(colour, 0.35))
    put(im, 10, 5, shade(colour, 0.35))
    save(im, name)


def pine_needles():
    im = new()
    rng = random.Random(5)
    for _ in range(9):
        x0, y0 = rng.randrange(2, 13), rng.randrange(2, 13)
        x1, y1 = x0 + rng.randrange(2, 6), y0 + rng.randrange(-3, 4)
        line(im, x0, y0, x1, y1, rng.choice((GREEN, GREEN_L, GREEN_D)))
    save(im, "pine_needles")


def snow_dust():
    im = new()
    rng = random.Random(7)
    for _ in range(40):
        x, y = rng.randrange(1, 15), rng.randrange(1, 15)
        put(im, x, y, rng.choice((SNOW, WHITE, ICE, WHITE_D)))
    for x, y in ((4, 4), (10, 6), (6, 11), (12, 11)):
        star(im, x, y, 1.6, 0.6, WHITE, None, points=4, phase=0)
    save(im, "snow_dust")


# -------------------------------------------------------------- block tex ----

def block_textures():
    # fairy lights
    im = new()
    rect(im, 0, 0, 16, 16, (44, 38, 34, 255))
    for x in range(0, 16, 3):
        put(im, x, 2, (86, 74, 62, 255))
        put(im, x + 1, 4, (86, 74, 62, 255))
    rect(im, 0, 0, 16, 4, (32, 28, 26, 255))
    for x in range(16):
        if x % 2:
            put(im, x, 1, (70, 62, 54, 255))
    save(im, "fairy_lights_wire", BLOCK_OUT)

    for name, c in (("fairy_lights_warm", (255, 226, 140, 255)),
                    ("fairy_lights_red", (255, 110, 110, 255)),
                    ("fairy_lights_green", (140, 240, 150, 255))):
        im = new()
        rect(im, 0, 0, 16, 16, c)
        rect(im, 2, 2, 12, 12, shade(c, 0.4))
        rect(im, 5, 5, 6, 6, shade(c, 0.65))
        put(im, 7, 7, (255, 255, 255, 255))
        save(im, name, BLOCK_OUT)

    # holly bush
    im = new()
    rect(im, 0, 0, 16, 16, GREEN)
    rng = random.Random(13)
    for _ in range(50):
        put(im, rng.randrange(16), rng.randrange(16), rng.choice((GREEN_L, GREEN_D)))
    for x, y in ((3, 4), (9, 6), (12, 11), (5, 12)):
        rect(im, x, y, 2, 2, RED)
        put(im, x, y, RED_L)
    save(im, "holly_leaves", BLOCK_OUT)

    im = new()
    rect(im, 0, 0, 16, 16, RED)
    rng = random.Random(14)
    for _ in range(24):
        put(im, rng.randrange(16), rng.randrange(16), RED_D)
    save(im, "holly_berries", BLOCK_OUT)

    im = new()
    rect(im, 0, 0, 16, 16, SNOW)
    rng = random.Random(15)
    for _ in range(40):
        put(im, rng.randrange(16), rng.randrange(16), WHITE_D)
    save(im, "holly_snow", BLOCK_OUT)

    # snow pile
    im = new()
    rect(im, 0, 0, 16, 16, SNOW)
    rng = random.Random(16)
    for _ in range(70):
        put(im, rng.randrange(16), rng.randrange(16), rng.choice((WHITE, WHITE_D, (250, 252, 255, 255))))
    save(im, "snow_pile", BLOCK_OUT)


def main():
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(BLOCK_OUT, exist_ok=True)

    snowflake(); bell(); tinsel(); fairy_lights()
    gingerbread_man(); gingerbread_house(); nutcracker(); rocking_horse()
    toy_train(); drum(); stocking(); mitten()
    bauble("bauble_red", RED, RED_L, RED_D)
    bauble("bauble_gold", GOLD, GOLD_L, GOLD_D)
    bauble("bauble_blue", BLUE, (150, 190, 250, 255), (48, 84, 160, 255))
    star_garland()

    peppermint_candy(); candy_apple(); chocolate_bar(); gingerbread_cookie()
    christmas_pudding(); fruitcake(); roasted_chestnuts(); marshmallow()
    christmas_cake(); sugar_plum(); snow_cone(); sweet_cane(); marzipan(); yule_log()

    eggnog(); mulled_cider(); peppermint_tea(); christmas_punch()
    snowberry_smoothie(); warm_cocoa()

    ice_shard(); frozen_heart(); snowy_pearl(); lucky_snowball()
    star_dust(); mistletoe(); holly_berries(); pine_cone()

    sword(); pickaxe(); axe(); shovel()

    wrapping_paper()
    bow("red_bow", RED, RED_D)
    bow("gold_bow", GOLD, GOLD_D)
    pine_needles(); snow_dust()

    block_textures()
    print("wrote", len(os.listdir(OUT)), "item textures and",
          len(os.listdir(BLOCK_OUT)), "block textures")


if __name__ == "__main__":
    main()
