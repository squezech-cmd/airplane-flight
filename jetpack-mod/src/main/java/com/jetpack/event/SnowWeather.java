package com.jetpack.event;

import com.jetpack.JetpackMod;
import net.minecraft.core.BlockPos;
import net.minecraft.core.particles.ParticleTypes;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.sounds.SoundEvents;
import net.minecraft.sounds.SoundSource;
import net.minecraft.util.RandomSource;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.level.block.Blocks;
import net.minecraft.world.level.block.SnowLayerBlock;
import net.minecraft.world.level.block.state.BlockState;
import net.minecraftforge.event.TickEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;

/**
 * Turns rain into snow: whenever a world rains, any rain around a player is
 * replaced by snowfall, and snow gradually piles up into layers.
 */
@Mod.EventBusSubscriber(modid = JetpackMod.MOD_ID)
public final class SnowWeather {
    /** How high snow is allowed to pile up. */
    private static final int MAX_LAYERS = 4;

    private SnowWeather() {
    }

    @SubscribeEvent
    public static void onWorldTick(TickEvent.LevelTickEvent event) {
        if (event.phase != TickEvent.Phase.END || !(event.level instanceof ServerLevel level)) {
            return;
        }
        if (!level.isRaining() || level.getGameTime() % 20 != 0) {
            return;
        }

        RandomSource random = level.getRandom();
        // Only a handful of players are handled per second to keep this cheap.
        for (Player player : level.players()) {
            if (player.isSpectator()) {
                continue;
            }
            buildSnowdrifts(level, player.blockPosition(), random);
        }
    }

    /** Adds a fresh layer of snow to ground that is already snowy, or starts one. */
    private static void buildSnowdrifts(ServerLevel level, BlockPos centre, RandomSource random) {
        for (int attempt = 0; attempt < 4; attempt++) {
            BlockPos target = centre.offset(random.nextInt(15) - 7, 0, random.nextInt(15) - 7);
            BlockPos ground = findSurface(level, target);
            if (ground == null) {
                continue;
            }
            if (!level.getBiome(ground).value().coldEnoughToSnow(ground)) {
                continue;
            }

            BlockState state = level.getBlockState(ground);
            if (state.is(Blocks.SNOW)) {
                int layers = state.getValue(SnowLayerBlock.LAYERS);
                if (layers < MAX_LAYERS && random.nextInt(3) == 0) {
                    level.setBlockAndUpdate(ground, state.setValue(SnowLayerBlock.LAYERS, layers + 1));
                }
            } else if (state.isAir() && level.getBlockState(ground.below()).isSolidRender(level, ground.below())) {
                BlockState below = level.getBlockState(ground.below());
                if (below.is(Blocks.SNOW_BLOCK) || below.is(Blocks.ICE) || below.is(Blocks.PACKED_ICE)) {
                    continue;
                }
                level.setBlockAndUpdate(ground, Blocks.SNOW.defaultBlockState());
                level.sendParticles(ParticleTypes.SNOWFLAKE,
                        ground.getX() + 0.5D, ground.getY() + 0.2D, ground.getZ() + 0.5D,
                        2, 0.25D, 0.02D, 0.25D, 0.01D);
            }

            if (random.nextInt(20) == 0) {
                level.playSound(null, ground, SoundEvents.SNOW_BREAK, SoundSource.WEATHER, 0.15F, 1.4F);
            }
        }
    }

    private static BlockPos findSurface(ServerLevel level, BlockPos start) {
        BlockPos.MutableBlockPos cursor = start.mutable();
        for (int i = 0; i < 8; i++) {
            BlockState state = level.getBlockState(cursor);
            if (!state.isAir() && state.isSolidRender(level, cursor)) {
                return cursor.immutable();
            }
            cursor.move(0, -1, 0);
            if (cursor.getY() < level.getMinBuildHeight()) {
                return null;
            }
        }
        return null;
    }
}
