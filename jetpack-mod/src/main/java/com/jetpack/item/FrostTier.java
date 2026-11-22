package com.jetpack.item;

import com.jetpack.registry.ChristmasItems;
import net.minecraft.world.item.Items;
import net.minecraft.world.item.Tier;
import net.minecraft.world.item.crafting.Ingredient;

/** A tier sitting between iron and diamond, built out of ice. */
public final class FrostTier implements Tier {
    public static final FrostTier INSTANCE = new FrostTier();

    private FrostTier() {
    }

    @Override
    public int getUses() {
        return 900;
    }

    @Override
    public float getSpeed() {
        return 7.0F;
    }

    @Override
    public float getAttackDamageBonus() {
        return 2.0F;
    }

    @Override
    public int getLevel() {
        return 2;
    }

    @Override
    public int getEnchantmentValue() {
        return 18;
    }

    @Override
    public Ingredient getRepairIngredient() {
        return Ingredient.of(Items.PACKED_ICE, ChristmasItems.ICE_SHARD.get());
    }
}
