package com.jetpack.block;

import net.minecraft.core.BlockPos;
import net.minecraft.core.particles.ParticleTypes;
import net.minecraft.util.RandomSource;
import net.minecraft.world.level.BlockGetter;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.state.BlockState;
import net.minecraft.world.phys.shapes.CollisionContext;
import net.minecraft.world.phys.shapes.VoxelShape;

/** A holly bush dotted with red berries. */
public class HollyBushBlock extends Block {
    private static final VoxelShape SHAPE = Block.box(2.0D, 0.0D, 2.0D, 14.0D, 13.0D, 14.0D);

    public HollyBushBlock(Properties properties) {
        super(properties);
    }

    @Override
    public VoxelShape getShape(BlockState state, BlockGetter level, BlockPos pos, CollisionContext context) {
        return SHAPE;
    }

    @Override
    public void animateTick(BlockState state, Level level, BlockPos pos, RandomSource random) {
        if (random.nextInt(30) == 0) {
            level.addParticle(ParticleTypes.HAPPY_VILLAGER,
                    pos.getX() + 0.2D + random.nextDouble() * 0.6D,
                    pos.getY() + 0.4D + random.nextDouble() * 0.4D,
                    pos.getZ() + 0.2D + random.nextDouble() * 0.6D,
                    0.0D, 0.0D, 0.0D);
        }
    }
}
