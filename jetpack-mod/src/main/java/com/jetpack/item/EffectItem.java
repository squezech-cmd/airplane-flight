package com.jetpack.item;

import net.minecraft.ChatFormatting;
import net.minecraft.core.particles.ParticleTypes;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.sounds.SoundEvents;
import net.minecraft.sounds.SoundSource;
import net.minecraft.world.InteractionHand;
import net.minecraft.world.InteractionResultHolder;
import net.minecraft.world.effect.MobEffect;
import net.minecraft.world.effect.MobEffectInstance;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.level.Level;

import java.util.function.Supplier;

/**
 * A consumable (or right-clickable) item that grants one or two potion effects
 * and leaves a little puff of themed particles behind.
 */
public class EffectItem extends TooltipItem {
    private final Supplier<MobEffect> primary;
    private final int primaryTicks;
    private final Supplier<MobEffect> secondary;
    private final int secondaryTicks;
    private final boolean drink;

    public EffectItem(Properties properties, ChatFormatting colour, String[] lines,
                      Supplier<MobEffect> primary, int primaryTicks,
                      Supplier<MobEffect> secondary, int secondaryTicks,
                      boolean drink) {
        super(properties, colour, lines);
        this.primary = primary;
        this.primaryTicks = primaryTicks;
        this.secondary = secondary;
        this.secondaryTicks = secondaryTicks;
        this.drink = drink;
    }

    @Override
    public InteractionResultHolder<ItemStack> use(Level level, Player player, InteractionHand hand) {
        ItemStack stack = player.getItemInHand(hand);

        if (!level.isClientSide) {
            applyEffects(player);
            level.playSound(null, player.getX(), player.getY(), player.getZ(),
                    this.drink ? SoundEvents.GENERIC_DRINK : SoundEvents.GENERIC_EAT,
                    SoundSource.PLAYERS, 0.9F, 1.1F);
        } else {
            spawnBurst(player);
        }

        if (!player.getAbilities().instabuild) {
            stack.shrink(1);
        }
        return InteractionResultHolder.sidedSuccess(stack, level.isClientSide());
    }

    private void applyEffects(Player player) {
        if (this.primary != null) {
            player.addEffect(new MobEffectInstance(this.primary.get(), this.primaryTicks, 0, false, true, true));
        }
        if (this.secondary != null) {
            player.addEffect(new MobEffectInstance(this.secondary.get(), this.secondaryTicks, 0, false, true, true));
        }
    }

    private void spawnBurst(Player player) {
        Level level = player.level();
        for (int i = 0; i < 14; i++) {
            double dx = (level.random.nextDouble() - 0.5D) * 0.8D;
            double dy = level.random.nextDouble() * 0.9D;
            double dz = (level.random.nextDouble() - 0.5D) * 0.8D;
            level.addParticle(ParticleTypes.SNOWFLAKE,
                    player.getX() + dx, player.getY() + 1.0D + dy, player.getZ() + dz,
                    0.0D, 0.02D, 0.0D);
        }
        if (level instanceof ServerLevel serverLevel) {
            serverLevel.sendParticles(ParticleTypes.HAPPY_VILLAGER,
                    player.getX(), player.getY() + 1.0D, player.getZ(),
                    6, 0.4D, 0.4D, 0.4D, 0.01D);
        }
    }
}
