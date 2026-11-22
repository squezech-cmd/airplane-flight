package com.jetpack.block;

import net.minecraft.core.BlockPos;
import net.minecraft.world.level.BlockGetter;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.state.BlockState;
import net.minecraft.world.phys.shapes.CollisionContext;
import net.minecraft.world.phys.shapes.Shapes;
import net.minecraft.world.phys.shapes.VoxelShape;

/**
 * A tall decorated spruce. The model reaches almost two blocks into the air,
 * but only the trunk below is solid, so you can walk through the branches.
 */
public class ChristmasTreeBlock extends Block {
    private static final VoxelShape TRUNK = Block.box(7.35D, 0.0D, 7.35D, 8.65D, 16.0D, 8.65D);
    private static final VoxelShape FOLIAGE = Block.box(0.6D, 1.6D, 0.6D, 15.4D, 16.0D, 15.4D);

    public ChristmasTreeBlock(Properties properties) {
        super(properties);
    }

    @Override
    public VoxelShape getShape(BlockState state, BlockGetter level, BlockPos pos, CollisionContext context) {
        return Shapes.or(TRUNK, FOLIAGE);
    }

    @Override
    public VoxelShape getCollisionShape(BlockState state, BlockGetter level, BlockPos pos, CollisionContext context) {
        return TRUNK;
    }
}
