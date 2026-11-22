# Jetpack — Minecraft 1.20.1 (Forge)

A cool jetpack mod for Minecraft **1.20.1** on **Forge**.

## Features

- **Jetpack** item — grab it from the creative inventory (tab **"Jetpack"**), equip it on your chest and hold **SPACE** to fly
- **Custom 3D model on your back** — twin red fuel tanks, a steel spine plate and two glowing thruster nozzles
- Hold **SHIFT** while flying to descend faster
- Fuel gauge on the item (orange bar), refuels automatically when you are not thrusting
- Flame and smoke particles + rocket sound while flying
- Soft landing: jetpack stabilizers prevent fall damage and stop you from dropping too fast

## How flight works

Thrust is applied on the **client**, which is what actually moves the player. The server
only spends fuel, spawns exhaust for other players and clears vanilla's
"above ground" counter so nobody gets kicked for flying. The `accesstransformer.cfg`
is what unlocks that counter.

## Textures

`tools/generate_textures.py` repaints both `jetpack_layer_1.png` (the worn 64x64 model)
and `jetpack.png` (the 16x16 inventory sprite). The UV layout in the script matches the
boxes in `JetpackModel.createBodyLayer()` pixel for pixel.

## Install

1. Install **Forge 47.2.0+** for Minecraft 1.20.1
2. Download `jetpack-1.1.0.jar` from the [Releases](../../releases) page
3. Put the JAR into the `mods` folder
4. Launch the game, open the creative inventory, take the Jetpack from the **Jetpack** tab and fly!

## Build

```bash
gradle build
```

The mod JAR is produced at `build/libs/jetpack-1.1.0.jar`.
