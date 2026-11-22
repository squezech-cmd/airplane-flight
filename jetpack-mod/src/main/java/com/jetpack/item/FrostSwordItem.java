package com.jetpack.item;

import net.minecraft.core.particles.ParticleTypes;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.world.effect.MobEffectInstance;
import net.minecraft.world.effect.MobEffects;
import net.minecraft.world.entity.LivingEntity;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.SwordItem;
import net.minecraft.world.item.TooltipFlag;
import net.minecraft.network.chat.Component;
import net.minecraft.ChatFormatting;
import net.minecraft.world.level.Level;
import org.jetbrains.annotations.Nullable;

import java.util.List;

/** A sword that chills whatever it hits. */
public class FrostSwordItem extends SwordItem {
    public FrostSwordItem(Properties properties) {
        super(FrostTier.INSTANCE, 4, -2.2F, properties);
    }

    @Override
    public boolean hurtEnemy(ItemStack stack, LivingEntity target, LivingEntity attacker) {
        target.addEffect(new MobEffectInstance(MobEffects.MOVEMENT_SLOWDOWN, 20 * 4, 1));

        if (attacker.level() instanceof ServerLevel level) {
            level.sendParticles(ParticleTypes.SNOWFLAKE,
                    target.getX(), target.getY() + 1.0D, target.getZ(),
                    16, 0.35D, 0.5D, 0.35D, 0.04D);
            level.sendParticles(ParticleTypes.ITEM_SNOWBALL,
                    target.getX(), target.getY() + 1.0D, target.getZ(),
                    6, 0.25D, 0.35D, 0.25D, 0.02D);
        }
        return super.hurtEnemy(stack, target, attacker);
    }

    @Override
    public void appendHoverText(ItemStack stack, @Nullable Level level, List<Component> tooltip, TooltipFlag flag) {
        tooltip.add(Component.literal("Chills what it touches").withStyle(ChatFormatting.AQUA));
        super.appendHoverText(stack, level, tooltip, flag);
    }
}
