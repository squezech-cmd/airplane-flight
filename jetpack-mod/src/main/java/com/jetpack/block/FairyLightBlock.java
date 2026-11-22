package com.jetpack.block;

import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.core.particles.ParticleTypes;
import net.minecraft.util.RandomSource;
import net.minecraft.world.item.context.BlockPlaceContext;
import net.minecraft.world.level.BlockGetter;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.HorizontalDirectionalBlock;
import net.minecraft.world.level.block.state.BlockState;
import net.minecraft.world.level.block.state.StateDefinition;
import net.minecraft.world.phys.shapes.CollisionContext;
import net.minecraft.world.phys.shapes.VoxelShape;

/** A string of fairy lights that can be pinned flat to a wall. */
public class FairyLightBlock extends HorizontalDirectionalBlock {
    private static final VoxelShape NORTH = Block.box(0.0D, 12.0D, 13.0D, 16.0D, 16.0D, 16.0D);
    private static final VoxelShape SOUTH = Block.box(0.0D, 12.0D, 0.0D, 16.0D, 16.0D, 3.0D);
    private static final VoxelShape WEST = Block.box(13.0D, 12.0D, 0.0D, 16.0D, 16.0D, 16.0D);
    private static final VoxelShape EAST = Block.box(0.0D, 12.0D, 0.0D, 3.0D, 16.0D, 16.0D);

    public FairyLightBlock(Properties properties) {
        super(properties);
        registerDefaultState(this.stateDefinition.any().setValue(FACING, Direction.NORTH));
    }

    @Override
    protected void createBlockStateDefinition(StateDefinition.Builder<Block, BlockState> builder) {
        builder.add(FACING);
    }

    @Override
    public BlockState getStateForPlacement(BlockPlaceContext context) {
        return defaultBlockState().setValue(FACING, context.getHorizontalDirection().getOpposite());
    }

    @Override
    public VoxelShape getShape(BlockState state, BlockGetter level, BlockPos pos, CollisionContext context) {
        return switch (state.getValue(FACING)) {
            case SOUTH -> SOUTH;
            case WEST -> WEST;
            case EAST -> EAST;
            default -> NORTH;
        };
    }

    @Override
    public void animateTick(BlockState state, Level level, BlockPos pos, RandomSource random) {
        // Little warm bulbs flicker along the string.
        if (random.nextInt(4) != 0) {
            return;
        }
        double x = pos.getX() + 0.1D + random.nextDouble() * 0.8D;
        double y = pos.getY() + 0.75D + random.nextDouble() * 0.2D;
        double z = pos.getZ() + 0.1D + random.nextDouble() * 0.8D;
        level.addParticle(ParticleTypes.FIREWORK, x, y, z, 0.0D, 0.0D, 0.0D);
    }
}
