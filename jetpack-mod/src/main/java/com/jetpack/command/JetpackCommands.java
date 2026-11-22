package com.jetpack.command;

import com.jetpack.event.WinterConfig;
import com.jetpack.event.WinterStorm;
import com.mojang.brigadier.CommandDispatcher;
import com.mojang.brigadier.arguments.IntegerArgumentType;
import net.minecraft.ChatFormatting;
import net.minecraft.commands.CommandSourceStack;
import net.minecraft.commands.Commands;
import net.minecraft.network.chat.Component;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.server.level.ServerPlayer;
import net.minecraftforge.event.RegisterCommandsEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;
import com.jetpack.JetpackMod;

/**
 * Everything the mod can be told to do from chat:
 *
 * <pre>
 * /jetpack snow              toggle the ambient snowfall
 * /jetpack blizzard          start a blizzard now
 * /jetpack blizzard 120      ... lasting 120 seconds
 * /jetpack blizzard stop     stop it early
 * /jetpack auto on|off       automatic blizzards
 * </pre>
 */
@Mod.EventBusSubscriber(modid = JetpackMod.MOD_ID)
public final class JetpackCommands {
    private JetpackCommands() {
    }

    @SubscribeEvent
    public static void onRegisterCommands(RegisterCommandsEvent event) {
        CommandDispatcher<CommandSourceStack> dispatcher = event.getDispatcher();

        dispatcher.register(Commands.literal("jetpack")
                .then(Commands.literal("snow")
                        .executes(context -> toggleSnow(context.getSource())))
                .then(Commands.literal("blizzard")
                        .executes(context -> startBlizzard(context.getSource(), 45))
                        .then(Commands.argument("seconds", IntegerArgumentType.integer(1, 600))
                                .executes(context -> startBlizzard(context.getSource(),
                                        IntegerArgumentType.getInteger(context, "seconds"))))
                        .then(Commands.literal("stop")
                                .executes(context -> stopBlizzard(context.getSource()))))
                .then(Commands.literal("auto")
                        .then(Commands.literal("on").executes(context -> setAuto(context.getSource(), true)))
                        .then(Commands.literal("off").executes(context -> setAuto(context.getSource(), false)))
                        .executes(context -> reply(context.getSource(),
                                "Automatic blizzards are " + (WinterConfig.autoStorms ? "on" : "off"),
                                ChatFormatting.AQUA))));
    }

    private static int toggleSnow(CommandSourceStack source) {
        WinterConfig.snowfallEnabled = !WinterConfig.snowfallEnabled;
        return reply(source,
                "Snowfall is now " + (WinterConfig.snowfallEnabled ? "on" : "off"),
                WinterConfig.snowfallEnabled ? ChatFormatting.AQUA : ChatFormatting.GRAY);
    }

    private static int startBlizzard(CommandSourceStack source, int seconds) {
        ServerLevel level = source.getLevel();
        WinterStorm.trigger(level, seconds);
        return reply(source, "Blizzard started for " + seconds + " seconds", ChatFormatting.AQUA);
    }

    private static int stopBlizzard(CommandSourceStack source) {
        WinterStorm.stop(source.getLevel());
        return reply(source, "Blizzard stopped", ChatFormatting.GRAY);
    }

    private static int setAuto(CommandSourceStack source, boolean on) {
        WinterConfig.autoStorms = on;
        return reply(source, "Automatic blizzards " + (on ? "enabled" : "disabled"),
                on ? ChatFormatting.AQUA : ChatFormatting.GRAY);
    }

    private static int reply(CommandSourceStack source, String message, ChatFormatting colour) {
        Component component = Component.literal(message).withStyle(colour);
        if (source.getEntity() instanceof ServerPlayer player) {
            player.sendSystemMessage(component);
        } else {
            source.sendSuccess(() -> component, false);
        }
        return 1;
    }
}
