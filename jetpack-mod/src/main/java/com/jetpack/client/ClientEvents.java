package com.jetpack.client;

import com.jetpack.JetpackItem;
import com.jetpack.JetpackMod;
import com.jetpack.network.NetworkHandler;
import com.jetpack.network.ThrustPacket;
import net.minecraft.client.Minecraft;
import net.minecraft.world.entity.EquipmentSlot;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.event.TickEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;

@Mod.EventBusSubscriber(modid = JetpackMod.MOD_ID, value = Dist.CLIENT)
public final class ClientEvents {
    private static boolean lastThrustState = false;

    private ClientEvents() {
    }

    @SubscribeEvent
    public static void onClientTick(TickEvent.ClientTickEvent event) {
        if (event.phase != TickEvent.Phase.END) {
            return;
        }
        Minecraft mc = Minecraft.getInstance();
        if (mc.player == null || mc.level == null) {
            return;
        }
        boolean wearingJetpack = mc.player.getItemBySlot(EquipmentSlot.CHEST).getItem() instanceof JetpackItem;
        boolean wantThrust = wearingJetpack && mc.options.keyJump.isDown();
        if (wantThrust != lastThrustState) {
            lastThrustState = wantThrust;
            NetworkHandler.INSTANCE.sendToServer(new ThrustPacket(wantThrust));
        }
    }
}
