package com.jetpack.voice;

import com.jetpack.JetpackMod;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.level.ServerPlayer;
import net.minecraftforge.event.TickEvent;
import net.minecraftforge.event.entity.player.PlayerEvent;
import net.minecraftforge.event.server.ServerStoppedEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;

import java.util.ArrayList;

/**
 * Server side glue for voice chat: keeps the player snapshot up to date,
 * pushes the nearby-player list once a second, and clears per-player state on
 * logout so a new session always starts clean.
 */
@Mod.EventBusSubscriber(modid = JetpackMod.MOD_ID)
public final class VoiceServerEvents {
    private static int tickCounter;

    private VoiceServerEvents() {
    }

    @SubscribeEvent
    public static void onServerTick(TickEvent.ServerTickEvent event) {
        if (event.phase != TickEvent.Phase.END) {
            return;
        }
        MinecraftServer server = event.getServer();
        if (server == null) {
            return;
        }
        if (tickCounter++ % 20 != 0) {
            return;
        }
        ArrayList<ServerPlayer> players = new ArrayList<>(server.getPlayerList().getPlayers());
        VoiceState.setPlayerSnapshot(players);
        VoiceState.broadcastPeers();
    }

    @SubscribeEvent
    public static void onLogout(PlayerEvent.PlayerLoggedOutEvent event) {
        if (event.getEntity() instanceof ServerPlayer player) {
            VoiceState.forget(player);
        }
    }

    @SubscribeEvent
    public static void onLogin(PlayerEvent.PlayerLoggedInEvent event) {
        if (event.getEntity() instanceof ServerPlayer player) {
            // Start with the microphone off: nobody should be broadcast before
            // they have opened the settings screen at least once.
            VoiceState.applySettings(player, false, VoiceConfig.defaultRange);
            VoiceState.syncTo(player);
        }
    }

    @SubscribeEvent
    public static void onServerStopped(ServerStoppedEvent event) {
        VoiceState.setPlayerSnapshot(new ArrayList<>());
        tickCounter = 0;
    }
}
