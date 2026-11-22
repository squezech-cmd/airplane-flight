package com.jetpack.block;

import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.core.particles.ParticleTypes;
import net.minecraft.util.RandomSource;
import net.minecraft.world.entity.LivingEntity;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.context.BlockPlaceContext;
import net.minecraft.world.level.BlockGetter;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.LevelAccessor;
import net.minecraft.world.level.LevelReader;
import net.minecraft.world.level.block.Block;
import net.minecraft.world.level.block.Blocks;
import net.minecraft.world.level.block.entity.BlockEntity;
import net.minecraft.world.level.block.state.BlockState;
import net.minecraft.world.level.block.state.StateDefinition;
import net.minecraft.world.level.block.state.properties.BlockStateProperties;
import net.minecraft.world.level.block.state.properties.DoubleBlockHalf;
import net.minecraft.world.level.block.state.properties.EnumProperty;
import net.minecraft.world.level.material.Fluids;
import net.minecraft.world.phys.shapes.CollisionContext;
import net.minecraft.world.phys.shapes.Shapes;
import net.minecraft.world.phys.shapes.VoxelShape;
import org.jetbrains.annotations.Nullable;

/**
 * The showpiece tree: two blocks tall, absolutely loaded with lights, garlands
 * and baubles, topped with a glowing star. Only the trunk is solid, so you can
 * walk through the branches. The upper half carries the top tiers and the star.
 */
public class BigChristmasTreeBlock extends Block {
    public static final EnumProperty<DoubleBlockHalf> HALF = BlockStateProperties.DOUBLE_BLOCK_HALF;

    private static final VoxelShape TRUNK = Block.box(6.0D, 0.0D, 6.0D, 10.0D, 16.0D, 10.0D);
    private static final VoxelShape FOLIAGE = Shapes.or(
            Block.box(1.0D, 1.0D, 1.0D, 15.0D, 6.0D, 15.0D),
            Block.box(2.5D, 5.0D, 2.5D, 13.5D, 11.0D, 13.5D),
            Block.box(4.0D, 10.0D, 4.0D, 12.0D, 16.0D, 12.0D));
    private static final VoxelShape UPPER_FOLIAGE = Shapes.or(
            Block.box(3.0D, 0.0D, 3.0D, 13.0D, 7.0D, 13.0D),
            Block.box(4.4D, 5.0D, 4.4D, 11.6D, 11.0D, 11.6D));

    public BigChristmasTreeBlock(Properties properties) {
        super(properties);
        registerDefaultState(this.stateDefinition.any().setValue(HALF, DoubleBlockHalf.LOWER));
    }

    @Override
    protected void createBlockStateDefinition(StateDefinition.Builder<Block, BlockState> builder) {
        builder.add(HALF);
    }

    @Override
    public VoxelShape getShape(BlockState state, BlockGetter level, BlockPos pos, CollisionContext context) {
        if (state.getValue(HALF) == DoubleBlockHalf.UPPER) {
            return UPPER_FOLIAGE;
        }
        return Shapes.or(TRUNK, FOLIAGE);
    }

    @Override
    public VoxelShape getCollisionShape(BlockState state, BlockGetter level, BlockPos pos, CollisionContext context) {
        return state.getValue(HALF) == DoubleBlockHalf.UPPER ? Shapes.empty() : TRUNK;
    }

    @Nullable
    @Override
    public BlockState getStateForPlacement(BlockPlaceContext context) {
        BlockPos pos = context.getClickedPos();
        Level level = context.getLevel();
        if (pos.getY() < level.getMaxBuildHeight() - 1 && level.getBlockState(pos.above()).canBeReplaced(context)) {
            return defaultBlockState().setValue(HALF, DoubleBlockHalf.LOWER);
        }
        return null;
    }

    @Override
    public void setPlacedBy(Level level, BlockPos pos, BlockState state, @Nullable LivingEntity placer, ItemStack stack) {
        level.setBlockAndUpdate(pos.above(), defaultBlockState().setValue(HALF, DoubleBlockHalf.UPPER));
    }

    @Override
    public boolean canSurvive(BlockState state, LevelReader level, BlockPos pos) {
        if (state.getValue(HALF) == DoubleBlockHalf.UPPER) {
            BlockState below = level.getBlockState(pos.below());
            return below.is(this) && below.getValue(HALF) == DoubleBlockHalf.LOWER;
        }
        return true;
    }

    @Override
    public BlockState updateShape(BlockState state, Direction direction, BlockState neighbourState,
                                  LevelAccessor level, BlockPos pos, BlockPos neighbourPos) {
        if (state.getValue(HALF) == DoubleBlockHalf.UPPER && direction == Direction.DOWN
                && !neighbourState.is(this)) {
            return Blocks.AIR.defaultBlockState();
        }
        return super.updateShape(state, direction, neighbourState, level, pos, neighbourPos);
    }

    @Override
    public void playerWillDestroy(Level level, BlockPos pos, BlockState state, Player player) {
        if (!level.isClientSide && state.getValue(HALF) == DoubleBlockHalf.LOWER) {
            BlockPos above = pos.above();
            BlockState upper = level.getBlockState(above);
            if (upper.is(this) && upper.getValue(HALF) == DoubleBlockHalf.UPPER) {
                level.setBlock(above, Blocks.AIR.defaultBlockState(), 35);
                level.levelEvent(player, 2001, above, Block.getId(upper));
            }
        }
        super.playerWillDestroy(level, pos, state, player);
    }

    @Override
    public void playerDestroy(Level level, Player player, BlockPos pos, BlockState state,
                              @Nullable BlockEntity blockEntity, ItemStack tool) {
        super.playerDestroy(level, player, pos, state, blockEntity, tool);
    }

    @Override
    public void animateTick(BlockState state, Level level, BlockPos pos, RandomSource random) {
        // Warm twinkle across the foliage.
        if (random.nextInt(7) == 0) {
            double x = pos.getX() + 0.15D + random.nextDouble() * 0.7D;
            double z = pos.getZ() + 0.15D + random.nextDouble() * 0.7D;
            double y = pos.getY() + 0.2D + random.nextDouble() * 0.8D;
            level.addParticle(ParticleTypes.FIREWORK, x, y, z, 0.0D, 0.004D, 0.0D);
        }
        // Soft falling glitter.
        if (random.nextInt(12) == 0) {
            level.addParticle(ParticleTypes.SNOWFLAKE,
                    pos.getX() + random.nextDouble(), pos.getY() + 0.85D,
                    pos.getZ() + random.nextDouble(),
                    0.0D, -0.02D, 0.0D);
        }
        // The upper half gets the star sparkle.
        if (state.getValue(HALF) == DoubleBlockHalf.UPPER && random.nextInt(30) == 0) {
            level.addParticle(ParticleTypes.END_ROD,
                    pos.getX() + 0.5D, pos.getY() + 0.95D, pos.getZ() + 0.5D,
                    0.0D, 0.02D, 0.0D);
        }
    }

    @Override
    public boolean isRandomlyTicking(BlockState state) {
        return false;
    }

    /** Snow and light make this block glow softly even in the day. */
    @Override
    public boolean propagatesSkylightDown(BlockState state, BlockGetter level, BlockPos pos) {
        return true;
    }

    /** Water logging is not supported, but falling fluid should not wash the tree away. */
    @Override
    public boolean canBeReplaced(BlockState state, net.minecraft.world.item.context.BlockPlaceContext context) {
        return context.getFluidState().getType() == Fluids.EMPTY && super.canBeReplaced(state, context);
    }
}
