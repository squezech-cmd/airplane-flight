package com.jetpack.item;

import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.Items;
import net.minecraft.world.item.Tier;
import net.minecraft.world.item.crafting.Ingredient;

/** A tier sitting between iron and diamond, built out of ice. */
public final class FrostTier implements Tier {
    public static final FrostTier INSTANCE = new FrostTier();

    private Ingredient repair;

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

    /**
     * Built lazily: this is only asked for when an anvil or grindstone opens,
     * long after registration, so the ice shard is guaranteed to exist.
     */
    @Override
    public Ingredient getRepairIngredient() {
        if (this.repair == null) {
            Item shard = BuiltInRegistries.ITEM.get(new ResourceLocation("jetpack", "ice_shard"));
            this.repair = shard == Items.AIR
                    ? Ingredient.of(Items.PACKED_ICE)
                    : Ingredient.of(Items.PACKED_ICE, shard);
        }
        return this.repair;
    }
}
