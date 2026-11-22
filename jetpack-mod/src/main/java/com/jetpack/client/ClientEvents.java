package com.jetpack.client;

import com.jetpack.JetpackItem;
import com.jetpack.JetpackMod;
import com.jetpack.ThrustState;
import com.jetpack.network.NetworkHandler;
import com.jetpack.network.ThrustPacket;
import net.minecraft.client.Minecraft;
import net.minecraft.world.entity.EquipmentSlot;
import net.minecraft.world.item.ItemStack;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.event.TickEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;

/**
 * Reads the local player's jump key and mirrors it into {@link ThrustState}.
 *
 * <p>The client flag is what actually moves the player (see
 * {@link JetpackItem#inventoryTick}), the packet lets the server burn fuel and
 * spawn exhaust particles for everyone else to see.
 */
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
            ThrustState.clearClient();
            lastThrustState = false;
            return;
        }

        ItemStack chest = mc.player.getItemBySlot(EquipmentSlot.CHEST);
        boolean wearingJetpack = chest.getItem() instanceof JetpackItem;
        boolean wantThrust = wearingJetpack
                && mc.options.keyJump.isDown()
                && JetpackItem.getFuel(chest) > 0;

        ThrustState.setClientThrusting(mc.player, wantThrust);

        if (wantThrust != lastThrustState) {
            lastThrustState = wantThrust;
            NetworkHandler.INSTANCE.sendToServer(new ThrustPacket(wantThrust));
        }
    }
}
