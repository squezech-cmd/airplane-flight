package com.jetpack.event;

import com.jetpack.JetpackMod;
import com.jetpack.registry.ModRegistry;
import net.minecraft.core.BlockPos;
import net.minecraft.core.particles.ParticleTypes;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.sounds.SoundEvents;
import net.minecraft.sounds.SoundSource;
import net.minecraft.tags.BiomeTags;
import net.minecraft.util.RandomSource;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.block.Blocks;
import net.minecraft.world.level.block.state.BlockState;
import net.minecraftforge.event.TickEvent;
import net.minecraftforge.event.entity.player.PlayerEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;

/**
 * The "very merry" part: gentle snowfall around every player, occasional
 * ground frost, a welcome gift on first join, and the ambient jingle bells.
 */
@Mod.EventBusSubscriber(modid = JetpackMod.MOD_ID)
public final class WinterEvents {
    /** Blocks are only frosted below this height so mountains stay untouched. */
    private static final int FROST_MAX_Y = 128;

    private WinterEvents() {
    }

    @SubscribeEvent
    public static void onPlayerTick(TickEvent.PlayerTickEvent event) {
        if (event.phase != TickEvent.Phase.END) {
            return;
        }
        Player player = event.player;
        if (!(player.level() instanceof ServerLevel level)) {
            return;
        }
        if (player.isSpectator()) {
            return;
        }

        // Snowfall around the player: dense enough to feel wintry, light enough
        // that it never gets in the way.
        if (player.tickCount % 2 == 0) {
            RandomSource random = level.getRandom();
            double radius = 9.0D;
            for (int i = 0; i < 3; i++) {
                double dx = (random.nextDouble() - 0.5D) * radius * 2.0D;
                double dz = (random.nextDouble() - 0.5D) * radius * 2.0D;
                double dy = 5.0D + random.nextDouble() * 4.0D;
                level.sendParticles(ParticleTypes.SNOWFLAKE,
                        player.getX() + dx, player.getY() + dy, player.getZ() + dz,
                        1, 0.0D, -0.03D, 0.0D, 0.005D);
            }
        }

        // Ground frost: slowly paints the world white, only thin layers, never
        // inside builds (it replaces air only).
        if (player.tickCount % 20 == 0 && level.getRandom().nextInt(3) == 0) {
            frostAround(level, player.blockPosition());
        }
    }

    private static void frostAround(ServerLevel level, BlockPos centre) {
        RandomSource random = level.getRandom();

        for (int attempt = 0; attempt < 6; attempt++) {
            int dx = random.nextInt(11) - 5;
            int dz = random.nextInt(11) - 5;
            BlockPos base = centre.offset(dx, 0, dz);

            if (base.getY() > FROST_MAX_Y) {
                continue;
            }

            BlockPos ground = findGround(level, base);
            if (ground == null) {
                continue;
            }

            BlockState below = level.getBlockState(ground);
            BlockPos target = ground.above();

            if (!level.getBlockState(target).isAir()) {
                continue;
            }
            if (below.is(Blocks.SNOW) || below.is(Blocks.SNOW_BLOCK)
                    || below.is(Blocks.ICE) || below.is(Blocks.PACKED_ICE)) {
                continue;
            }
            if (!below.isSolidRender(level, ground)) {
                continue;
            }
            if (level.getBiome(ground).is(BiomeTags.IS_NETHER) || level.getBiome(ground).is(BiomeTags.IS_END)) {
                continue;
            }
            if (below.getBlock() == Blocks.LAVA || below.getBlock() == Blocks.WATER) {
                continue;
            }

            level.setBlockAndUpdate(target, Blocks.SNOW.defaultBlockState());
            if (random.nextInt(4) == 0) {
                level.sendParticles(ParticleTypes.SNOWFLAKE,
                        target.getX() + 0.5D, target.getY() + 0.2D, target.getZ() + 0.5D,
                        2, 0.2D, 0.05D, 0.2D, 0.01D);
            }
        }
    }

    /** Finds the first solid block below the given position with air above it. */
    private static BlockPos findGround(Level level, BlockPos start) {
        BlockPos.MutableBlockPos cursor = start.mutable();
        for (int i = 0; i < 6; i++) {
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

    /** Everyone gets a small welcome pack the first time they join. */
    @SubscribeEvent
    public static void onPlayerJoin(PlayerEvent.PlayerLoggedInEvent event) {
        if (!(event.getEntity() instanceof ServerPlayer player)) {
            return;
        }

        String key = "jetpack:welcome_gift";
        if (player.getPersistentData().getBoolean(key)) {
            return;
        }
        player.getPersistentData().putBoolean(key, true);

        player.getInventory().add(new net.minecraft.world.item.ItemStack(ModRegistry.CHRISTMAS_STAR.get(), 2));
        player.getInventory().add(new net.minecraft.world.item.ItemStack(ModRegistry.HOT_COCOA.get()));
        player.getInventory().add(new net.minecraft.world.item.ItemStack(ModRegistry.SNOW_GLOBE.get()));

        player.level().playSound(null, player.blockPosition(), SoundEvents.PLAYER_LEVELUP,
                SoundSource.PLAYERS, 0.7F, 1.5F);
        player.sendSystemMessage(net.minecraft.network.chat.Component
                .literal("Merry Christmas! A small gift appeared in your inventory.")
                .withStyle(net.minecraft.ChatFormatting.RED));
    }
}
