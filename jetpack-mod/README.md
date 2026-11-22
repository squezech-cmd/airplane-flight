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

## Decorations you can place

Eleven decorations are **real blocks**, not inventory junk — every one has a
facing property, its own collision box, a light level and idle particles:

| Block | Shines | Particles |
| --- | --- | --- |
| Toy Train | – | golden sparkles |
| Little Drum | – | – |
| Rocking Horse | – | – |
| Nutcracker | 3 | warm motes |
| Gingerbread Man | – | – |
| Gingerbread House | 4 | warm motes |
| Stocking | – | – |
| Mitten | – | – |
| Jingle Bell | 7 | golden sparkles |
| Box of Baubles | – | snow glitter |
| Jar of Candy | – | golden sparkles |

They rotate to face you when placed, so a shelf of them looks deliberate.

## The full Christmas catalogue

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

Every one of them has a recipe, and so does every block — 83 in total.

## Three trees, three sizes

Every tree is a cascade of branch plates with drooping boughs, a spiral light
garland and baubles hanging off the branches — no stacks of cubes.

| Tree | Size | Model |
| --- | --- | --- |
| **Christmas Tree** | 1 block | 1 part, light 11 |
| **Grand Christmas Tree** | 2 blocks | 2 parts, light 12 |
| **Giant Christmas Tree** | **3 blocks** | 3 parts, 186 elements, light 14 |

The giant tree is the centrepiece: eight drooping tiers, a garland of coloured
bulbs winding 2.6 times around the trunk, fifteen baubles and a four-armed
glowing star. Only the trunk is solid, so you can walk under the branches.
Break any part and the whole tree comes down, but it only drops one item.

## Voice chat

Proximity voice chat, built in, no extra libraries. It opens from the **Voice chat**
button in the pause menu, from **Left Alt**, or from `/voice`.

- **Push to talk** by default: hold **V** while you play, release to stop
- Open microphone mode if you would rather not hold a key
- Your voice carries **32 blocks** by default and fades with distance
- **Range 8–128 blocks**, adjustable per player
- **Noise gate** so breathing, fans and keyboards stay off the wire
- **Microphone picker**, gain with a live level meter, and a monitor button to
  hear yourself
- **HUD** shows your microphone level and the names of anyone speaking near you
- Mute yourself from the screen, or with `/voice mute`

`/voice` opens the settings, `/voice range <8-128>` sets how far you are heard,
and operators get `/voice mode on|off` and `/voice muteplayer <player>`.

### How the audio travels

The microphone is recorded at 48 kHz, decimated to 16 kHz wideband speech and
companded with G.711 mu-law. That is 16 KB/s per speaker instead of 96 KB/s of
raw PCM. The server validates every frame, applies a rate limit, and only relays
it to players inside the speaker's range, so a busy server keeps up. On the
receiving side each speaker gets a short queue and a dedicated playback thread:
during a stutter a frame is dropped rather than everyone falling behind.

Everything is plain Java (`javax.sound.sampled`), so there is nothing to install
and the mod still loads on a dedicated server.

## Blizzard commands

Snow and storms are now under your control:

| Command | What it does |
| --- | --- |
| `/jetpack snow` | toggle the ambient snowfall on or off |
| `/jetpack blizzard` | start a 45 second blizzard right now |
| `/jetpack blizzard <seconds>` | a blizzard of 1–600 seconds |
| `/jetpack blizzard stop` | end it early |
| `/jetpack auto on/off` | automatic blizzards every 6 minutes |

Without commands a blizzard still rolls in on its own: first one 30 seconds
after the world loads, then every six minutes, 45 seconds each. Dense sideways
snow, drifting ground particles, gusting wind and a mild chill, announced in
chat when it starts and when it passes.

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
2. Download the newest `jetpack-3.x.x.jar` from the [Releases](../../releases) page
3. Put the JAR into the `mods` folder
4. Launch the game, open the creative inventory and look for the **Jetpack** tabs

## Build

```bash
gradle build
```

The mod JAR is produced at `build/libs/jetpack-3.1.0.jar`.

## How flight works

Thrust is applied on the **client**, which is what actually moves the player. The server
only spends fuel, spawns exhaust for other players and clears vanilla's
"above ground" counter so nobody gets kicked for flying. The `accesstransformer.cfg`
is what unlocks that counter.

## Textures and models

Self-contained generators, no external assets:

```bash
python3 tools/generate_textures.py             # jetpack item + worn layer
python3 tools/generate_christmas_textures.py   # item and block textures
python3 tools/generate_tree_models.py          # the three trees + their textures
python3 tools/generate_decor_models.py         # the eleven decoration blocks
python3 tools/preview_tree_models.py           # isometric PNG previews in docs/
```

The models are generated rather than hand-typed: `generate_tree_models.py` builds
a tree in absolute coordinates, then slices it into per-block parts so no element
ever exceeds the 32 units the game allows. `preview_tree_models.py` renders the
result as an isometric PNG before you build, which is how the trees were tuned.
