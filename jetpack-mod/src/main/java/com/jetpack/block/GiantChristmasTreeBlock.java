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
import net.minecraft.world.level.block.state.BlockState;
import net.minecraft.world.level.block.state.StateDefinition;
import net.minecraft.world.level.block.state.properties.BlockStateProperties;
import net.minecraft.world.level.block.state.properties.IntegerProperty;
import net.minecraft.world.phys.shapes.CollisionContext;
import net.minecraft.world.phys.shapes.Shapes;
import net.minecraft.world.phys.shapes.VoxelShape;
import org.jetbrains.annotations.Nullable;

/**
 * The centrepiece: a three-block-tall Christmas tree with eight foliage tiers,
 * a spiral light garland, fourteen baubles and a glowing star.
 *
 * <p>Place the bottom block and the two above it are filled in. Only the trunk
 * has collision, so you can walk through the branches.
 */
public class GiantChristmasTreeBlock extends Block {
    /** 0 = bottom, 1 = middle, 2 = top. */
    public static final IntegerProperty PART = IntegerProperty.create("part", 0, 2);

    private static final VoxelShape TRUNK = Block.box(7.3D, 0.0D, 7.3D, 8.7D, 16.0D, 8.7D);
    private static final VoxelShape LOWER_FOLIAGE = Shapes.or(
            Block.box(0.2D, 1.6D, 0.2D, 15.8D, 8.0D, 15.8D),
            Block.box(3.2D, 8.0D, 3.2D, 12.8D, 16.0D, 12.8D));
    private static final VoxelShape MIDDLE_FOLIAGE = Shapes.or(
            Block.box(1.0D, 0.0D, 1.0D, 15.0D, 7.0D, 15.0D),
            Block.box(4.0D, 7.0D, 4.0D, 12.0D, 16.0D, 12.0D));
    private static final VoxelShape TOP_FOLIAGE = Shapes.or(
            Block.box(2.4D, 0.0D, 2.4D, 13.6D, 7.0D, 13.6D),
            Block.box(5.0D, 7.0D, 5.0D, 11.0D, 16.0D, 11.0D));

    public GiantChristmasTreeBlock(Properties properties) {
        super(properties);
        registerDefaultState(this.stateDefinition.any().setValue(PART, 0));
    }

    @Override
    protected void createBlockStateDefinition(StateDefinition.Builder<Block, BlockState> builder) {
        builder.add(PART);
    }

    public static int partIndex(BlockState state) {
        return state.getValue(PART);
    }

    // ------------------------------------------------------------------ shapes

    @Override
    public VoxelShape getShape(BlockState state, BlockGetter level, BlockPos pos, CollisionContext context) {
        return switch (partIndex(state)) {
            case 1 -> MIDDLE_FOLIAGE;
            case 2 -> TOP_FOLIAGE;
            default -> Shapes.or(TRUNK, LOWER_FOLIAGE);
        };
    }

    @Override
    public VoxelShape getCollisionShape(BlockState state, BlockGetter level, BlockPos pos, CollisionContext context) {
        // Only the trunk is solid, and only on the bottom block.
        return partIndex(state) == 0 ? TRUNK : Shapes.empty();
    }

    // ---------------------------------------------------------------- placing

    @Nullable
    @Override
    public BlockState getStateForPlacement(BlockPlaceContext context) {
        Level level = context.getLevel();
        BlockPos pos = context.getClickedPos();
        if (pos.getY() + 2 >= level.getMaxBuildHeight()) {
            return null;
        }
        for (int i = 1; i <= 2; i++) {
            if (!level.getBlockState(pos.above(i)).canBeReplaced(context)) {
                return null;
            }
        }
        return defaultBlockState().setValue(PART, 0);
    }

    @Override
    public void setPlacedBy(Level level, BlockPos pos, BlockState state, @Nullable LivingEntity placer, ItemStack stack) {
        level.setBlockAndUpdate(pos.above(1), defaultBlockState().setValue(PART, 1));
        level.setBlockAndUpdate(pos.above(2), defaultBlockState().setValue(PART, 2));
    }

    // ------------------------------------------------------------- survival

    @Override
    public boolean canSurvive(BlockState state, LevelReader level, BlockPos pos) {
        int part = partIndex(state);
        if (part == 0) {
            return true;
        }
        BlockState below = level.getBlockState(pos.below());
        return below.is(this) && partIndex(below) == part - 1;
    }

    @Override
    public BlockState updateShape(BlockState state, Direction direction, BlockState neighbourState,
                                  LevelAccessor level, BlockPos pos, BlockPos neighbourPos) {
        if (direction == Direction.DOWN && partIndex(state) != 0 && !neighbourState.is(this)) {
            return Blocks.AIR.defaultBlockState();
        }
        return super.updateShape(state, direction, neighbourState, level, pos, neighbourPos);
    }

    @Override
    public void playerWillDestroy(Level level, BlockPos pos, BlockState state, Player player) {
        if (!level.isClientSide) {
            int part = partIndex(state);
            // Breaking any part takes the whole tree down, but only the bottom drops an item.
            if (part != 0) {
                BlockPos bottom = pos.below(part);
                if (level.getBlockState(bottom).is(this)) {
                    level.destroyBlock(bottom, !player.isCreative(), player);
                }
            } else {
                for (int i = 1; i <= 2; i++) {
                    BlockPos above = pos.above(i);
                    if (level.getBlockState(above).is(this)) {
                        BlockState old = level.getBlockState(above);
                        level.setBlock(above, Blocks.AIR.defaultBlockState(), 35);
                        level.levelEvent(player, 2001, above, Block.getId(old));
                    }
                }
            }
        }
        super.playerWillDestroy(level, pos, state, player);
    }

    // -------------------------------------------------------------- animation

    @Override
    public void animateTick(BlockState state, Level level, BlockPos pos, RandomSource random) {
        int part = partIndex(state);

        // Warm twinkle in the foliage.
        if (random.nextInt(5) == 0) {
            double x = pos.getX() + 0.1D + random.nextDouble() * 0.8D;
            double z = pos.getZ() + 0.1D + random.nextDouble() * 0.8D;
            double y = pos.getY() + random.nextDouble();
            level.addParticle(ParticleTypes.FIREWORK, x, y, z, 0.0D, 0.003D, 0.0D);
        }

        // Falling glitter from the branches.
        if (random.nextInt(8) == 0) {
            level.addParticle(ParticleTypes.SNOWFLAKE,
                    pos.getX() + random.nextDouble(), pos.getY() + 0.9D,
                    pos.getZ() + random.nextDouble(),
                    0.0D, -0.03D, 0.0D);
        }

        // Star sparkle, only on the top block.
        if (part == 2 && random.nextInt(18) == 0) {
            level.addParticle(ParticleTypes.END_ROD,
                    pos.getX() + 0.5D, pos.getY() + 0.85D, pos.getZ() + 0.5D,
                    0.0D, 0.03D, 0.0D);
        }

        // Occasional golden spark drifting down the whole tree.
        if (random.nextInt(40) == 0) {
            level.addParticle(ParticleTypes.WAX_ON,
                    pos.getX() + 0.2D + random.nextDouble() * 0.6D,
                    pos.getY() + 0.8D,
                    pos.getZ() + 0.2D + random.nextDouble() * 0.6D,
                    0.0D, -0.01D, 0.0D);
        }
    }

    @Override
    public boolean propagatesSkylightDown(BlockState state, BlockGetter level, BlockPos pos) {
        return true;
    }
}
