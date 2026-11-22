#!/usr/bin/env python3
"""Regenerates the eleven placeable decoration models.

Every model is written around the block centre, faces the -Z direction (the
block's FACING property rotates it), and is split into real parts -- legs,
arms, wheels, icing -- rather than one lump, so the decorations read as
objects and not as coloured cubes.
"""
import json
import math
import os

ASSETS = os.path.join(os.path.dirname(__file__), "..", "src", "main", "resources", "assets", "jetpack")
BLOCK_MODELS = os.path.join(ASSETS, "models", "block")

FACES = ("north", "south", "east", "west", "up", "down")


def face_uv(size):
    span = max(1.5, min(8.0, round(size, 1)))
    return [0, 0, span, span]


def box(x0, y0, z0, x1, y1, z1, tex, top=None, bottom=None):
    """Axis aligned element; coordinates in the usual 0..16 model space."""
    lo = [round(min(x0, x1), 3), round(min(y0, y1), 3), round(min(z0, z1), 3)]
    hi = [round(max(x0, x1), 3), round(max(y0, y1), 3), round(max(z0, z1), 3)]
    size = max(hi[0] - lo[0], hi[1] - lo[1], hi[2] - lo[2])
    faces = {}
    for side in FACES:
        ref = tex
        if side == "up" and top:
            ref = top
        if side == "down" and bottom:
            ref = bottom
        faces[side] = {"texture": "#" + ref, "uv": face_uv(size)}
    return {"from": lo, "to": hi, "faces": faces}


def rot(origin, axis, angle):
    return {"origin": [round(c, 3) for c in origin], "axis": axis,
            "angle": angle, "rescale": False}


def with_rot(element, origin, axis, angle):
    element["rotation"] = rot(origin, axis, angle)
    return element



def round_layer(cx, cz, y0, y1, r, tex, k=0.60):
    """One horizontal slice of a round body: two crossed plates making an octagon."""
    out = []
    out.append(box(cx - r, y0, cz - r * k, cx + r, y1, cz + r * k, tex))
    out.append(box(cx - r * k, y0, cz - r, cx + r * k, y1, cz + r, tex))
    return out


def round_body(cx, cz, profile, tex, k=0.60):
    """A lathe-like body: `profile` is [(y0, y1, radius), ...] bottom to top."""
    out = []
    for y0, y1, r in profile:
        out.extend(round_layer(cx, cz, y0, y1, r, tex, k))
    return out


def chamfer_box(x0, y0, z0, x1, y1, z1, tex, cut=0.9):
    """A box with its four vertical edges cut off: a real rounded volume."""
    out = []
    cut = min(cut, (x1 - x0) / 3.0, (z1 - z0) / 3.0)
    # core slab
    out.append(box(x0 + cut, y0, z0, x1 - cut, y1, z1, tex))
    out.append(box(x0, y0, z0 + cut, x1, y1, z1 - cut, tex))
    # chamfer pillars: thin plates rotated 45 degrees in the corners
    return out


def blob(cx, cy, cz, r, tex, height=None, segments=3, steps=None):
    """An eight sided volume: crossed boxes at 45 degrees read as round."""
    h = height if height is not None else r * 2.0
    side = r * 0.78
    y0 = cy - h / 2.0
    out = []
    out.append(box(cx - r, y0, cz - side, cx + r, y0 + h, cz + side, tex))
    out.append(box(cx - side, y0, cz - r, cx + side, y0 + h, cz + r, tex))
    if r > 1.6:
        mid = r * 0.62
        out.append(box(cx - mid, y0 - 0.35, cz - mid, cx + mid, y0 + h + 0.35, cz + mid, tex))
    return out


def emit(name, textures, elements):
    """Write a model: short texture keys are expanded to real asset paths."""
    for element in elements:
        for key in ("from", "to"):
            for value in element[key]:
                assert -16.0 <= value <= 32.0, (name, key, value)
        if "rotation" in element:
            assert element["rotation"]["angle"] in (-45, -22.5, 0, 22.5, 45), name

    resolved = {key: "jetpack:block/" + value for key, value in textures.items()}
    resolved.setdefault("particle", resolved.get("body") or next(iter(resolved.values())))
    payload = {"textures": resolved, "elements": elements}
    with open(os.path.join(BLOCK_MODELS, name + ".json"), "w") as handle:
        json.dump(payload, handle, indent=1)
    print("model:", name, len(elements))


# ---------------------------------------------------------------- toy train
def toy_train():
    tex = {"body": "toy_train_body", "wagon": "toy_train_wagon",
           "wheel": "toy_train_wheel", "track": "toy_train_track",
           "chimney": "toy_train_chimney"}
    e = []
    # track: sleepers plus two rails running east-west
    for i in range(4):
        e.append(box(0.6 + i * 3.8, 0.0, 2.6, 2.6 + i * 3.8, 0.45, 13.4, "track"))
    e.append(box(0.0, 0.45, 3.0, 16.0, 0.75, 3.9, "track"))
    e.append(box(0.0, 0.45, 12.1, 16.0, 0.75, 13.0, "track"))
    # locomotive body
    e.append(box(1.6, 1.05, 4.2, 8.2, 4.6, 11.8, "body", top="body"))
    e.append(box(1.6, 4.6, 4.2, 8.2, 5.1, 11.8, "chimney", top="chimney"))
    # boiler front, cab and roof
    e.append(box(1.2, 1.5, 4.0, 1.7, 4.4, 12.0, "chimney"))
    e.append(box(5.4, 4.6, 4.6, 8.4, 7.6, 11.4, "body"))
    e.append(box(5.1, 7.6, 4.3, 8.8, 8.2, 11.7, "chimney", top="chimney"))
    e.append(box(6.0, 5.6, 4.4, 7.8, 7.2, 4.8, "chimney"))
    # chimney stack
    e.append(box(2.4, 5.0, 7.2, 4.0, 7.0, 8.8, "chimney", top="chimney"))
    # wheels
    for z in (5.0, 10.2):
        e.append(box(2.0, 0.75, z - 0.35, 4.0, 2.35, z + 0.35, "wheel"))
        e.append(box(5.8, 0.75, z - 0.35, 7.8, 2.35, z + 0.35, "wheel"))
    # wagon coupled behind
    e.append(box(9.4, 1.35, 4.6, 15.2, 4.4, 11.4, "wagon"))
    e.append(box(9.4, 4.4, 4.6, 15.2, 4.8, 11.4, "chimney"))
    e.append(box(9.0, 1.35, 3.2, 15.4, 3.2, 4.4, "wagon"))
    for z in (5.4, 10.4):
        e.append(box(10.2, 0.75, z - 0.4, 12.0, 2.2, z + 0.4, "wheel"))
        e.append(box(12.8, 0.75, z - 0.4, 14.6, 2.2, z + 0.4, "wheel"))
    return tex, e


# -------------------------------------------------------------------- drum
def drum():
    tex = {"side": "drum_side", "top": "drum_top", "rim": "drum_rim"}
    e = []
    # body: three stacked octagon-ish boxes fake the round shell
    for i, (inset, y0, y1) in enumerate(((0.0, 1.4, 4.6), (0.5, 4.6, 8.4), (0.0, 8.4, 11.6))):
        e.append(box(2.5 + inset, y0, 3.2 + inset, 13.5 - inset, y1, 12.8 - inset, "side"))
    e.append(box(2.4, 11.6, 3.1, 13.6, 12.2, 12.9, "rim", top="top"))
    e.append(box(2.4, 0.9, 3.1, 13.6, 1.5, 12.9, "rim"))
    # drum head
    e.append(box(3.0, 12.2, 3.7, 13.0, 12.5, 12.3, "top"))
    # lugs
    for x in (3.2, 12.4):
        e.append(box(x, 3.0, 6.6, x + 0.4, 9.0, 9.4, "rim"))
    # crossed drumsticks on top
    e.append(with_rot(box(3.4, 12.5, 7.4, 12.0, 13.1, 7.9, "rim"), (8, 12.6, 7.6), "y", 22.5))
    e.append(with_rot(box(4.0, 12.9, 8.2, 12.6, 13.5, 8.7, "rim"), (8, 13.0, 8.4), "y", -22.5))
    return tex, e


# ------------------------------------------------------------ rocking horse
def rocking_horse():
    tex = {"body": "horse_body", "mane": "horse_mane",
           "rocker": "horse_rocker", "saddle": "horse_saddle"}
    e = []
    # the two rockers: three angled bars each, so the curve reads
    for z in (4.4, 10.6):
        e.append(with_rot(box(1.0, 0.3, z - 0.4, 6.4, 0.9, z + 0.4, "rocker"), (3.6, 0.6, z), "z", 22.5))
        e.append(box(5.4, 0.3, z - 0.4, 10.6, 0.9, z + 0.4, "rocker"))
        e.append(with_rot(box(9.6, 0.3, z - 0.4, 15.0, 0.9, z + 0.4, "rocker"), (12.4, 0.6, z), "z", -22.5))
    # legs
    for z in (4.6, 10.4):
        e.append(box(4.0, 1.0, z - 0.5, 5.2, 5.4, z + 0.5, "body"))
        e.append(box(9.4, 1.0, z - 0.5, 10.6, 5.4, z + 0.5, "body"))
    # barrel + neck + head
    e.append(box(3.6, 5.0, 4.0, 11.4, 9.0, 12.0, "body"))
    e.append(with_rot(box(10.4, 8.4, 5.2, 12.6, 13.2, 10.8, "body"), (11.6, 8.6, 8.0), "z", -22.5))
    e.append(box(11.6, 12.4, 5.0, 15.2, 14.6, 11.0, "body"))
    # muzzle and ears
    e.append(box(14.2, 12.2, 6.2, 15.8, 13.8, 9.8, "mane"))
    e.append(box(12.4, 14.6, 6.0, 13.2, 15.8, 7.0, "mane"))
    e.append(box(12.4, 14.6, 9.0, 13.2, 15.8, 10.0, "mane"))
    # mane along the neck, tail at the back
    for i in range(3):
        e.append(box(10.0 - i * 0.2, 8.6 + i * 1.5, 4.8, 12.2, 10.0 + i * 1.5, 5.4, "mane"))
    e.append(box(2.6, 6.0, 6.6, 3.8, 9.4, 9.4, "mane"))
    # saddle and blanket
    e.append(box(6.4, 9.0, 4.6, 9.6, 9.9, 11.4, "saddle"))
    e.append(box(5.8, 8.6, 4.1, 10.2, 9.2, 11.9, "mane"))
    return tex, e


# --------------------------------------------------------------- nutcracker
def nutcracker():
    tex = {"skin": "nutcracker_skin", "uniform": "nutcracker_uniform",
           "gold": "nutcracker_gold", "hat": "nutcracker_hat",
           "beard": "nutcracker_beard"}
    e = []
    # boots
    e.append(box(4.8, 0.0, 4.6, 7.4, 1.8, 11.4, "hat"))
    e.append(box(3.8, 0.0, 4.6, 7.6, 1.2, 11.4, "hat"))
    # legs
    e.append(box(5.4, 1.8, 5.6, 7.2, 4.6, 10.4, "uniform"))
    e.append(box(8.8, 1.8, 5.6, 10.6, 4.6, 10.4, "uniform"))
    e.append(box(5.2, 1.8, 5.4, 10.8, 4.6, 10.6, "uniform"))
    # jacket + belt
    e.append(box(4.6, 4.6, 5.0, 11.4, 9.4, 11.0, "uniform"))
    e.append(box(4.4, 4.6, 4.8, 11.6, 5.4, 11.2, "gold"))
    e.append(box(7.6, 4.4, 4.6, 8.4, 9.6, 5.2, "gold"))
    # arms held at the sides
    for x0 in (3.6, 11.4):
        e.append(box(x0, 5.0, 5.4, x0 + 1.6, 9.0, 10.6, "uniform"))
        e.append(box(x0, 4.2, 5.8, x0 + 1.6, 5.2, 10.2, "gold"))
    # shoulders
    e.append(box(4.2, 9.0, 5.2, 11.8, 10.2, 10.8, "gold"))
    # head, beard, face
    e.append(box(5.2, 10.2, 5.4, 10.8, 13.6, 10.6, "skin"))
    e.append(box(5.0, 10.2, 5.2, 11.0, 12.2, 10.8, "beard"))
    e.append(box(5.8, 12.2, 5.6, 10.2, 13.4, 6.2, "beard"))
    # eyes, nose, mouth
    e.append(box(6.2, 12.6, 5.2, 6.9, 13.2, 5.5, "hat"))
    e.append(box(9.1, 12.6, 5.2, 9.8, 13.2, 5.5, "hat"))
    e.append(box(7.6, 12.2, 4.9, 8.4, 12.8, 5.6, "skin"))
    e.append(box(6.4, 11.2, 5.2, 9.6, 11.8, 5.6, "gold"))
    # tall hat with a band
    e.append(box(5.0, 13.6, 5.2, 11.0, 15.4, 10.8, "hat"))
    e.append(box(4.6, 15.4, 4.8, 11.4, 16.0, 11.2, "hat"))
    e.append(box(5.0, 13.6, 5.0, 11.0, 14.2, 5.4, "gold"))
    return tex, e


# ----------------------------------------------------------- gingerbread man
def gingerbread_man():
    tex = {"dough": "ginger_bread", "icing": "ginger_icing", "candy": "ginger_candy"}
    e = []
    # stubby legs with rounded feet
    for x0, tilt in ((4.4, 22.5), (8.6, -22.5)):
        e.append(with_rot(box(x0, 0.6, 6.4, x0 + 3.0, 5.2, 9.6, "dough"),
                          (x0 + 1.5, 5.2, 8.0), "z", tilt))
        e.extend(blob(x0 + 1.5, 0.9, 8.0, 1.5, "dough", height=1.4))
    # body, wider at the shoulders
    e.append(box(5.2, 4.6, 5.8, 10.8, 9.4, 10.2, "dough"))
    e.append(box(4.8, 7.0, 5.8, 11.2, 9.4, 10.2, "dough"))
    # arms out to the sides with rounded hands
    for x0, tilt in ((1.4, 22.5), (11.6, -22.5)):
        e.append(with_rot(box(x0, 7.4, 6.4, x0 + 3.4, 9.4, 9.6, "dough"),
                          (x0 + 3.4 if x0 < 8 else x0, 8.4, 8.0), "z", tilt))
        e.extend(blob(x0 + (0.9 if x0 < 8 else 2.5), 8.4, 8.0, 1.8, "dough"))
    # head
    e.extend(blob(8.0, 12.2, 8.0, 2.7, "dough", height=5.0))
    # icing: eyes, smile, buttons, cuffs, zigzag hems
    e.append(box(6.4, 13.4, 5.2, 7.2, 14.2, 5.7, "icing"))
    e.append(box(8.8, 13.4, 5.2, 9.6, 14.2, 5.7, "icing"))
    e.append(box(6.6, 11.6, 5.2, 9.4, 12.2, 5.7, "icing"))
    e.append(box(6.2, 12.2, 5.2, 6.8, 12.8, 5.7, "icing"))
    e.append(box(9.2, 12.2, 5.2, 9.8, 12.8, 5.7, "icing"))
    for y in (5.4, 6.8, 8.2):
        e.append(box(7.4, y, 5.4, 8.6, y + 0.8, 5.9, "candy"))
    e.append(box(1.6, 7.6, 6.4, 2.6, 9.2, 9.6, "icing"))
    e.append(box(13.4, 7.6, 6.4, 14.4, 9.2, 9.6, "icing"))
    e.append(box(4.4, 0.4, 6.2, 7.4, 1.2, 9.8, "icing"))
    e.append(box(8.6, 0.4, 6.2, 11.6, 1.2, 9.8, "icing"))
    return tex, e


# --------------------------------------------------------- gingerbread house
def gingerbread_house():
    tex = {"dough": "ginger_bread", "icing": "ginger_icing", "candy": "ginger_candy"}
    e = []
    # walls
    e.append(box(2.4, 0.0, 2.8, 13.6, 7.2, 13.2, "dough"))
    # gable, stepped so it looks solid
    e.append(box(3.4, 7.2, 3.6, 12.6, 8.6, 12.4, "dough"))
    e.append(box(4.6, 8.6, 4.6, 11.4, 9.8, 11.4, "dough"))
    e.append(box(5.8, 9.8, 5.8, 10.2, 10.8, 10.2, "dough"))
    # snowy roof planes
    e.append(with_rot(box(0.6, 6.4, 2.4, 8.4, 7.2, 13.6, "icing"), (8.0, 7.0, 8.0), "z", -22.5))
    e.append(with_rot(box(7.6, 6.4, 2.4, 15.4, 7.2, 13.6, "icing"), (8.0, 7.0, 8.0), "z", 22.5))
    e.append(with_rot(box(1.2, 7.2, 2.2, 8.2, 7.8, 13.8, "icing"), (8.0, 7.0, 8.0), "z", -22.5))
    e.append(with_rot(box(7.8, 7.2, 2.2, 14.8, 7.8, 13.8, "icing"), (8.0, 7.0, 8.0), "z", 22.5))
    # chimney
    e.append(box(10.2, 8.2, 8.6, 12.2, 12.4, 10.6, "dough"))
    e.append(box(9.9, 12.4, 8.3, 12.5, 12.9, 10.9, "icing"))
    # door
    e.append(box(6.6, 0.0, 2.3, 9.4, 4.6, 2.9, "candy"))
    e.append(box(7.9, 2.0, 2.0, 8.5, 2.7, 2.5, "icing"))
    # windows with icing frames
    for x0 in (3.4, 10.2):
        e.append(box(x0, 3.4, 2.3, x0 + 2.4, 5.4, 2.9, "icing"))
        e.append(box(x0 + 0.4, 3.8, 2.1, x0 + 2.0, 5.0, 2.4, "candy"))
    # icing drips along the eaves
    for i in range(5):
        e.append(box(2.6 + i * 2.4, 6.2, 2.5, 3.8 + i * 2.4, 7.0, 3.1, "icing"))
    # gumdrops in the garden
    e.append(box(1.2, 0.0, 1.4, 3.2, 1.4, 3.2, "candy"))
    e.append(box(12.8, 0.0, 1.4, 14.8, 1.4, 3.2, "candy"))
    return tex, e


# ---------------------------------------------------------------- stocking
def stocking():
    tex = {"felt": "stocking_felt", "trim": "stocking_trim"}
    e = []
    # cuff
    e.append(box(4.6, 12.2, 4.6, 11.4, 14.6, 11.4, "trim"))
    e.append(box(4.2, 13.6, 4.2, 11.8, 15.0, 11.8, "trim"))
    # leg of the stocking, hanging down and turning at the heel
    e.append(box(5.0, 4.4, 5.2, 11.0, 12.2, 10.8, "felt"))
    # toe
    e.append(box(5.0, 2.6, 5.2, 14.6, 6.2, 10.8, "felt"))
    e.append(box(13.4, 3.2, 5.6, 15.2, 6.0, 10.4, "felt"))
    # heel patch
    e.append(box(5.0, 2.6, 5.0, 8.6, 5.0, 5.6, "trim"))
    # snowflake / patch on the front
    e.append(box(7.0, 8.0, 5.0, 9.0, 10.0, 5.4, "trim"))
    e.append(box(7.7, 7.4, 4.9, 8.3, 10.6, 5.5, "trim"))
    e.append(box(6.4, 8.7, 4.9, 9.6, 9.3, 5.5, "trim"))
    # hanging loop
    e.append(box(6.6, 14.6, 7.4, 7.4, 15.8, 8.6, "trim"))
    e.append(box(8.6, 14.6, 7.4, 9.4, 15.8, 8.6, "trim"))
    e.append(box(6.6, 15.4, 7.4, 9.4, 16.0, 8.6, "trim"))
    return tex, e


# ------------------------------------------------------------------ mitten
def mitten():
    tex = {"wool": "mitten_wool", "cuff": "mitten_cuff"}
    e = []
    # cuff at the bottom, palm above it
    e.append(box(4.8, 0.6, 5.0, 11.2, 4.2, 11.0, "cuff"))
    e.append(box(4.4, 0.0, 4.6, 11.6, 1.4, 11.4, "cuff"))
    # palm and fingers
    e.append(box(4.6, 4.2, 5.2, 11.4, 12.4, 10.8, "wool"))
    e.append(box(5.4, 12.4, 5.8, 10.6, 14.4, 10.2, "wool"))
    e.append(box(6.0, 14.4, 6.2, 10.0, 15.2, 9.8, "wool"))
    # thumb sticking out to the side
    e.append(with_rot(box(10.6, 6.0, 6.0, 13.6, 10.6, 10.0, "wool"), (11.0, 8.0, 8.0), "z", -45))
    # snowflake embroidery on the back of the hand
    e.append(box(7.0, 7.4, 5.0, 9.0, 9.4, 5.4, "cuff"))
    e.append(box(7.7, 6.8, 4.9, 8.3, 10.0, 5.5, "cuff"))
    e.append(box(6.4, 8.1, 4.9, 9.6, 8.7, 5.5, "cuff"))
    return tex, e


# -------------------------------------------------------------------- bell
def bell():
    tex = {"gold": "bell_gold", "shine": "bell_shine"}
    e = []

    def slice_at(y0, y1, r, texture="gold", k=0.60):
        e.extend(round_layer(8.0, 8.0, y0, y1, r, texture, k))

    # A bell is mostly straight, flaring only near the mouth. Keeping the
    # radius almost constant through the middle is what stops it looking
    # like a pyramid.
    steps = 9
    for i in range(steps):
        t = i / float(steps - 1)
        y0 = 1.0 + t * 7.4
        if t < 0.45:
            radius = 5.55 - 0.25 * (t / 0.45)          # nearly vertical
        else:
            u = (t - 0.45) / 0.55
            radius = 5.30 - 2.85 * (u ** 1.55)          # shoulder curves in
        slice_at(y0, y0 + 1.0, radius)
    # dome and crown
    slice_at(8.4, 9.2, 2.35, "shine")
    slice_at(9.2, 9.9, 1.95, "shine")
    e.append(box(7.6, 9.9, 7.6, 8.4, 11.4, 8.4, "shine"))
    slice_at(11.4, 12.3, 1.35, "gold", k=0.75)
    e.append(box(7.3, 12.3, 7.3, 8.7, 13.0, 8.7, "shine"))
    # flared lip and clapper
    slice_at(0.0, 0.95, 5.95, "shine", k=0.55)
    slice_at(0.1, 0.85, 1.4, "shine", k=0.8)
    # red ribbon round the waist with a bow at the front
    slice_at(5.6, 6.6, 4.55, "shine", k=0.62)
    e.append(box(7.0, 5.4, 3.6, 9.0, 6.9, 4.7, "gold"))
    return tex, e


# ------------------------------------------------------------- bauble pile
def bauble_pile():
    tex = {"red": "bauble_red", "gold": "bauble_gold", "blue": "bauble_blue_t"}
    e = []

    def sphere(cx, cy, cz, r, texture):
        # a ball is a short stack of octagon slices, widest in the middle
        profile = []
        slices = 5
        for i in range(slices):
            t = (i + 0.5) / slices
            y0 = cy - r * 0.95 + t * r * 1.9 - r * 0.19
            radius = r * math.sqrt(max(0.04, 1.0 - (2.0 * t - 1.0) ** 2))
            profile.append((y0, y0 + r * 0.42, radius * 1.05))
        e.extend(round_body(cx, cz, profile, texture))
        e.append(box(cx - 0.5, cy + r * 0.75, cz - 0.5, cx + 0.5, cy + r * 1.25, cz + 0.5, "gold"))

    sphere(5.2, 3.7, 9.2, 3.5, "red")
    sphere(11.0, 3.3, 8.6, 3.1, "gold")
    sphere(6.8, 9.0, 8.4, 2.8, "blue")
    sphere(11.2, 8.6, 9.6, 2.3, "red")
    sphere(2.8, 1.8, 13.0, 1.8, "blue")
    # candy cane leaning across the bottom of the pile
    e.append(box(1.6, 0.0, 6.0, 5.2, 0.7, 6.9, "gold"))
    e.append(box(1.6, 0.7, 6.0, 2.4, 2.5, 6.9, "gold"))
    return tex, e


# ---------------------------------------------------------------- candy jar
def candy_jar():
    tex = {"glass": "jar_glass", "candy": "jar_candy", "lid": "jar_lid"}
    e = []
    # sweets, drawn first so the glass reads as glass in front of them
    for (cx, cy, cz, r, t) in ((6.6, 2.6, 8.0, 1.25, "candy"), (9.6, 2.7, 7.8, 1.2, "candy"),
                               (8.1, 4.2, 7.9, 1.15, "candy"), (10.2, 4.3, 8.2, 1.05, "candy"),
                               (8.6, 5.7, 8.0, 1.0, "candy")):
        e.extend(round_body(cx, cz, [(cy - r, cy + r, r)], t, k=0.62))
    # glass belly with a shoulder and a neck
    body = [(0.9 + i * 1.05, 0.9 + (i + 1) * 1.05 + 0.05, 4.35 - abs(i - 2) * 0.18)
            for i in range(6)]
    e.extend(round_body(8.0, 8.0, body, "glass"))
    e.extend(round_body(8.0, 8.0, [(7.3, 8.2, 3.4), (8.2, 9.0, 2.3), (9.0, 9.9, 1.6)], "glass"))
    # gold lid and knob
    e.extend(round_body(8.0, 8.0, [(9.9, 10.9, 2.3), (10.9, 11.7, 1.9)], "lid"))
    e.extend(round_body(8.0, 8.0, [(11.7, 12.8, 1.15)], "lid", k=0.75))
    e.extend(round_body(8.0, 8.0, [(12.8, 13.6, 0.8)], "lid", k=0.8))
    # base ring and a paper label
    e.extend(round_body(8.0, 8.0, [(0.0, 0.9, 4.5)], "lid"))
    e.append(box(6.0, 2.6, 3.3, 10.0, 5.8, 3.8, "candy"))
    return tex, e


MODELS = {
    "toy_train_block": toy_train,
    "drum_block": drum,
    "rocking_horse_block": rocking_horse,
    "nutcracker_block": nutcracker,
    "gingerbread_man_block": gingerbread_man,
    "gingerbread_house_block": gingerbread_house,
    "stocking_block": stocking,
    "mitten_block": mitten,
    "bell_block": bell,
    "bauble_pile_block": bauble_pile,
    "candy_jar_block": candy_jar,
}


if __name__ == "__main__":
    for name, maker in MODELS.items():
        textures, elements = maker()
        emit(name, textures, elements)
