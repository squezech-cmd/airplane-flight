#!/usr/bin/env python3
"""Builds the three Christmas trees and their textures.

The trees are built as stacks of crossed branch plates: every level has one
axis-aligned square plate and one thinner plate rotated 45 degrees, so the
silhouette is a soft octagonal cone instead of a pile of cubes.  Plate faces
sample a small window of the needle texture, which keeps the needles crisp
instead of smearing the 16x16 texture over a 16 unit face.

Everything the game accepts is respected here:

* element coordinates stay inside -16..32 (vanilla hard limit)
* element rotation only uses 0 / +-22.5 / +-45 degrees
* a multi-block tree is split into one model per block, each with local
  coordinates 0..16, because a single model may not be taller than 32 units
* item icons are separate, uniformly shrunk copies that fit 0..32
"""
import json
import math
import os
import random

from PIL import Image, ImageDraw

ASSETS = os.path.join(os.path.dirname(__file__), "..", "src", "main", "resources", "assets", "jetpack")
BLOCK_MODELS = os.path.join(ASSETS, "models", "block")
ITEM_MODELS = os.path.join(ASSETS, "models", "item")
TEXTURES = os.path.join(ASSETS, "textures", "block")

rng = random.Random(20251224)

# --------------------------------------------------------------------------
# textures
# --------------------------------------------------------------------------


def _canvas():
    return Image.new("RGBA", (16, 16), (0, 0, 0, 0))


def tex_needles(snowy: bool):
    """Dense needle plate.  The top rows get a dusting of snow."""
    im = _canvas()
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 15, 15], fill=(23, 58, 34, 255))
    for _ in range(150):
        x, y = rng.randrange(16), rng.randrange(16)
        shade = rng.choice(
            [(30, 74, 42), (38, 88, 48), (46, 102, 56), (28, 66, 38), (56, 116, 62)]
        )
        d.point((x, y), fill=shade + (255,))
    # short needle strokes so it reads as branches, not noise
    for _ in range(46):
        x, y = rng.randrange(15), rng.randrange(16)
        d.line([(x, y), (x + 1, y)], fill=rng.choice([(62, 126, 68), (44, 96, 52)]) + (255,))
    if snowy:
        for _ in range(70):
            x = rng.randrange(16)
            y = rng.randrange(0, 8)
            d.point((x, y), fill=rng.choice([(226, 238, 248), (198, 220, 238), (245, 250, 255)]) + (255,))
        for _ in range(22):
            x = rng.randrange(16)
            y = rng.randrange(8, 12)
            if rng.random() < 0.45:
                d.point((x, y), fill=(232, 242, 250, 255))
    # a few pine cones / darker pockets
    for _ in range(9):
        x, y = rng.randrange(14), rng.randrange(14)
        d.point((x, y), fill=(18, 44, 26, 255))
    return im


def tex_bark():
    im = _canvas()
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 15, 15], fill=(74, 47, 28, 255))
    for x in range(16):
        if x % 3 == 0:
            d.line([(x, 0), (x, 15)], fill=(58, 36, 21, 255))
        elif x % 3 == 1:
            d.line([(x, 0), (x, 15)], fill=(94, 61, 36, 255))
    for _ in range(60):
        x, y = rng.randrange(16), rng.randrange(16)
        d.point((x, y), fill=rng.choice([(110, 72, 42), (52, 32, 18), (86, 55, 32)]) + (255,))
    for _ in range(10):
        x, y = rng.randrange(15), rng.randrange(15)
        d.line([(x, y), (x + rng.randrange(1, 3), y + 1)], fill=(40, 24, 13, 255))
    return im


def tex_star():
    im = _canvas()
    d = ImageDraw.Draw(im)
    for y in range(16):
        for x in range(16):
            dist = math.hypot(x - 7.5, y - 7.5) / 10.5
            if dist < 0.20:
                col = (255, 253, 226)
            elif dist < 0.45:
                col = (255, 226, 130)
            elif dist < 0.75:
                col = (247, 190, 74)
            else:
                col = (214, 148, 40)
            im.putpixel((x, y), col + (255,))
    for _ in range(16):
        x, y = rng.randrange(16), rng.randrange(16)
        im.putpixel((x, y), (255, 255, 245, 255))
    return im


def tex_bulb(core, edge):
    im = _canvas()
    d = ImageDraw.Draw(im)
    for y in range(16):
        for x in range(16):
            dist = math.hypot(x - 7.5, y - 7.5)
            if dist < 2.2:
                col = (255, 255, 240)
            elif dist < 4.2:
                col = core
            elif dist < 6.4:
                col = edge
            else:
                col = (0, 0, 0, 0)
            im.putpixel((x, y), col if len(col) == 4 else col + (255,))
    return im


def tex_bauble(base, shine):
    im = _canvas()
    for y in range(16):
        for x in range(16):
            dist = math.hypot(x - 7.5, y - 7.5)
            if dist > 7.2:
                im.putpixel((x, y), (0, 0, 0, 0))
            elif dist > 6.0:
                im.putpixel((x, y), tuple(int(c * 0.62) for c in base) + (255,))
            else:
                im.putpixel((x, y), base + (255,))
    d = ImageDraw.Draw(im)
    d.ellipse([4, 4, 7, 7], fill=shine + (255,))
    d.point((5, 5), fill=(255, 255, 255, 255))
    d.rectangle([7, 0, 8, 2], fill=(214, 178, 84, 255))
    return im


TEXTURE_FILES = {
    "tree_needles": lambda: tex_needles(False),
    "tree_needles_snow": lambda: tex_needles(True),
    "tree_bark": tex_bark,
    "tree_star": tex_star,
    "tree_bulb_warm": lambda: tex_bulb((255, 206, 112), (198, 138, 40)),
    "tree_bulb_red": lambda: tex_bulb((246, 108, 96), (168, 44, 40)),
    "tree_bulb_blue": lambda: tex_bulb((120, 186, 250), (44, 96, 168)),
    "tree_bulb_green": lambda: tex_bulb((150, 232, 130), (52, 132, 62)),
    "tree_bauble_red": lambda: tex_bauble((196, 40, 46), (255, 150, 150)),
    "tree_bauble_gold": lambda: tex_bauble((214, 162, 54), (255, 236, 176)),
}


def write_textures():
    for name, maker in TEXTURE_FILES.items():
        maker().save(os.path.join(TEXTURES, name + ".png"))
    print("textures:", len(TEXTURE_FILES))


# --------------------------------------------------------------------------
# geometry helpers
# --------------------------------------------------------------------------

FACES = ("north", "south", "east", "west", "up", "down")
UV_WINDOWS = [(0, 0), (1, 1), (2, 0), (0, 2), (3, 1), (1, 3), (0, 1), (2, 2)]


def face_uv(size, side):
    """Small window of the texture so needles stay crisp on big plates."""
    span = max(2.0, min(6.0, size * 0.75))
    ox, oy = rng.choice(UV_WINDOWS)
    ox = min(ox, max(0.0, 16.0 - span))
    oy = min(oy, max(0.0, 16.0 - span))
    if side == "up" or side == "down":
        return [ox, oy, ox + span, oy + span]
    return [ox, oy, ox + span, oy + span]


def box(x0, y0, z0, x1, y1, z1, tex_side, tex_top=None, tex_bottom=None, rotation=None):
    tex_top = tex_top or tex_side
    tex_bottom = tex_bottom or tex_side
    size = max(abs(x1 - x0), abs(z1 - z0), abs(y1 - y0))
    faces = {}
    for side in FACES:
        tex = tex_side
        if side == "up":
            tex = tex_top
        elif side == "down":
            tex = tex_bottom
        faces[side] = {"texture": tex, "uv": face_uv(size, side)}
    element = {
        "from": [round(x0, 3), round(y0, 3), round(z0, 3)],
        "to": [round(x1, 3), round(y1, 3), round(z1, 3)],
        "faces": faces,
    }
    if rotation:
        element["rotation"] = rotation
    return element


def rot_y(origin_y):
    return {"origin": [8.0, round(origin_y, 3), 8.0], "axis": "y", "angle": 45, "rescale": False}


def radius_at(height, base_radius, y, canopy_bottom, canopy_top):
    """Tapered spruce silhouette with a gentle scallop so the tiers read."""
    t = max(0.0, min(1.0, (y - canopy_bottom) / max(1.0, canopy_top - canopy_bottom)))
    return (base_radius - 0.45) * ((1.0 - t) ** 1.22) + 0.45


def octagon_edge(angle, r, rd):
    """Distance from the centre to the edge of the union of the two plates."""
    sq = lambda dist, a: dist / max(abs(math.cos(a)), abs(math.sin(a)))
    return max(sq(r, angle), sq(rd, angle - math.pi / 4.0))


def rot_x(origin, angle):
    return {"origin": [8.0, round(origin[1], 3), round(origin[2], 3)],
            "axis": "x", "angle": angle, "rescale": False}


def rot_z(origin, angle):
    return {"origin": [round(origin[0], 3), round(origin[1], 3), 8.0],
            "axis": "z", "angle": angle, "rescale": False}


def build_tree(height, base_radius, canopy_bottom, trunk_half=1.25):
    """Dense cone of branch plates with drooping boughs, garland, baubles, star."""
    elements = []
    trunk = []
    canopy_top = height - 3.1
    layer = 2.0
    thickness = 2.0   # tiers touch, so nothing shows through the canopy

    def radius(y):
        return radius_at(height, base_radius, y, canopy_bottom, canopy_top)

    # ---- foliage ---------------------------------------------------------
    # Tiers are laid on in a cascade: each one is clearly narrower than the
    # one below, so the silhouette steps in like a real decorated spruce.
    tier = 0.0
    y = canopy_bottom
    while y < canopy_top - 0.8:
        r = radius(y)
        rd = max(1.1, r * 0.88)
        elements.append(box(8 - r, y, 8 - r, 8 + r, y + thickness, 8 + r,
                            "#leaves", tex_top="#snow"))
        elements.append(box(8 - rd, y + 0.45, 8 - rd, 8 + rd, y + thickness - 0.05, 8 + rd,
                            "#leaves2", tex_top="#snow", rotation=rot_y(y)))

        # drooping boughs on four sides, the classic candle-branch look
        bough = min(2.3, max(1.2, r * 0.66))
        for dx, dz, axis, angle in ((1, 0, "z", -22.5), (-1, 0, "z", 22.5),
                                    (0, 1, "x", 22.5), (0, -1, "x", -22.5)):
            inner = r - 0.2
            lo_x, hi_x = (8 - bough, 8 + bough) if dz else (8 + dx * inner, 8 + dx * (inner + 2.6))
            lo_z, hi_z = (8 - bough, 8 + bough) if dx else (8 + dz * inner, 8 + dz * (inner + 2.6))
            plate = box(min(lo_x, hi_x), y + 0.35, min(lo_z, hi_z),
                        max(lo_x, hi_x), y + thickness * 0.62, max(lo_z, hi_z),
                        "#leaves2", tex_top="#snow")
            origin_y = y + thickness * 0.35
            if axis == "z":
                plate["rotation"] = {"origin": [round(8 + dx * r, 3), round(origin_y, 3), 8.0],
                                     "axis": "z", "angle": angle, "rescale": False}
            else:
                plate["rotation"] = {"origin": [8.0, round(origin_y, 3),
                                                round(8 + dz * r, 3)],
                                     "axis": "x", "angle": angle, "rescale": False}
            elements.append(plate)
        tier += 1
        y += 2.65

    # ---- apex: a cone that fills the gap between the last tier and the star
    cone_start = max(y - 2.65, canopy_bottom + 1.2)
    base_r = max(1.35, radius(cone_start) * 0.95)
    star_lo = min(height - 4.4, cone_start + base_r * 1.5)
    steps = 6
    step_h = max(0.55, (star_lo - cone_start) / steps)
    for i in range(steps):
        r = max(0.72, base_r * (1.0 - i / float(steps)) ** 0.9)
        elements.append(box(8 - r, cone_start + i * step_h, 8 - r,
                            8 + r, cone_start + (i + 1) * step_h + 0.08, 8 + r,
                            "#leaves", tex_top="#snow"))

    # ---- trunk: tapered, cut into short segments so it never pokes out ----
    blocks = int(math.ceil(height / 16.0))
    segment = 4.0
    trunk_top = canopy_top - 2.0
    seg_y = 0.0
    while seg_y < trunk_top:
        seg_hi = min(trunk_top, seg_y + segment)
        half = min(trunk_half, max(0.62, radius(seg_y) * 0.42))
        part = min(blocks - 1, int(seg_y // 16.0))
        off = part * 16.0
        trunk.append((part, box(8 - half, seg_y - off, 8 - half,
                                8 + half, seg_hi - off, 8 + half,
                                "#bark", tex_top="#bark")))
        seg_y = seg_hi
    trunk.append((0, box(8 - 0.7, 0.0, 8 - 0.7, 8 + 0.7, 0.45, 8 + 0.7, "#bark")))

    def surface(gy, angle, offset):
        r = radius(gy)
        edge = octagon_edge(angle, r, r * 0.90)
        return 8 + math.cos(angle) * (edge + offset), 8 + math.sin(angle) * (edge + offset)

    # ---- garland ---------------------------------------------------------
    bulbs = ["bulb_warm", "bulb_red", "bulb_blue", "bulb_green"]
    steps = max(10, int((canopy_top - canopy_bottom) * 1.15))
    for i in range(steps):
        t = i / float(max(1, steps - 1))
        gy = canopy_bottom + 1.2 + t * (canopy_top - canopy_bottom - 2.2)
        angle = t * math.radians(360 * 3.1) + 0.35
        cx, cz = surface(gy, angle, 0.16)
        s = 0.44
        elements.append(box(cx - s, gy - s, cz - s, cx + s, gy + s, cz + s,
                            "#" + bulbs[i % len(bulbs)], tex_top="#bulb_warm"))

    # ---- baubles ---------------------------------------------------------
    baubles = 5 if height <= 18 else (9 if height <= 34 else 15)
    for i in range(baubles):
        t = (i + 0.5) / baubles
        by = canopy_bottom + 2.0 + t * (canopy_top - canopy_bottom - 3.4)
        angle = t * math.radians(360 * 1.9) + 2.15
        cx, cz = surface(by, angle, 0.24)
        s = 0.52
        elements.append(box(cx - s, by - s - 0.2, cz - s, cx + s, by + s - 0.2, cz + s,
                            "#bauble_red" if i % 2 == 0 else "#bauble_gold",
                            tex_top="#bauble_gold" if i % 2 else "#bauble_red"))

    # ---- star ------------------------------------------------------------
    star_hi = min(height, star_lo + 3.0)
    half, thin = 1.55, 0.22
    mid = (star_lo + star_hi) / 2.0
    # four arms in a plus, then four shorter diagonal arms: a real star
    elements.append(box(8 - half, mid - 0.42, 8 - thin, 8 + half, mid + 0.42, 8 + thin, "#star"))
    elements.append(box(8 - thin, mid - 0.42, 8 - half, 8 + thin, mid + 0.42, 8 + half, "#star"))
    elements.append(box(8 - thin, star_lo, 8 - thin, 8 + thin, star_hi, 8 + thin, "#star"))
    diag = half * 0.62
    for sx, sz in ((1, 1), (1, -1), (-1, 1), (-1, -1)):
        elements.append(box(8 + min(0, sx * diag), mid - 0.34, 8 + min(0, sz * diag),
                            8 + max(0, sx * diag), mid + 0.34, 8 + max(0, sz * diag),
                            "#star", rotation=rot_y(mid)))
    # bright core so it glows from every angle
    elements.append(box(7.2, mid - 0.8, 7.2, 8.8, mid + 0.8, 8.8, "#star"))
    return elements, trunk


def split_parts(elements, trunk, parts):
    """Group absolute-coordinate elements into per-block local models."""
    buckets = [[] for _ in range(parts)]
    for part, element in trunk:
        buckets[part].append(element)
    for element in elements:
        y0 = element["from"][1]
        part = min(parts - 1, max(0, int(y0 // 16)))
        shifted = json.loads(json.dumps(element))
        off = part * 16.0
        shifted["from"][1] = round(shifted["from"][1] - off, 3)
        shifted["to"][1] = round(shifted["to"][1] - off, 3)
        if "rotation" in shifted:
            shifted["rotation"]["origin"][1] = round(shifted["rotation"]["origin"][1] - off, 3)
        buckets[part].append(shifted)
    for bucket in buckets:
        for element in bucket:
            for key in ("from", "to"):
                for value in element[key]:
                    assert -16.0 <= value <= 32.0, (key, value, element)
            if "rotation" in element:
                assert element["rotation"]["angle"] in (-45, -22.5, 0, 22.5, 45)
                for value in element["rotation"]["origin"]:
                    assert -16.0 <= value <= 32.0
    return buckets


def model(textures, elements):
    return {"textures": textures, "elements": elements}


TEXTURES_OF = {
    "particle": "jetpack:block/tree_needles",
    "leaves": "jetpack:block/tree_needles",
    "leaves2": "jetpack:block/tree_needles",
    "snow": "jetpack:block/tree_needles_snow",
    "bark": "jetpack:block/tree_bark",
    "star": "jetpack:block/tree_star",
    "bulb_warm": "jetpack:block/tree_bulb_warm",
    "bulb_red": "jetpack:block/tree_bulb_red",
    "bulb_blue": "jetpack:block/tree_bulb_blue",
    "bulb_green": "jetpack:block/tree_bulb_green",
    "bauble_red": "jetpack:block/tree_bauble_red",
    "bauble_gold": "jetpack:block/tree_bauble_gold",
}


def write_model(name, elements, textures=None):
    payload = model(textures or TEXTURES_OF, elements)
    with open(os.path.join(BLOCK_MODELS, name + ".json"), "w") as handle:
        json.dump(payload, handle, indent=1)
    print("model:", name, len(elements), "elements")


def scaled(elements, factor):
    out = []
    for element in elements:
        copy = json.loads(json.dumps(element))
        # items are modelled around the block centre
        for key in ("from", "to"):
            copy[key][0] = round(8 + (copy[key][0] - 8) * factor, 3)
            copy[key][1] = round(copy[key][1] * factor, 3)
            copy[key][2] = round(8 + (copy[key][2] - 8) * factor, 3)
        if "rotation" in copy:
            copy["rotation"]["origin"][0] = round(8 + (copy["rotation"]["origin"][0] - 8) * factor, 3)
            copy["rotation"]["origin"][1] = round(copy["rotation"]["origin"][1] * factor, 3)
            copy["rotation"]["origin"][2] = round(8 + (copy["rotation"]["origin"][2] - 8) * factor, 3)
        out.append(copy)
    return out


def main():
    write_textures()

    # ---- small tree: one block -------------------------------------------
    elements, trunk = build_tree(16.0, 4.7, 1.6)
    (small,) = split_parts(elements, trunk, 1)
    write_model("christmas_tree", small)

    # ---- big tree: two blocks --------------------------------------------
    elements, trunk = build_tree(32.0, 6.1, 1.6)
    lower, upper = split_parts(elements, trunk, 2)
    write_model("big_christmas_tree_lower", lower)
    write_model("big_christmas_tree_upper", upper)
    combined = lower + scaled(upper, 1.0)
    for element in combined[len(lower):]:
        for key in ("from", "to"):
            element[key][1] = round(element[key][1] + 16.0, 3)
        if "rotation" in element:
            element["rotation"]["origin"][1] = round(element["rotation"]["origin"][1] + 16.0, 3)
    write_model("big_christmas_tree_preview", scaled(combined, 0.90))

    # ---- giant tree: three blocks ----------------------------------------
    elements, trunk = build_tree(48.0, 7.6, 1.6)
    parts = split_parts(elements, trunk, 3)
    for index, bucket in enumerate(parts):
        write_model("giant_christmas_tree_%d" % index, bucket)
    combined = []
    for index, bucket in enumerate(parts):
        copy = json.loads(json.dumps(bucket))
        for element in copy:
            for key in ("from", "to"):
                element[key][1] = round(element[key][1] + 16.0 * index, 3)
            if "rotation" in element:
                element["rotation"]["origin"][1] = round(element["rotation"]["origin"][1] + 16.0 * index, 3)
        combined.extend(copy)
    write_model("giant_christmas_tree_preview", scaled(combined, 0.60))
    print("giant elements:", sum(len(bucket) for bucket in parts))

    # ---- item models ------------------------------------------------------
    def item_model(block, gui_scale, third_scale, translate_y):
        return {
            "parent": "jetpack:block/" + block,
            "display": {
                "gui": {"rotation": [30, 225, 0], "translation": [0, translate_y, 0],
                        "scale": [gui_scale] * 3},
                "ground": {"rotation": [0, 0, 0], "translation": [0, 3, 0],
                           "scale": [third_scale] * 3},
                "fixed": {"rotation": [0, 180, 0], "scale": [gui_scale] * 3},
                "thirdperson_righthand": {"rotation": [75, 45, 0], "translation": [0, 2.5, 0],
                                          "scale": [third_scale] * 3},
                "firstperson_righthand": {"rotation": [0, 45, 0], "translation": [0, 2.5, 0],
                                          "scale": [third_scale * 0.8] * 3},
            },
        }

    write_item("christmas_tree", item_model("christmas_tree_preview", 0.62, 0.35, 3))
    write_item("big_christmas_tree", item_model("big_christmas_tree_preview", 0.46, 0.26, 3))
    write_item("giant_christmas_tree", item_model("giant_christmas_tree_preview", 0.42, 0.24, 2))


def write_item(name, payload):
    with open(os.path.join(ITEM_MODELS, name + ".json"), "w") as handle:
        json.dump(payload, handle, indent=2)
    print("item model:", name)


if __name__ == "__main__":
    main()
