package com.jetpack;

import net.minecraft.ChatFormatting;
import net.minecraft.core.particles.ParticleTypes;
import net.minecraft.network.chat.Component;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.sounds.SoundEvents;
import net.minecraft.sounds.SoundSource;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.item.ArmorItem;
import net.minecraft.world.item.ArmorMaterials;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.TooltipFlag;
import net.minecraft.world.level.Level;
import net.minecraft.world.phys.Vec3;

import java.util.List;

public class JetpackItem extends ArmorItem {
    public static final int MAX_FUEL = 1000;
    private static final String FUEL_TAG = "Fuel";

    public JetpackItem() {
        super(ArmorMaterials.DIAMOND, Type.CHESTPLATE, new Item.Properties());
    }

    public static int getFuel(ItemStack stack) {
        return stack.getOrCreateTag().getInt(FUEL_TAG);
    }

    public static void setFuel(ItemStack stack, int fuel) {
        stack.getOrCreateTag().putInt(FUEL_TAG, Math.max(0, Math.min(MAX_FUEL, fuel)));
    }

    @Override
    public void onArmorTick(ItemStack stack, Level level, Player player) {
        if (level.isClientSide) {
            return;
        }

        Vec3 motion = player.getDeltaMovement();

        // Jetpack stabilizers: never fall fast, never take fall damage
        if (motion.y < -0.35) {
            player.setDeltaMovement(motion.x, -0.35, motion.z);
            motion = player.getDeltaMovement();
        }
        player.fallDistance = 0;

        boolean thrusting = ThrustState.isThrusting(player) && getFuel(stack) > 0;
        if (thrusting) {
            double newY = Math.min(motion.y + 0.18, 0.55);
            player.setDeltaMovement(motion.x, newY, motion.z);
            player.fallDistance = 0;
            setFuel(stack, getFuel(stack) - 2);

            if (level instanceof ServerLevel serverLevel) {
                serverLevel.sendParticles(ParticleTypes.FLAME,
                        player.getX(), player.getY() + 0.4, player.getZ(),
                        3, 0.15, 0.1, 0.15, 0.05);
            }
            if (player.tickCount % 8 == 0) {
                level.playSound(null, player.blockPosition(), SoundEvents.FIREWORK_ROCKET_BLAST,
                        SoundSource.PLAYERS, 0.4F, 1.2F);
            }
        } else {
            // Slowly refuel when not thrusting
            int fuel = getFuel(stack);
            if (fuel < MAX_FUEL) {
                setFuel(stack, fuel + 1);
            }
        }
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
        tooltip.add(Component.literal("Hold SPACE to fly!").withStyle(ChatFormatting.GOLD));
        tooltip.add(Component.literal("Fuel: " + getFuel(stack) + " / " + MAX_FUEL).withStyle(ChatFormatting.GRAY));
    }
}
