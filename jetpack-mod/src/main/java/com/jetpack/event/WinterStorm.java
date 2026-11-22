package com.jetpack.event;

import com.jetpack.JetpackMod;
import net.minecraft.core.particles.ParticleTypes;
import net.minecraft.network.chat.Component;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.sounds.SoundEvent;
import net.minecraft.sounds.SoundEvents;
import net.minecraft.sounds.SoundSource;
import net.minecraft.util.RandomSource;
import net.minecraft.world.effect.MobEffectInstance;
import net.minecraft.world.effect.MobEffects;
import net.minecraftforge.event.TickEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;

import java.util.HashMap;
import java.util.Map;
import java.util.UUID;

/**
 * Blizzards: thick sideways snow, gusting wind, ground drift and a chill.
 *
 * <p>They roll in on their own every few minutes, and the
 * {@code /jetpack blizzard} command can start or stop one immediately.
 */
@Mod.EventBusSubscriber(modid = JetpackMod.MOD_ID)
public final class WinterStorm {
    private static final int CYCLE = 20 * 60 * 6;     // six minutes between storms
    private static final int STORM_LENGTH = 20 * 45;  // 45 second blizzard
    private static final int FIRST_DELAY = 20 * 30;   // first one after half a minute

    /** Per-level clock so a forced storm does not leak between dimensions. */
    private static final Map<UUID, Long> LEVEL_START = new HashMap<>();

    private WinterStorm() {
    }

    // ------------------------------------------------------------------ control

    /** Starts a storm in this level right now. */
    public static void trigger(ServerLevel level, int seconds) {
        int ticks = Math.max(20, seconds * 20);
        WinterConfig.forcedStormLength = ticks;
        WinterConfig.forcedStormStart = level.getGameTime();
        LEVEL_START.put(level.dimension().location().toString().isEmpty()
                ? UUID.nameUUIDFromBytes(new byte[0])
                : UUID.nameUUIDFromBytes(level.dimension().location().toString().getBytes()), level.getGameTime());
        announce(level, true);
    }

    /** Stops whatever storm is running. */
    public static void stop(ServerLevel level) {
        WinterConfig.forcedStormStart = -1L;
        LEVEL_START.remove(key(level));
        announce(level, false);
    }

    public static boolean isStorming(ServerLevel level) {
        long start = currentStart(level);
        if (start < 0L) {
            return false;
        }
        return level.getGameTime() - start < activeLength();
    }

    private static long activeLength() {
        return WinterConfig.forcedStormStart >= 0L ? WinterConfig.forcedStormLength : STORM_LENGTH;
    }

    private static UUID key(ServerLevel level) {
        return UUID.nameUUIDFromBytes(level.dimension().location().toString().getBytes());
    }

    private static long currentStart(ServerLevel level) {
        if (WinterConfig.forcedStormStart >= 0L) {
            return WinterConfig.forcedStormStart;
        }
        return LEVEL_START.getOrDefault(key(level), -1L);
    }

    // --------------------------------------------------------------------- tick

    @SubscribeEvent
    public static void onLevelTick(TickEvent.LevelTickEvent event) {
        if (event.phase != TickEvent.Phase.END || !(event.level instanceof ServerLevel level)) {
            return;
        }
        if (level.players().isEmpty()) {
            return;
        }

        long now = level.getGameTime();
        UUID key = key(level);

        // Schedule the next automatic storm if none is running.
        if (WinterConfig.forcedStormStart < 0L && !LEVEL_START.containsKey(key)) {
            if (WinterConfig.autoStorms) {
                LEVEL_START.put(key, now + FIRST_DELAY);
            }
            return;
        }

        long start = currentStart(level);
        if (start < 0L) {
            return;
        }
        if (now < start) {
            return;
        }

        long elapsed = now - start;
        long length = activeLength();

        if (elapsed < length) {
            if (elapsed % 10 == 0) {
                blowSnow(level);
            }
            if (elapsed % 200 == 0) {
                level.playSound(null, 0, 64, 0, SoundEvents.WEATHER_RAIN_ABOVE,
                        SoundSource.WEATHER, 0.35F, 0.6F);
            }
        } else {
            // Storm over: clear state and schedule the next automatic one.
            WinterConfig.forcedStormStart = -1L;
            if (WinterConfig.autoStorms) {
                LEVEL_START.put(key, now + CYCLE);
            } else {
                LEVEL_START.remove(key);
            }
        }
    }

    private static void blowSnow(ServerLevel level) {
        RandomSource random = level.getRandom();

        for (ServerPlayer player : level.players()) {
            if (player.isSpectator()) {
                continue;
            }

            for (int i = 0; i < 26; i++) {
                double dx = (random.nextDouble() - 0.5D) * 34.0D;
                double dz = (random.nextDouble() - 0.5D) * 34.0D;
                double dy = random.nextDouble() * 12.0D;
                level.sendParticles(ParticleTypes.SNOWFLAKE,
                        player.getX() + dx, player.getY() + dy, player.getZ() + dz,
                        1, 1.6D, -0.15D, 0.6D, 0.02D);
            }

            for (int i = 0; i < 8; i++) {
                double dx = (random.nextDouble() - 0.5D) * 20.0D;
                double dz = (random.nextDouble() - 0.5D) * 20.0D;
                level.sendParticles(ParticleTypes.ITEM_SNOWBALL,
                        player.getX() + dx, player.getY() + 0.15D, player.getZ() + dz,
                        1, 0.9D, 0.0D, 0.35D, 0.01D);
            }

            if (player.tickCount % 100 == 0 && !player.isCreative()) {
                player.addEffect(new MobEffectInstance(MobEffects.MOVEMENT_SLOWDOWN, 120, 0, false, false, true));
            }
        }
    }

    private static void announce(ServerLevel level, boolean starting) {
        String message = starting
                ? "A winter blizzard rolls in..."
                : "The blizzard passes. The world is quiet again.";
        Component component = Component.literal(message)
                .withStyle(starting ? net.minecraft.ChatFormatting.AQUA : net.minecraft.ChatFormatting.GRAY);

        for (ServerPlayer player : level.players()) {
            player.sendSystemMessage(component);
        }

        SoundEvent sound = starting ? SoundEvents.AMBIENT_CAVE.get() : SoundEvents.AMETHYST_BLOCK_CHIME;
        level.playSound(null, 0, 64, 0, sound, SoundSource.WEATHER, 0.5F, starting ? 0.7F : 1.4F);
    }
}
