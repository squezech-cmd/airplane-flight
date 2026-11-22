# Jetpack — Minecraft 1.20.1 (Forge)

A cool jetpack mod **plus a full Christmas update** for Minecraft **1.20.1** on **Forge**.

Everything is available in the creative inventory from two tabs: **Jetpack** and
**Jetpack Christmas**. Everything is also craftable in survival.

## Jetpack

- **Jetpack** item — equip it on your chest and hold **SPACE** to fly
- Hold **SHIFT** while flying to descend faster
- **Custom 3D model on your back** — twin red fuel tanks, a steel spine plate and two glowing thruster nozzles
- Fuel gauge on the item (orange bar), refuels automatically when you are not thrusting
- Flame and smoke particles + rocket sound while flying
- Soft landing: the jetpack catches you and you never take fall damage

### Jetpack recipe

```
I T I      I = Iron Ingot
I F I      T = Redstone Torch
I C I      F = Furnace
           C = Fire Charge
```

## Christmas blocks

| Block | Recipe |
| --- | --- |
| **Christmas Tree** (glowing star, ornaments, snow) | 4 spruce saplings + 1 Christmas Star |
| **Snowman** (carrot nose, scarf, top hat) | 4 snow blocks + carrot + coal |
| **Wreath** (hangs flat on a wall) | 4 oak leaves |
| **Candy Cane** | 3 sugar |
| **Snow Lantern** (full light level) | 4 snow blocks + 2 glass + glowstone dust |
| **Red / Green / Golden Present** | paper ring + red / green / yellow wool |

Presents are interactive — right-click one and it pops open with a small surprise
(cookies, gold nuggets, a diamond if you're lucky), then drops itself so you can
wrap it up again.

## Christmas items

| Item | Recipe | What it does |
| --- | --- | --- |
| **Christmas Star** | 4 gold ingots + emerald | Tree topper and crafting ingredient |
| **Santa Hat** | 5 red wool + 3 white wool | A real 3D hat that jingles while worn |
| **Hot Cocoa** | milk + cocoa beans + sugar | Night Vision (30s) + Speed (20s) |
| **Snow Globe** | 8 glass + snowball | Right-click for a private snowstorm |
| **Festive Firework** | paper + firework star | Random red/green/gold/ice-blue bursts |

## The full Christmas catalogue (52 items)

**Decorations** — snowflake, jingle bell, tinsel, fairy lights, gingerbread man,
gingerbread house, nutcracker, rocking horse, toy train, little drum, stocking,
mitten, three glass baubles, star garland.

**Sweets** — peppermint candy, candy apple, chocolate bar, gingerbread cookie,
Christmas pudding, fruitcake, roasted chestnuts (speed), marshmallow, Christmas
cake (regeneration), sugar plum (night vision), snow cone (fire resistance),
sweet cane, marzipan pig, yule log (resistance).

**Hot drinks** — eggnog (health boost), mulled cider (resistance), peppermint tea
(night vision), Christmas punch, snowberry smoothie (speed), warm cocoa
(regeneration).

**Trinkets** — ice shard, frozen heart, snowy pearl, lucky snowball, stardust,
mistletoe, holly berries, pine cone.

**Frost tools** — a full ice tier between iron and diamond: sword (slows what it
hits), pickaxe, axe, shovel.

**Materials** — wrapping paper, red bow, gold bow, pine needles, snow dust.

Every one of them has a recipe — 67 in total.

## Grand Christmas Tree

Two blocks tall, five foliage tiers, a light garland, baubles and a glowing star.
It twinkles, drops glitter and casts light level 12.

## Blizzards

Every few minutes a real blizzard rolls in: dense sideways snow, drifting ground
particles, gusting wind sounds and a mild chill, announced in chat and gone again
45 seconds later.

## Animations

- Trees twinkle and shed glitter
- Fairy lights flicker along the wire
- Sparkles drift around the player
- Frosty breath puffs when it snows
- Shooting stars on clear nights
- Holly bushes and snow piles shimmer

## Winter atmosphere

- **Snowfall** follows every player, dense enough to feel wintry
- **Ground frost** slowly paints the world white, only near the surface
- **Rain becomes snow** — and snow drifts build up into layered piles
- Every player gets a small welcome gift the first time they join

## Install

1. Install **Forge 47.2.0+** for Minecraft 1.20.1
2. Download `jetpack-2.1.0.jar` from the [Releases](../../releases) page
3. Put the JAR into the `mods` folder
4. Launch the game, open the creative inventory and look for the **Jetpack** tabs

## Build

```bash
gradle build
```

The mod JAR is produced at `build/libs/jetpack-2.1.0.jar`.

## How flight works

Thrust is applied on the **client**, which is what actually moves the player. The server
only spends fuel, spawns exhaust for other players and clears vanilla's
"above ground" counter so nobody gets kicked for flying. The `accesstransformer.cfg`
is what unlocks that counter.

## Textures and models

Self-contained generators, no external assets:

```bash
python3 tools/generate_textures.py            # jetpack item + worn layer
python3 tools/generate_christmas_textures.py  # everything Christmas
```

The block models in `assets/jetpack/models/block/` are hand-authored JSON; the UV
layouts in the texture scripts match the boxes in the models pixel for pixel.
