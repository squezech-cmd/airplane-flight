#!/usr/bin/env python3
"""Quick and dirty isometric previewer for the mod's block models.

It is not the Minecraft renderer: it is an orthographic painter's-algorithm
sketch that shows silhouette, layering and colour balance.  Good enough to
catch a broken model before a four minute CI build.
"""
import json
import math
import os
import sys

from PIL import Image, ImageDraw

ASSETS = os.path.join(os.path.dirname(__file__), "..", "src", "main", "resources", "assets", "jetpack")
MODELS = os.path.join(ASSETS, "models", "block")
TEXTURES = os.path.join(ASSETS, "textures", "block")

SHADE = {"up": 1.0, "down": 0.45, "north": 0.82, "south": 0.92, "east": 0.68, "west": 0.76}
OPPOSITE = {"up": "up", "down": "down", "north": "south", "south": "north",
            "east": "west", "west": "east"}

_tex_cache = {}


def tex_colour(ref):
    if ref.startswith("#"):
        return None
    key = ref.split(":")[-1]
    if key.startswith("block/"):
        key = key[len("block/"):]
    if key in _tex_cache:
        return _tex_cache[key]
    path = os.path.join(TEXTURES, key + ".png")
    if not os.path.exists(path):
        _tex_cache[key] = (255, 0, 255)
        return _tex_cache[key]
    im = Image.open(path).convert("RGBA")
    px = list(im.getdata())
    total = [0.0, 0.0, 0.0]
    weight = 0.0
    for r, g, b, a in px:
        if a < 40:
            continue
        w = a / 255.0
        total[0] += r * w
        total[1] += g * w
        total[2] += b * w
        weight += w
    if weight == 0:
        colour = (255, 0, 255)
    else:
        colour = tuple(int(c / weight) for c in total)
    _tex_cache[key] = colour
    return colour


def rot_point(p, rotation):
    ox, oy, oz = rotation["origin"]
    angle = math.radians(rotation["angle"])
    axis = rotation["axis"]
    x, y, z = p[0] - ox, p[1] - oy, p[2] - oz
    # vanilla bakes element rotation as a quaternion about +axis, which is the
    # right handed (counter-clockwise) rotation -- match it exactly
    if axis == "y":
        x, z = x * math.cos(angle) + z * math.sin(angle), -x * math.sin(angle) + z * math.cos(angle)
    elif axis == "x":
        y, z = y * math.cos(angle) - z * math.sin(angle), y * math.sin(angle) + z * math.cos(angle)
    else:
        x, y = x * math.cos(angle) - y * math.sin(angle), x * math.sin(angle) + y * math.cos(angle)
    return (x + ox, y + oy, z + oz)


CORNERS = {
    "north": [(0, 0, 0), (1, 0, 0), (1, 0, 1), (0, 0, 1)],
    "south": [(0, 1, 0), (0, 1, 1), (1, 1, 1), (1, 1, 0)],
    "west": [(1, 0, 0), (1, 0, 1), (1, 1, 1), (1, 1, 0)],
    "east": [(0, 0, 1), (0, 0, 0), (0, 1, 0), (0, 1, 1)],
    "up": [(0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0)],
    "down": [(0, 0, 1), (1, 0, 1), (1, 1, 1), (0, 1, 1)],
}
# corner index in the box that each face corner refers to
SIGNS = {
    "north": [(0, 0, 0), (1, 0, 0), (1, 0, 1), (0, 0, 1)],
    "south": [(0, 1, 0), (0, 1, 1), (1, 1, 1), (1, 1, 0)],
    "west": [(1, 0, 0), (1, 0, 1), (1, 1, 1), (1, 1, 0)],
    "east": [(0, 0, 1), (0, 0, 0), (0, 1, 0), (0, 1, 1)],
    "up": [(0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0)],
    "down": [(0, 0, 1), (1, 0, 1), (1, 1, 1), (0, 1, 1)],
}

TILE = 26  # pixels per block


def project(p):
    x, y, z = p
    sx = (x - z) * math.cos(math.radians(30))
    sy = (x + z) * math.sin(math.radians(30)) - y
    return (ORIGIN_X + sx * TILE, ORIGIN_Y + sy * TILE)


def depth(p):
    return p[0] + p[1] * 1.35 + p[2]


def render(names, out_path, height_units):
    # first pass: project everything so the canvas can be fitted to the model
    global ORIGIN_X, ORIGIN_Y
    ORIGIN_X, ORIGIN_Y = 0.0, 0.0

    polygons = []
    for name in names:
        with open(os.path.join(MODELS, name + ".json")) as handle:
            model = json.load(handle)
        textures = model.get("textures", {})
        for element in model.get("elements", []):
            if "rotation" in element:
                # only rotate elements the game accepts, and use the same maths
                rotation = element["rotation"]
            else:
                rotation = None
            fr, to = element["from"], element["to"]
            for face, signs in SIGNS.items():
                if face not in element.get("faces", {}):
                    continue
                raw = textures.get(element["faces"][face]["texture"].lstrip("#"), "#missing")
                colour = tex_colour(raw)
                if colour is None:
                    colour = (200, 200, 200)
                pts = []
                for sx, sy, sz in signs:
                    p = (fr[0] + (to[0] - fr[0]) * sx,
                         fr[1] + (to[1] - fr[1]) * sy,
                         fr[2] + (to[2] - fr[2]) * sz)
                    if rotation:
                        p = rot_point(p, rotation)
                    # the camera sits at +X/+Z, so spin the model round to show
                    # the face that a player sees when the block is placed
                    p = (16.0 - p[0], p[1], 16.0 - p[2])
                    pts.append(p)
                screen = [project(p) for p in pts]
                d = sum(depth(p) for p in pts) / 4.0
                shade = SHADE[OPPOSITE[face]]
                tinted = tuple(min(255, int(c * shade)) for c in colour)
                polygons.append((d, screen, tinted))

    xs = [p[0] for _, poly, _ in polygons for p in poly]
    ys = [p[1] for _, poly, _ in polygons for p in poly]
    margin = 24
    width = int(max(xs) - min(xs)) + 2 * margin
    height = int(max(ys) - min(ys)) + 2 * margin
    canvas = Image.new("RGBA", (width, height), (26, 31, 41, 255))
    draw = ImageDraw.Draw(canvas)
    shift_x = margin - min(xs)
    shift_y = margin - min(ys)
    polygons = [(d, [(x + shift_x, y + shift_y) for x, y in poly], c) for d, poly, c in polygons]

    polygons.sort(key=lambda item: item[0])  # far first, near last
    for _, screen, colour in polygons:
        draw.polygon(screen, fill=colour + (255,))
        draw.line(screen + [screen[0]], fill=(0, 0, 0, 45), width=1)

    canvas.save(out_path)
    print("wrote", out_path, len(polygons), "faces")


if __name__ == "__main__":
    docs = os.path.join(os.path.dirname(__file__), "..", "docs")
    os.makedirs(docs, exist_ok=True)
    names = sys.argv[1:]
    if not names:
        render(["christmas_tree"], os.path.join(docs, "tree_small.png"), 16)
        render(["big_christmas_tree_preview"], os.path.join(docs, "tree_big.png"), 32)
        render(["giant_christmas_tree_preview"], os.path.join(docs, "tree_giant.png"), 48)
    else:
        for name in names:
            render([name], os.path.join(docs, name + ".png"), 16)
