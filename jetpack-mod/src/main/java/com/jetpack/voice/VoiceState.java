package com.jetpack.voice;

import com.jetpack.network.NetworkHandler;
import com.jetpack.network.VoicePeersPacket;
import com.jetpack.network.VoiceSettingsPacket;
import net.minecraft.server.level.ServerPlayer;
import net.minecraftforge.network.PacketDistributor;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;

/**
 * Server side truth about voice chat: who is transmitting, who is muted, who
 * has the microphone off, and who can hear whom.
 *
 * <p>Nothing here is persisted. Voice chat is a live thing, so a fresh session
 * always starts with everyone able to talk.
 */
public final class VoiceState {
    /** Players with the microphone on. */
    private static final Set<UUID> MIC_ON = ConcurrentHashMap.newKeySet();
    /** Players who do not want to hear anybody. */
    private static final Set<UUID> DEAFENED = ConcurrentHashMap.newKeySet();
    /** Players nobody can hear (moderator mute). */
    private static final Set<UUID> SERVER_MUTED = ConcurrentHashMap.newKeySet();
    /** Per-player hearing range in blocks. */
    private static final Map<UUID, Integer> RANGE = new ConcurrentHashMap<>();
    /** Last measured level, for the "who is talking" list. */
    private static final Map<UUID, Float> LEVEL = new ConcurrentHashMap<>();
    /** Simple flood protection: frames relayed in the current second. */
    private static final Map<UUID, int[]> BUDGET = new ConcurrentHashMap<>();
    /** Filled by the server tick hook; avoids keeping a MinecraftServer reference here. */
    private static final ThreadLocal<List<ServerPlayer>> PLAYERS =
            ThreadLocal.withInitial(ArrayList::new);

    /** Called once per server tick with the current player list. */
    public static void setPlayerSnapshot(List<ServerPlayer> players) {
        PLAYERS.set(players);
    }

    private VoiceState() {
    }

    // ------------------------------------------------------------- listeners

    public static boolean maySpeak(ServerPlayer player) {
        if (!VoiceConfig.enabled) {
            return false;
        }
        UUID id = player.getUUID();
        return MIC_ON.contains(id) && !SERVER_MUTED.contains(id);
    }

    public static int rangeOf(ServerPlayer player) {
        return RANGE.getOrDefault(player.getUUID(), VoiceConfig.defaultRange);
    }

    /**
     * Everyone who should receive audio from {@code speaker}: same dimension,
     * inside the speaker's range, not deafened, and not the speaker themselves.
     */
    public static List<ServerPlayer> listenersFor(ServerPlayer speaker) {
        List<ServerPlayer> result = new ArrayList<>();
        if (!VoiceConfig.enabled) {
            return result;
        }
        if (!consumeBudget(speaker)) {
            return result;
        }
        int range = rangeOf(speaker);
        double rangeSq = (double) range * range;
        for (ServerPlayer other : PLAYERS.get()) {
            if (other == speaker || other.level() != speaker.level()) {
                continue;
            }
            if (DEAFENED.contains(other.getUUID()) || SERVER_MUTED.contains(speaker.getUUID())) {
                continue;
            }
            if (other.distanceToSqr(speaker) <= rangeSq) {
                result.add(other);
            }
        }
        return result;
    }

    private static boolean consumeBudget(ServerPlayer player) {
        long second = System.currentTimeMillis() / 1000L;
        int[] bucket = BUDGET.computeIfAbsent(player.getUUID(), id -> new int[]{0, 0});
        synchronized (bucket) {
            if (bucket[1] != second) {
                bucket[1] = (int) second;
                bucket[0] = 0;
            }
            if (bucket[0] >= VoiceConfig.MAX_FRAMES_PER_SECOND) {
                return false;
            }
            bucket[0]++;
            return true;
        }
    }

    // -------------------------------------------------------------- settings

    public static void applySettings(ServerPlayer player, boolean microphoneOn, int range) {
        if (microphoneOn) {
            MIC_ON.add(player.getUUID());
        } else {
            MIC_ON.remove(player.getUUID());
        }
        RANGE.put(player.getUUID(), VoiceConfig.clampRange(range));
    }

    public static void setDeafened(ServerPlayer player, boolean deafened) {
        if (deafened) {
            DEAFENED.add(player.getUUID());
        } else {
            DEAFENED.remove(player.getUUID());
        }
    }

    public static boolean isDeafened(ServerPlayer player) {
        return DEAFENED.contains(player.getUUID());
    }

    public static void setServerMuted(ServerPlayer player, boolean muted) {
        if (muted) {
            SERVER_MUTED.add(player.getUUID());
            MIC_ON.remove(player.getUUID());
        } else {
            SERVER_MUTED.remove(player.getUUID());
        }
    }

    public static boolean isServerMuted(ServerPlayer player) {
        return SERVER_MUTED.contains(player.getUUID());
    }

    public static void setRange(ServerPlayer player, int range) {
        RANGE.put(player.getUUID(), VoiceConfig.clampRange(range));
    }

    public static void setLevel(ServerPlayer player, float level) {
        LEVEL.put(player.getUUID(), level);
    }

    public static void forget(ServerPlayer player) {
        UUID id = player.getUUID();
        MIC_ON.remove(id);
        DEAFENED.remove(id);
        SERVER_MUTED.remove(id);
        RANGE.remove(id);
        LEVEL.remove(id);
        BUDGET.remove(id);
    }

    // ------------------------------------------------------------- peer list

    /** Pushes the nearby-player list to everyone, once a second. */
    public static void broadcastPeers() {
        if (!VoiceConfig.enabled) {
            return;
        }
        for (ServerPlayer player : PLAYERS.get()) {
            if (player.level() == null) {
                continue;
            }
            int theirRange = rangeOf(player);
            List<VoicePeersPacket.Peer> peers = new ArrayList<>();
            for (ServerPlayer other : PLAYERS.get()) {
                if (other.level() != player.level()) {
                    continue;
                }
                if (other == player) {
                    continue;
                }
                int distance = (int) Math.sqrt(other.distanceToSqr(player));
                if (distance > theirRange) {
                    continue;
                }
                float level = LEVEL.getOrDefault(other.getUUID(), 0.0F);
                peers.add(new VoicePeersPacket.Peer(
                        other.getGameProfile().getName(), distance, level, level > 0.02F));
            }
            peers.sort(java.util.Comparator.comparingInt(VoicePeersPacket.Peer::distance));
            if (peers.size() > 12) {
                peers = peers.subList(0, 12);
            }
            NetworkHandler.INSTANCE.send(PacketDistributor.PLAYER.with(() -> player),
                    new VoicePeersPacket(theirRange, peers));
        }
    }

    /** Asks the client to open the voice settings screen. */
    public static void openScreenFor(ServerPlayer player) {
        NetworkHandler.INSTANCE.send(PacketDistributor.PLAYER.with(() -> player),
                new com.jetpack.network.VoiceOpenPacket());
    }

    /** Sends our own settings back so the screen shows what the server thinks. */
    public static void syncTo(ServerPlayer player) {
        NetworkHandler.INSTANCE.send(PacketDistributor.PLAYER.with(() -> player),
                new VoiceSettingsPacket(MIC_ON.contains(player.getUUID()), rangeOf(player)));
    }
}
