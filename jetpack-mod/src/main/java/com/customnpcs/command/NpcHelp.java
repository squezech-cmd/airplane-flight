package com.customnpcs.command;

import net.minecraft.ChatFormatting;
import net.minecraft.network.chat.Component;

import java.util.List;

/**
 * In-game instructions.
 *
 * <p>A player who installs this mod and does not read the release notes should
 * still be able to figure it out, so {@code /npc help} prints the whole flow in
 * chat: how to place one, how to edit it, and what each setting does.
 */
public final class NpcHelp {
    private static final List<String> LINES = List.of(
            "1. Right-click the ground with the NPC Spawner to place a character.",
            "2. Right-click it with the Editor Wand to open the menu.",
            "3. Name, dialogue, skin, behaviour, size and gear are all there.",
            "4. Empty hand + right-click makes the NPC say its line.",
            "5. Sneak + right-click with the wand copies it; then right-click the ground to place copies.",
            "6. Shift + right-click with the spawner cycles Wander / Follow / Guard.",
            "",
            "Skins: steve, alex, or a texture path like customnpcs:textures/entity/guard.png",
            "Behaviour: Wander walks near home, Follow comes with you, Guard stands still.",
            "Hostile NPCs fight. Invulnerable NPCs cannot be hurt.",
            "",
            "Server settings live in config/customnpcs-common.toml");

    private NpcHelp() {
    }

    public static void send(net.minecraft.commands.CommandSourceStack source) {
        source.sendSuccess(() -> Component.literal("Custom NPCs — how to use")
                .withStyle(ChatFormatting.AQUA), false);
        for (String line : LINES) {
            source.sendSuccess(() -> Component.literal(line).withStyle(ChatFormatting.GRAY), false);
        }
    }
}
