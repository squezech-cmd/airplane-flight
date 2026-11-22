package com.jetpack.client;

import com.jetpack.JetpackMod;
import net.minecraft.client.Minecraft;
import net.minecraft.core.particles.ParticleTypes;
import net.minecraft.world.level.Level;
import net.minecraftforge.api.distmarker.Dist;
import net.minecraftforge.event.TickEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;

/**
 * Client-side winter mood: drifting sparkles near the player, frosty breath in
 * cold places, and the occasional shooting star at night.
 */
@Mod.EventBusSubscriber(modid = JetpackMod.MOD_ID, value = Dist.CLIENT)
public final class WinterAmbience {
    private WinterAmbience() {
    }

    @SubscribeEvent
    public static void onClientTick(TickEvent.ClientTickEvent event) {
        if (event.phase != TickEvent.Phase.END) {
            return;
        }
        Minecraft mc = Minecraft.getInstance();
        if (mc.player == null || mc.level == null || mc.isPaused()) {
            return;
        }

        Level level = mc.level;
        int tick = mc.player.tickCount;

        // Sparkles hovering just around the player, like frost in the air.
        if (tick % 7 == 0) {
            for (int i = 0; i < 2; i++) {
                double dx = (level.random.nextDouble() - 0.5D) * 7.0D;
                double dy = level.random.nextDouble() * 3.5D;
                double dz = (level.random.nextDouble() - 0.5D) * 7.0D;
                level.addParticle(ParticleTypes.END_ROD,
                        mc.player.getX() + dx, mc.player.getY() + dy, mc.player.getZ() + dz,
                        0.0D, 0.004D, 0.0D);
            }
        }

        // Frosty breath puffs when it is cold or snowing.
        if (level.isRaining() && tick % 22 == 0) {
            double lookX = -Math.sin(Math.toRadians(mc.player.getYRot())) * 0.35D;
            double lookZ = Math.cos(Math.toRadians(mc.player.getYRot())) * 0.35D;
            level.addParticle(ParticleTypes.CLOUD,
                    mc.player.getX() + lookX, mc.player.getY() + 1.55D, mc.player.getZ() + lookZ,
                    lookX * 0.03D, 0.01D, lookZ * 0.03D);
        }

        // A rare shooting star on clear nights.
        if (!level.isRaining() && level.getDayTime() % 24000L > 14000L
                && level.getDayTime() % 24000L < 18000L && level.random.nextInt(900) == 0) {
            double x = mc.player.getX() + (level.random.nextDouble() - 0.5D) * 90.0D;
            double z = mc.player.getZ() + (level.random.nextDouble() - 0.5D) * 90.0D;
            for (int i = 0; i < 22; i++) {
                level.addParticle(ParticleTypes.END_ROD,
                        x + i * 0.55D, mc.player.getY() + 45.0D - i * 0.42D, z + i * 0.3D,
                        0.0D, 0.0D, 0.0D);
            }
        }
    }
}
