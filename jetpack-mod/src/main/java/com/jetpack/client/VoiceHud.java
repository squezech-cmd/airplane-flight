package com.jetpack.client;

import com.jetpack.network.VoicePeersPacket;
import com.jetpack.voice.MicrophoneCapture;
import com.jetpack.voice.VoiceClient;
import com.jetpack.voice.VoiceSettings;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;

import java.util.List;

/**
 * A small, quiet voice indicator in the corner of the screen: the microphone
 * level, whether you are transmitting, and the names of anyone speaking near
 * you. Can be turned off from the settings screen.
 */
public final class VoiceHud {
    private VoiceHud() {
    }

    public static void render(GuiGraphics graphics, float partialTick) {
        VoiceSettings settings = VoiceSettings.get();
        if (!settings.enabled || !settings.showHud) {
            return;
        }
        Minecraft minecraft = Minecraft.getInstance();
        if (minecraft.player == null || minecraft.options.hideGui || minecraft.options.renderDebug) {
            return;
        }
        if (minecraft.screen != null && !(minecraft.screen instanceof VoiceSettingsScreen)) {
            return;
        }

        int x = 6;
        int y = graphics.guiHeight() - 46;
        float level = MicrophoneCapture.level();
        boolean transmitting = settings.microphoneOn && level >= settings.noiseGate;

        int colour = transmitting ? 0xFF3EDC81 : 0xFF7A8899;
        graphics.fill(x, y, x + 3, y + 10, colour);

        String label;
        if (settings.pushToTalk && !settings.microphoneOn) {
            label = "hold V to talk";
        } else if (transmitting) {
            label = "talking";
        } else {
            label = "mic open";
        }
        graphics.drawString(minecraft.font, label, x + 7, y + 1, colour, true);

        int barWidth = 40;
        float scaled = Math.min(1.0F, level / Math.max(0.05F, settings.noiseGate * 4.0F));
        graphics.fill(x + 7, y + 11, x + 7 + barWidth, y + 13, 0x60000000);
        graphics.fill(x + 7, y + 11, x + 7 + (int) (barWidth * scaled), y + 13, colour);

        // nearby speakers, only shown while they actually talk
        List<VoicePeersPacket.Peer> peers = VoiceClient.nearby();
        int line = 0;
        for (VoicePeersPacket.Peer peer : peers) {
            if (line >= 5) {
                break;
            }
            if (!peer.speaking()) {
                continue;
            }
            int peerColour = peer.level() > 0.12F ? 0xFF8BE9FD : 0xFFB0BEC5;
            graphics.drawString(minecraft.font, peer.name(), x, y - 12 - line * 10, peerColour, true);
            line++;
        }
    }
}
