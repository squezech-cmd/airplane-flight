package com.jetpack.item;

import net.minecraft.ChatFormatting;
import net.minecraft.core.particles.ParticleTypes;
import net.minecraft.network.chat.Component;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.sounds.SoundEvents;
import net.minecraft.sounds.SoundSource;
import net.minecraft.world.InteractionHand;
import net.minecraft.world.InteractionResultHolder;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.TooltipFlag;
import net.minecraft.world.level.Level;
import org.jetbrains.annotations.Nullable;

import java.util.List;

/**
 * Shake it and a little snowstorm swirls around you. Purely cosmetic, but very
 * hard to stop using.
 */
public class SnowGlobeItem extends Item {
    public SnowGlobeItem(Properties properties) {
        super(properties);
    }

    @Override
    public InteractionResultHolder<ItemStack> use(Level level, Player player, InteractionHand hand) {
        ItemStack stack = player.getItemInHand(hand);

        level.playSound(player, player.blockPosition(), SoundEvents.GLASS_BREAK, SoundSource.PLAYERS,
                0.35F, 1.8F);
        level.playSound(player, player.blockPosition(), SoundEvents.AMETHYST_BLOCK_CHIME, SoundSource.PLAYERS,
                0.8F, 1.2F);

        if (level instanceof ServerLevel serverLevel) {
            for (int i = 0; i < 60; i++) {
                double angle = serverLevel.random.nextDouble() * Math.PI * 2.0D;
                double radius = 1.2D + serverLevel.random.nextDouble() * 2.2D;
                double dx = Math.cos(angle) * radius;
                double dz = Math.sin(angle) * radius;

                serverLevel.sendParticles(ParticleTypes.SNOWFLAKE,
                        player.getX() + dx, player.getY() + 0.4D + serverLevel.random.nextDouble() * 2.0D,
                        player.getZ() + dz,
                        1, 0.0D, -0.02D, 0.0D, 0.01D);
            }
            for (int i = 0; i < 16; i++) {
                serverLevel.sendParticles(ParticleTypes.END_ROD,
                        player.getX() + (serverLevel.random.nextDouble() - 0.5D) * 2.5D,
                        player.getY() + serverLevel.random.nextDouble() * 2.0D,
                        player.getZ() + (serverLevel.random.nextDouble() - 0.5D) * 2.5D,
                        1, 0.0D, 0.0D, 0.0D, 0.0D);
            }
        }

        player.getCooldowns().addCooldown(this, 30);
        return InteractionResultHolder.sidedSuccess(stack, level.isClientSide());
    }

    @Override
    public void appendHoverText(ItemStack stack, @Nullable Level level, List<Component> tooltip, TooltipFlag flag) {
        tooltip.add(Component.literal("Right-click for a private snowstorm")
                .withStyle(ChatFormatting.AQUA));
        super.appendHoverText(stack, level, tooltip, flag);
    }
}
