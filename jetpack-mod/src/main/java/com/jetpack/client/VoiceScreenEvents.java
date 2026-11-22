package com.jetpack.client;

import com.jetpack.JetpackMod;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.PauseScreen;
import net.minecraft.network.chat.Component;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.client.event.RenderGuiEvent;
import net.minecraftforge.client.event.ScreenEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;

/**
 * Draws the voice HUD and adds a "Voice chat" button to the pause menu, which
 * is the discoverable way into the settings without knowing a hotkey.
 */
@Mod.EventBusSubscriber(modid = JetpackMod.MOD_ID, value = Dist.CLIENT)
public final class VoiceScreenEvents {
    private VoiceScreenEvents() {
    }

    @SubscribeEvent
    public static void onRenderGui(RenderGuiEvent.Post event) {
        VoiceHud.render(event.getGuiGraphics(), event.getPartialTick());
    }

    @SubscribeEvent
    public static void onScreenInit(ScreenEvent.Init.Post event) {
        if (!(event.getScreen() instanceof PauseScreen pauseScreen)) {
            return;
        }
        // The pause screen rebuilds its widget list from scratch every time it
        // opens, so adding the button here is safe and idempotent.
        int x = pauseScreen.width / 2 - 100;
        int y = pauseScreen.height / 4 + 152;
        event.addListener(Button.builder(Component.literal("Voice chat"),
                        button -> net.minecraft.client.Minecraft.getInstance()
                                .setScreen(new VoiceSettingsScreen(pauseScreen)))
                .bounds(x, y, 200, 20)
                .build());
    }
}
