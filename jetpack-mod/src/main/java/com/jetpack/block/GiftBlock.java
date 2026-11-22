package com.jetpack.block;

import net.minecraft.core.BlockPos;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.sounds.SoundEvents;
import net.minecraft.sounds.SoundSource;
import net.minecraft.util.RandomSource;
import net.minecraft.world.InteractionHand;
import net.minecraft.world.InteractionResult;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.level.BlockGetter;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.state.BlockState;
import net.minecraft.world.phys.BlockHitResult;
import net.minecraft.world.phys.shapes.CollisionContext;
import net.minecraft.world.phys.shapes.VoxelShape;

/**
 * A wrapped present. Right-click it and it pops open with a small loot surprise,
 * then it needs a moment to be rewrapped (the block is destroyed and the wrapper
 * drops, so you can place it again).
 */
public class GiftBlock extends Block {
    private static final VoxelShape SHAPE = Block.box(1.5D, 0.0D, 1.5D, 14.5D, 13.0D, 14.5D);

    private final int[] colours;

    public GiftBlock(Properties properties, int[] colours) {
        super(properties.randomTicks());
        this.colours = colours;
    }

    public int ribbonColour() {
        return colours[1];
    }

    @Override
    public VoxelShape getShape(BlockState state, BlockGetter level, BlockPos pos, CollisionContext context) {
        return SHAPE;
    }

    @Override
    public InteractionResult use(BlockState state, Level level, BlockPos pos, Player player,
                                 InteractionHand hand, BlockHitResult hit) {
        if (level.isClientSide) {
            return InteractionResult.SUCCESS;
        }

        ServerLevel serverLevel = (ServerLevel) level;
        level.playSound(null, pos, SoundEvents.AMETHYST_BLOCK_CHIME, SoundSource.BLOCKS, 1.0F, 1.4F);
        level.playSound(null, pos, SoundEvents.PLAYER_LEVELUP, SoundSource.BLOCKS, 0.4F, 1.6F);

        serverLevel.sendParticles(net.minecraft.core.particles.ParticleTypes.HAPPY_VILLAGER,
                pos.getX() + 0.5D, pos.getY() + 0.8D, pos.getZ() + 0.5D,
                18, 0.35D, 0.35D, 0.35D, 0.02D);

        if (!player.isCreative()) {
            ItemStack present = rollPresent(serverLevel.getRandom());
            popResource(serverLevel, pos, present);
        }

        level.removeBlock(pos, false);
        return InteractionResult.CONSUME;
    }

    /** Small weighted loot table, all seasonal and always harmless. */
    private static ItemStack rollPresent(RandomSource random) {
        int roll = random.nextInt(100);
        if (roll < 22) {
            return new ItemStack(Items.COOKIE, 2 + random.nextInt(4));
        } else if (roll < 40) {
            return new ItemStack(Items.SNOWBALL, 4 + random.nextInt(9));
        } else if (roll < 55) {
            return new ItemStack(Items.SWEET_BERRIES, 2 + random.nextInt(4));
        } else if (roll < 68) {
            return new ItemStack(Items.GOLD_NUGGET, 1 + random.nextInt(4));
        } else if (roll < 78) {
            return new ItemStack(Items.AMETHYST_SHARD, 1 + random.nextInt(3));
        } else if (roll < 88) {
            return new ItemStack(Items.FIREWORK_ROCKET, 2 + random.nextInt(4));
        } else if (roll < 95) {
            return new ItemStack(Items.EMERALD);
        } else {
            return new ItemStack(Items.DIAMOND);
        }
    }

    /** Snow settles softly on top of presents. */
    @Override
    public void randomTick(BlockState state, ServerLevel level, BlockPos pos, RandomSource random) {
        if (level.getBlockState(pos.above()).isAir() && random.nextInt(6) == 0) {
            level.setBlockAndUpdate(pos.above(), net.minecraft.world.level.block.Blocks.SNOW.defaultBlockState());
        }
    }

    @Override
    public boolean isRandomlyTicking(BlockState state) {
        return true;
    }

    @Override
    public void animateTick(BlockState state, Level level, BlockPos pos, RandomSource random) {
        // A faint sparkle so presents catch the eye at night.
        if (random.nextInt(12) == 0) {
            level.addParticle(net.minecraft.core.particles.ParticleTypes.FIREWORK,
                    pos.getX() + 0.15D + random.nextDouble() * 0.7D,
                    pos.getY() + 0.9D + random.nextDouble() * 0.3D,
                    pos.getZ() + 0.15D + random.nextDouble() * 0.7D,
                    0.0D, 0.01D, 0.0D);
        }
    }
}
