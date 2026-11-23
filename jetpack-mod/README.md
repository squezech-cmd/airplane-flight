# Custom NPCs — Minecraft 1.20.1 Forge

A CustomNPCs-style mod: place characters in your world and make them yours.

## What it does

| Item | Use |
| --- | --- |
| **NPC Spawner** | right-click the ground to place an NPC; shift-right-click cycles its behaviour |
| **NPC Editor Wand** | right-click an NPC to open the editor; sneak-right-click to copy it; right-click a block to place a copy |

Every NPC has a name, a dialogue line, a behaviour, a size, hostility and
invulnerability toggles, and can hold an item or wear a helmet. All of it is
saved in the world and synced, so two players see the same character.

## Behaviours

- **Wander** — strolls around its home position, opens doors, avoids falling.
- **Follow** — walks with its owner, teleports if left far behind.
- **Guard** — stays put and watches players go by.

Turn on **Hostile** and it will also attack players and monsters.

## Commands

`/npc spawn <name>`, `/npc list`, `/npc remove`, `/npc removeall`,
`/npc behaviour <wander|follow|guard>`, `/npc name <name>`,
`/npc dialogue <text>`, `/npc scale <50–200>`, `/npc hostile <on|off>`,
`/npc invulnerable <on|off>`.

Commands target the nearest NPC within 12 blocks, except `list` and `removeall`.

`/npc help` and `/npc list` work for every player; the rest need permission
level 2 (operator).

## Server settings

`config/customnpcs-common.toml` is created on first launch:

| Key | Default | Meaning |
| --- | --- | --- |
| `allowEditing` | `true` | whether players may open the editor at all |
| `ownerOnlyEditing` | `true` | only the player who placed an NPC may edit it |
| `defaultScale` | `100` | size of newly placed NPCs, 50–200% |
| `defaultInvulnerable` | `true` | new NPCs start out unable to be hurt |
| `maxNpcsPerPlayer` | `50` | cap per player, `0` = unlimited |
| `wandRange` | `8` | reach of the wand, in blocks |

Operators always bypass the first two: a server owner should never be locked
out of their own world by their own config.

## Skins

The skin field accepts `steve`, `alex`, or any texture path such as
`customnpcs:textures/entity/guard.png`. A resource pack can ship its own 64×64
skins and NPCs will use them.

## Building

```bash
cd jetpack-mod
gradle build
# jar in build/libs/customnpcs-1.1.1.jar
```

Requires JDK 17. The directory is still called `jetpack-mod` because that is the
path the CI workflow and Gradle cache are set up around; the mod inside it is
Custom NPCs.

## Install

1. Forge 47.2.0+ for Minecraft 1.20.1.
2. Drop `customnpcs-1.1.1.jar` into `mods`.
3. Remove any older `jetpack-*.jar` — this mod replaces it.
