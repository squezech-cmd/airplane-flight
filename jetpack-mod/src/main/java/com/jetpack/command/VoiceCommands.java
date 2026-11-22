package com.jetpack.command;

import com.jetpack.voice.VoiceConfig;
import com.jetpack.voice.VoiceState;
import com.mojang.brigadier.CommandDispatcher;
import net.minecraft.ChatFormatting;
import net.minecraft.commands.CommandSourceStack;
import net.minecraft.commands.Commands;
import net.minecraft.commands.arguments.EntityArgument;
import net.minecraft.network.chat.Component;
import net.minecraft.server.level.ServerPlayer;

/**
 * {@code /voice} — the voice chat commands.
 *
 * <pre>
 * /voice                  open the settings screen (client side)
 * /voice mute             stop hearing anyone
 * /voice unmute           hear people again
 * /voice range &lt;blocks&gt;   how far your voice carries
 * /voice mode &lt;on|off|ptt&gt; server side default for everybody
 * /voice muteplayer &lt;player&gt;  moderator mute, needs permission level 2
 * </pre>
 */
public final class VoiceCommands {
    private VoiceCommands() {
    }

    public static void register(CommandDispatcher<CommandSourceStack> dispatcher) {
        dispatcher.register(Commands.literal("voice")
                .executes(context -> openScreen(context.getSource()))
                .then(Commands.literal("mute")
                        .executes(context -> setDeafened(context.getSource(), true)))
                .then(Commands.literal("unmute")
                        .executes(context -> setDeafened(context.getSource(), false)))
                .then(Commands.literal("range")
                        .then(Commands.argument("blocks", com.mojang.brigadier.arguments.IntegerArgumentType
                                        .integer(VoiceConfig.MIN_RANGE, VoiceConfig.MAX_RANGE))
                                .executes(context -> setRange(context.getSource(),
                                        com.mojang.brigadier.arguments.IntegerArgumentType
                                                .getInteger(context, "blocks")))))
                .then(Commands.literal("mode")
                        .requires(source -> source.hasPermission(2))
                        .then(Commands.literal("on").executes(context -> setEnabled(context.getSource(), true)))
                        .then(Commands.literal("off").executes(context -> setEnabled(context.getSource(), false))))
                .then(Commands.literal("muteplayer")
                        .requires(source -> source.hasPermission(2))
                        .then(Commands.argument("player", EntityArgument.player())
                                .executes(context -> mutePlayer(context.getSource(),
                                        EntityArgument.getPlayer(context, "player"), true))))
                .then(Commands.literal("unmuteplayer")
                        .requires(source -> source.hasPermission(2))
                        .then(Commands.argument("player", EntityArgument.player())
                                .executes(context -> mutePlayer(context.getSource(),
                                        EntityArgument.getPlayer(context, "player"), false)))));
    }

    private static int openScreen(CommandSourceStack source) {
        // The screen is opened on the client by the VoiceKeyEvents hook when it
        // sees this command come back; to keep it simple the command just tells
        // the player the hotkey and asks the client to open it.
        ServerPlayer player = source.getPlayer();
        if (player == null) {
            return reply(source, "Only a player can open the voice settings screen",
                    ChatFormatting.RED);
        }
        com.jetpack.voice.VoiceState.openScreenFor(player);
        return 1;
    }

    private static int setDeafened(CommandSourceStack source, boolean deafened) {
        ServerPlayer player = source.getPlayer();
        if (player == null) {
            return reply(source, "Only a player can mute themselves", ChatFormatting.RED);
        }
        VoiceState.setDeafened(player, deafened);
        return reply(source, deafened ? "You will not hear anyone" : "You can hear people again",
                deafened ? ChatFormatting.GRAY : ChatFormatting.GREEN);
    }

    private static int setRange(CommandSourceStack source, int blocks) {
        ServerPlayer player = source.getPlayer();
        if (player == null) {
            return reply(source, "Only a player has a voice range", ChatFormatting.RED);
        }
        int clamped = VoiceConfig.clampRange(blocks);
        VoiceState.setRange(player, clamped);
        return reply(source, "Your voice now carries " + clamped + " blocks", ChatFormatting.AQUA);
    }

    private static int setEnabled(CommandSourceStack source, boolean enabled) {
        VoiceConfig.enabled = enabled;
        for (ServerPlayer player : source.getServer().getPlayerList().getPlayers()) {
            player.sendSystemMessage(Component.literal(
                            enabled ? "Voice chat has been enabled" : "Voice chat has been disabled")
                    .withStyle(enabled ? ChatFormatting.GREEN : ChatFormatting.RED));
        }
        return 1;
    }

    private static int mutePlayer(CommandSourceStack source, ServerPlayer target, boolean muted) {
        VoiceState.setServerMuted(target, muted);
        target.sendSystemMessage(Component.literal(
                        muted ? "You have been muted in voice chat" : "You can speak in voice chat again")
                .withStyle(muted ? ChatFormatting.RED : ChatFormatting.GREEN));
        return reply(source, target.getGameProfile().getName() + (muted ? " muted" : " unmuted"),
                ChatFormatting.AQUA);
    }

    private static int reply(CommandSourceStack source, String message, ChatFormatting colour) {
        source.sendSuccess(() -> Component.literal(message).withStyle(colour), false);
        return 1;
    }
}
