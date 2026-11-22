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
import net.minecraft.world.phys.shapes.BooleanOp;
import net.minecraft.world.phys.shapes.CollisionContext;
import net.minecraft.world.phys.shapes.Shapes;
import net.minecraft.world.phys.shapes.VoxelShape;

/**
 * A placeable decoration. Each one has its own bounding box, an optional glow
 * and a particle flavour so it feels alive on the shelf.
 */
public class DecorBlock extends HorizontalDirectionalBlock {
    public enum Flavour {
        NONE,
        SPARKLE,   // tiny stars drifting up
        DUST,      // slow warm motes
        GLITTER    // snow sparkle
    }

    private final VoxelShape shape;
    private final Flavour flavour;

    public DecorBlock(Properties properties, VoxelShape shape, Flavour flavour) {
        super(properties);
        this.shape = shape;
        this.flavour = flavour;
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
        return this.shape;
    }

    @Override
    public VoxelShape getCollisionShape(BlockState state, BlockGetter level, BlockPos pos, CollisionContext context) {
        return this.shape;
    }

    /** Convenience builder used by the registry. */
    public static VoxelShape box(double x0, double y0, double z0, double x1, double y1, double z1) {
        return Shapes.box(x0 / 16.0D, y0 / 16.0D, z0 / 16.0D, x1 / 16.0D, y1 / 16.0D, z1 / 16.0D);
    }

    public static VoxelShape union(VoxelShape a, VoxelShape b) {
        return Shapes.join(a, b, BooleanOp.OR);
    }

    @Override
    public void animateTick(BlockState state, Level level, BlockPos pos, RandomSource random) {
        switch (this.flavour) {
            case SPARKLE -> {
                if (random.nextInt(10) == 0) {
                    level.addParticle(ParticleTypes.FIREWORK,
                            pos.getX() + 0.2D + random.nextDouble() * 0.6D,
                            pos.getY() + 0.7D + random.nextDouble() * 0.3D,
                            pos.getZ() + 0.2D + random.nextDouble() * 0.6D,
                            0.0D, 0.005D, 0.0D);
                }
            }
            case DUST -> {
                if (random.nextInt(14) == 0) {
                    level.addParticle(ParticleTypes.WAX_ON,
                            pos.getX() + 0.3D + random.nextDouble() * 0.4D,
                            pos.getY() + 0.5D + random.nextDouble() * 0.4D,
                            pos.getZ() + 0.3D + random.nextDouble() * 0.4D,
                            0.0D, 0.004D, 0.0D);
                }
            }
            case GLITTER -> {
                if (random.nextInt(12) == 0) {
                    level.addParticle(ParticleTypes.SNOWFLAKE,
                            pos.getX() + 0.2D + random.nextDouble() * 0.6D,
                            pos.getY() + 0.6D,
                            pos.getZ() + 0.2D + random.nextDouble() * 0.6D,
                            0.0D, -0.01D, 0.0D);
                }
            }
            default -> {
            }
        }
    }
}
