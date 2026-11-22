package com.jetpack.event;

import com.jetpack.JetpackMod;
import net.minecraft.core.particles.ParticleTypes;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.sounds.SoundEvents;
import net.minecraft.sounds.SoundSource;
import net.minecraft.util.RandomSource;
import net.minecraft.world.effect.MobEffectInstance;
import net.minecraft.world.effect.MobEffects;
import net.minecraftforge.event.TickEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;

/**
 * A blizzard the whole world takes part in. Every few minutes the storm rolls
 * in: thick sideways snow, gusting wind sounds, snow drifting across the ground
 * and a gentle chill that makes the jetpack feel earned.
 */
@Mod.EventBusSubscriber(modid = JetpackMod.MOD_ID)
public final class WinterStorm {
    /** Storm cycle, in ticks: build-up, full storm, then calm. */
    private static final int CYCLE = 20 * 60 * 6;   // six minutes
    private static final int STORM_LENGTH = 20 * 45; // 45 seconds of full blizzard

    /** Local clock so the weather feels alive without saving state. */
    private static long stormStart = -1L;

    private WinterStorm() {
    }

    @SubscribeEvent
    public static void onLevelTick(TickEvent.LevelTickEvent event) {
        if (event.phase != TickEvent.Phase.END || !(event.level instanceof ServerLevel level)) {
            return;
        }
        if (level.players().isEmpty()) {
            return;
        }

        long now = level.getGameTime();
        if (stormStart < 0L) {
            stormStart = now + 20L * 30L; // first storm a bit sooner
        }
        if (now < stormStart) {
            return;
        }

        long elapsed = now - stormStart;
        boolean storming = elapsed < STORM_LENGTH;

        if (storming) {
            if (elapsed % 10 == 0) {
                blowSnow(level);
            }
            if (elapsed == 0 || elapsed == 20) {
                announce(level, true);
            }
            if (elapsed % 200 == 0) {
                level.playSound(null, 0, 64, 0, SoundEvents.WEATHER_RAIN_ABOVE,
                        SoundSource.WEATHER, 0.35F, 0.6F);
            }
        } else if (elapsed < STORM_LENGTH + 40) {
            if (elapsed == STORM_LENGTH) {
                announce(level, false);
            }
        } else {
            // Re-arm for the next cycle.
            stormStart = now + CYCLE;
        }
    }

    private static void blowSnow(ServerLevel level) {
        RandomSource random = level.getRandom();

        for (ServerPlayer player : level.players()) {
            if (player.isSpectator()) {
                continue;
            }

            // Thick sideways snow all around the player.
            for (int i = 0; i < 26; i++) {
                double dx = (random.nextDouble() - 0.5D) * 34.0D;
                double dz = (random.nextDouble() - 0.5D) * 34.0D;
                double dy = random.nextDouble() * 12.0D;
                level.sendParticles(ParticleTypes.SNOWFLAKE,
                        player.getX() + dx, player.getY() + dy, player.getZ() + dz,
                        1, 1.6D, -0.15D, 0.6D, 0.02D);
            }

            // Ground drift.
            for (int i = 0; i < 8; i++) {
                double dx = (random.nextDouble() - 0.5D) * 20.0D;
                double dz = (random.nextDouble() - 0.5D) * 20.0D;
                level.sendParticles(ParticleTypes.ITEM_SNOWBALL,
                        player.getX() + dx, player.getY() + 0.15D, player.getZ() + dz,
                        1, 0.9D, 0.0D, 0.35D, 0.01D);
            }

            // The chill: a short, mild slowdown that refreshes during the storm.
            if (player.tickCount % 100 == 0) {
                player.addEffect(new MobEffectInstance(MobEffects.MOVEMENT_SLOWDOWN, 120, 0));
            }
        }
    }

    private static void announce(ServerLevel level, boolean starting) {
        String message = starting
                ? "A winter blizzard rolls in..."
                : "The blizzard passes. The world is quiet again.";
        net.minecraft.network.chat.Component component = net.minecraft.network.chat.Component
                .literal(message)
                .withStyle(starting ? net.minecraft.ChatFormatting.AQUA : net.minecraft.ChatFormatting.GRAY);

        for (ServerPlayer player : level.players()) {
            player.sendSystemMessage(component);
        }

        net.minecraft.sounds.SoundEvent sound = starting
                ? SoundEvents.AMBIENT_CAVE.get()
                : SoundEvents.AMETHYST_BLOCK_CHIME;
        level.playSound(null, 0, 64, 0, sound, SoundSource.WEATHER, 0.5F, starting ? 0.7F : 1.4F);
    }

    /** True while the blizzard is at full strength (used by the client sparkle). */
    public static boolean isStorming(ServerLevel level) {
        return stormStart >= 0L && level.getGameTime() - stormStart < STORM_LENGTH;
    }
}
