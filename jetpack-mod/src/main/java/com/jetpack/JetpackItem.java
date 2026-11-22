package com.jetpack;

import com.jetpack.client.JetpackArmorExtensions;
import net.minecraft.ChatFormatting;
import net.minecraft.core.particles.ParticleTypes;
import net.minecraft.network.chat.Component;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.sounds.SoundEvents;
import net.minecraft.sounds.SoundSource;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.EquipmentSlot;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.item.ArmorItem;
import net.minecraft.world.item.ArmorMaterials;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.TooltipFlag;
import net.minecraft.world.level.Level;
import net.minecraft.world.phys.Vec3;
import net.minecraftforge.client.extensions.common.IClientItemExtensions;
import org.jetbrains.annotations.Nullable;

import java.util.List;
import java.util.function.Consumer;

/**
 * The jetpack itself: a chest slot item with its own fuel tank, soft-landing
 * stabilizers and a custom 3D model worn on the player's back.
 *
 * <p>All the thrust math runs in {@link #inventoryTick} because that hook fires on
 * both sides. The client is the one that actually moves the player; the server only
 * burns fuel, plays sounds and spawns exhaust so everyone else sees the flames.
 */
public class JetpackItem extends ArmorItem {
    public static final int MAX_FUEL = 2000;
    private static final String FUEL_TAG = "Fuel";

    /** Vertical speed added each tick while the thrusters fire. */
    private static final double THRUST_ACCEL = 0.16D;
    /** Top ascent speed (~0.5 blocks/tick, a bit faster than a boat on ice). */
    private static final double MAX_ASCENT = 0.48D;
    /** The stabilizers never let the player drop faster than this. */
    private static final double MAX_DESCENT = -0.30D;
    /** Fuel burnt per tick of flight. */
    private static final int FUEL_PER_TICK = 2;
    /** Passive refuel while the thrusters are off. */
    private static final int REFUEL_PER_TICK = 5;

    public JetpackItem() {
        super(ArmorMaterials.DIAMOND, Type.CHESTPLATE, new Item.Properties().stacksTo(1));
    }

    public static int getFuel(ItemStack stack) {
        return stack.getOrCreateTag().getInt(FUEL_TAG);
    }

    public static void setFuel(ItemStack stack, int fuel) {
        stack.getOrCreateTag().putInt(FUEL_TAG, Math.max(0, Math.min(MAX_FUEL, fuel)));
    }

    /** A jetpack straight out of the creative menu comes with a full tank. */
    private static void ensureFuel(ItemStack stack) {
        if (!stack.getOrCreateTag().contains(FUEL_TAG)) {
            setFuel(stack, MAX_FUEL);
        }
    }

    @Override
    public void inventoryTick(ItemStack stack, Level level, Entity entity, int slotIndex, boolean selected) {
        super.inventoryTick(stack, level, entity, slotIndex, selected);

        if (!(entity instanceof Player player) || player.isSpectator()) {
            return;
        }
        if (!isWornOnChest(player, stack)) {
            return;
        }

        ensureFuel(stack);

        if (level.isClientSide) {
            tickClient(stack, player);
        } else {
            tickServer(stack, level, player);
        }
    }

    private static boolean isWornOnChest(Player player, ItemStack stack) {
        return player.getItemBySlot(EquipmentSlot.CHEST) == stack;
    }

    /**
     * Movement is settled on the client: the server would only rubber-band the player
     * back to wherever the client says they are. Running this from the inventory tick
     * keeps the push in {@code deltaMovement} for {@code travel()} to consume.
     */
    private void tickClient(ItemStack stack, Player player) {
        // Creative flight and elytra manage their own physics, don't fight them.
        if (player.getAbilities().flying || player.isFallFlying()) {
            return;
        }

        boolean thrusting = ThrustState.isClientThrusting(player) && getFuel(stack) > 0;

        Vec3 motion = player.getDeltaMovement();
        double y = motion.y;

        if (thrusting) {
            // Push harder on the way down so taking off feels snappy.
            double accel = y < 0.3D ? THRUST_ACCEL * 2.5D : THRUST_ACCEL;
            y = Math.min(y + accel, MAX_ASCENT);
        } else if (!player.onGround() && !player.isInWater() && y < MAX_DESCENT) {
            // Landing jets: the jetpack always catches the player.
            y = MAX_DESCENT;
        }

        if (y != motion.y) {
            player.setDeltaMovement(motion.x, y, motion.z);
        }

        player.fallDistance = 0.0F;
    }

    private void tickServer(ItemStack stack, Level level, Player player) {
        // Never take fall damage while wearing the jetpack.
        player.fallDistance = 0.0F;

        // The client keeps us off the ground for long stretches, which vanilla reads as
        // cheating. Reset its "floating" counter so the player is not kicked.
        if (player instanceof ServerPlayer serverPlayer) {
            serverPlayer.connection.aboveGroundTickCount = 0;
        }

        boolean thrusting = ThrustState.isThrusting(player) && getFuel(stack) > 0;

        if (!thrusting) {
            int fuel = getFuel(stack);
            if (fuel < MAX_FUEL) {
                setFuel(stack, fuel + REFUEL_PER_TICK);
            }
            return;
        }

        if (!player.isCreative()) {
            setFuel(stack, getFuel(stack) - FUEL_PER_TICK);
        }

        if (level instanceof ServerLevel serverLevel) {
            spawnExhaust(serverLevel, player);
        }

        if (player.tickCount % 6 == 0) {
            level.playSound(null, player.blockPosition(), SoundEvents.FIREWORK_ROCKET_BLAST,
                    SoundSource.PLAYERS, 0.35F, 1.25F);
        }
    }

    /** Two flame jets coming out of the nozzles at the bottom of the tanks. */
    private static void spawnExhaust(ServerLevel level, Player player) {
        Vec3 look = player.getLookAngle();
        Vec3 right = new Vec3(-look.z, 0.0D, look.x);
        right = right.lengthSqr() < 1.0E-4D ? new Vec3(1.0D, 0.0D, 0.0D) : right.normalize();

        Vec3 base = player.position().add(0.0D, 1.1D, 0.0D).subtract(look.scale(0.30D));

        for (int side = -1; side <= 1; side += 2) {
            Vec3 nozzle = base.add(right.scale(0.24D * side));
            level.sendParticles(ParticleTypes.FLAME,
                    nozzle.x, nozzle.y, nozzle.z,
                    2, 0.05D, 0.10D, 0.05D, 0.02D);

            if (player.tickCount % 2 == 0) {
                level.sendParticles(ParticleTypes.SMOKE,
                        nozzle.x, nozzle.y - 0.15D, nozzle.z,
                        1, 0.04D, 0.06D, 0.04D, 0.01D);
            }
        }
    }

    /**
     * Without this the chestplate would be drawn with the diamond texture.
     * The jetpack is not dyeable, so the {@code "overlay"} pass never runs.
     */
    @Nullable
    @Override
    public String getArmorTexture(ItemStack stack, Entity entity, EquipmentSlot slot, String type) {
        return "jetpack:textures/models/armor/jetpack_layer_1.png";
    }

    @Override
    public void initializeClient(Consumer<IClientItemExtensions> consumer) {
        consumer.accept(new JetpackArmorExtensions());
    }

    @Override
    public boolean isBarVisible(ItemStack stack) {
        return true;
    }

    @Override
    public int getBarWidth(ItemStack stack) {
        return Math.round(13.0F * ((float) getFuel(stack) / (float) MAX_FUEL));
    }

    @Override
    public int getBarColor(ItemStack stack) {
        return 0xFF8800;
    }

    @Override
    public void appendHoverText(ItemStack stack, Level level, List<Component> tooltip, TooltipFlag flag) {
        tooltip.add(Component.literal("Hold SPACE to fly").withStyle(ChatFormatting.AQUA));
        tooltip.add(Component.literal("Hold SHIFT while flying to descend faster").withStyle(ChatFormatting.GRAY));
        tooltip.add(Component.literal("Fuel: " + getFuel(stack) + " / " + MAX_FUEL).withStyle(ChatFormatting.GOLD));
    }
}
