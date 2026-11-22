package com.jetpack.voice;

import com.jetpack.network.NetworkHandler;
import com.jetpack.network.VoicePacket;
import com.jetpack.network.VoicePeersPacket;
import com.jetpack.network.VoiceSettingsPacket;
import net.minecraft.client.Minecraft;
import net.minecraft.client.multiplayer.ClientLevel;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.phys.Vec3;

import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;

/**
 * Client side voice session: keeps one {@link SpeakerLine} per speaker, works
 * out how loud each of them should be from where they stand, and owns the
 * microphone thread.
 */
public final class VoiceClient {
    private static final Map<UUID, SpeakerLine> SPEAKERS = new HashMap<>();
    private static final Map<UUID, String> NAMES = new HashMap<>();
    private static volatile List<VoicePeersPacket.Peer> nearby = List.of();
    private static volatile int serverRange = VoiceConfig.defaultRange;
    private static VoiceSettingsPacket lastSettingsFromServer;
    private static boolean started;

    private VoiceClient() {
    }

    // ------------------------------------------------------------ lifecycle

    public static void tick() {
        VoiceSettings settings = VoiceSettings.get();
        if (settings.enabled && !started) {
            MicrophoneCapture.start();
            started = true;
        } else if (!settings.enabled && started) {
            MicrophoneCapture.stop();
            started = false;
        }

        Minecraft minecraft = Minecraft.getInstance();
        if (minecraft.player == null || minecraft.level == null) {
            shutdown();
            return;
        }

        ClientLevel level = minecraft.level;
        Vec3 listener = minecraft.player.getEyePosition();

        SPEAKERS.entrySet().removeIf(entry -> {
            if (entry.getValue().isIdle()) {
                entry.getValue().close();
                return true;
            }
            return false;
        });

        for (Map.Entry<UUID, SpeakerLine> entry : SPEAKERS.entrySet()) {
            Player speaker = level.getPlayerByUUID(entry.getKey());
            if (speaker == null) {
                continue;
            }
            entry.getValue().updateSpatial(listener, speaker.getEyePosition(), serverRange);
        }
    }

    public static void shutdown() {
        for (SpeakerLine line : SPEAKERS.values()) {
            line.close();
        }
        SPEAKERS.clear();
        NAMES.clear();
    }

    public static void onDisconnect() {
        shutdown();
        MicrophoneCapture.stop();
        started = false;
    }

    // ------------------------------------------------------------ packets in

    public static void accept(VoicePacket packet) {
        Minecraft minecraft = Minecraft.getInstance();
        Player self = minecraft.player;
        if (self == null) {
            return;
        }
        if (self.getUUID().equals(packet.getSpeaker())) {
            return; // never play yourself back through the network
        }
        if (!VoiceSettings.get().enabled) {
            return;
        }
        NAMES.put(packet.getSpeaker(), packet.getSpeakerName());
        SpeakerLine line = SPEAKERS.computeIfAbsent(packet.getSpeaker(), id -> new SpeakerLine());
        for (int i = 0; i < packet.getFrames().size(); i++) {
            line.push(packet.getFrames().get(i), packet.getLevels().get(i));
        }
    }

    public static void acceptPeers(VoicePeersPacket packet) {
        serverRange = packet.getRange();
        nearby = packet.getPeers();
    }

    // ----------------------------------------------------------- packets out

    public static void sendSettings() {
        VoiceSettings settings = VoiceSettings.get();
        NetworkHandler.INSTANCE.sendToServer(
                new VoiceSettingsPacket(settings.transmits(), settings.range));
    }

    public static void onServerSettings(VoiceSettingsPacket packet) {
        lastSettingsFromServer = packet;
        // the server just told us what it thinks; make sure it knows what we
        // actually want, including the range from our own config file
        sendSettings();
    }

    /** Called on the client thread when the server answers {@code /voice}. */
    public static void openScreen() {
        Minecraft minecraft = Minecraft.getInstance();
        if (minecraft.player != null) {
            minecraft.setScreen(new com.jetpack.client.VoiceSettingsScreen(
                    minecraft.screen instanceof com.jetpack.client.VoiceSettingsScreen
                            ? null : minecraft.screen));
        }
    }

    // ------------------------------------------------------------- accessors

    /** What the server last told us it believes, for the settings screen. */
    public static VoiceSettingsPacket serverSettings() {
        return lastSettingsFromServer;
    }

    public static List<VoicePeersPacket.Peer> nearby() {
        return nearby;
    }

    public static int serverRange() {
        return serverRange;
    }

    public static float levelOf(UUID speaker) {
        SpeakerLine line = SPEAKERS.get(speaker);
        return line == null ? 0.0F : line.level();
    }

    public static boolean speaking(UUID player) {
        return levelOf(player) > 0.02F;
    }

    public static int activeSpeakers() {
        return SPEAKERS.size();
    }

    public static String displayName(UUID speaker) {
        return NAMES.getOrDefault(speaker, "player");
    }
}
