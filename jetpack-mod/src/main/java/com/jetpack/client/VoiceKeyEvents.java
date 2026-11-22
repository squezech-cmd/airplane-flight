package com.jetpack.client;

import com.jetpack.JetpackMod;
import com.jetpack.voice.MicrophoneCapture;
import com.jetpack.voice.VoiceClient;
import com.jetpack.voice.VoiceSettings;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.screens.ChatScreen;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.event.TickEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;
import org.lwjgl.glfw.GLFW;

/**
 * Push to talk, the settings hotkey and the per-tick voice update.
 *
 * <pre>
 * V            hold to talk while playing (push to talk mode),
 *              or open the settings screen when in a GUI
 * Left Alt     open the voice settings screen at any time
 * </pre>
 *
 * <p>Key state is read straight from GLFW rather than a KeyMapping, so the mod
 * never edits the vanilla controls file.
 */
@Mod.EventBusSubscriber(modid = JetpackMod.MOD_ID, value = Dist.CLIENT)
public final class VoiceKeyEvents {
    private static boolean talking;
    private static boolean altHeld;

    private VoiceKeyEvents() {
    }

    @SubscribeEvent
    public static void onClientTick(TickEvent.ClientTickEvent event) {
        if (event.phase != TickEvent.Phase.END) {
            return;
        }
        Minecraft minecraft = Minecraft.getInstance();
        if (minecraft.player == null || minecraft.level == null) {
            return;
        }

        VoiceClient.tick();

        VoiceSettings settings = VoiceSettings.get();
        if (!settings.enabled) {
            talking = false;
            return;
        }

        long window = minecraft.getWindow().getWindow();
        boolean inGui = minecraft.screen != null;
        boolean typing = minecraft.screen instanceof ChatScreen;

        // ---- settings screen -------------------------------------------------
        boolean alt = GLFW.glfwGetKey(window, GLFW.GLFW_KEY_LEFT_ALT) == GLFW.GLFW_PRESS
                || GLFW.glfwGetKey(window, GLFW.GLFW_KEY_RIGHT_ALT) == GLFW.GLFW_PRESS;
        if (alt && !altHeld && !inGui) {
            minecraft.setScreen(new VoiceSettingsScreen(null));
        }
        altHeld = alt;

        // ---- push to talk ----------------------------------------------------
        boolean pushKey = GLFW.glfwGetKey(window, settings.pushToTalkKey) == GLFW.GLFW_PRESS;
        boolean want;
        if (!settings.pushToTalk) {
            want = true;                       // open microphone
        } else if (inGui) {
            want = talking;                    // keep holding while a menu is open
        } else {
            want = pushKey;                    // hold the key to talk
        }
        if (typing) {
            want = false;
        }

        if (want != talking) {
            talking = want;
            settings.microphoneOn = want;
            VoiceClient.sendSettings();
        }

        if (!MicrophoneCapture.isRunning()) {
            MicrophoneCapture.start();
        }
    }
}
