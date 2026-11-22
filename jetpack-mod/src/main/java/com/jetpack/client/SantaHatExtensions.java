package com.jetpack.client;

import net.minecraft.client.Minecraft;
import net.minecraft.client.model.HumanoidModel;
import net.minecraft.world.entity.EquipmentSlot;
import net.minecraft.world.entity.LivingEntity;
import net.minecraft.world.item.ItemStack;
import net.minecraftforge.client.extensions.common.IClientItemExtensions;
import org.jetbrains.annotations.NotNull;

/** Draws the custom Santa hat model instead of a leather helmet. */
public class SantaHatExtensions implements IClientItemExtensions {
    private SantaHatModel model;

    @Override
    public @NotNull HumanoidModel<?> getHumanoidArmorModel(LivingEntity entity, ItemStack stack,
                                                          EquipmentSlot slot, HumanoidModel<?> original) {
        if (slot != EquipmentSlot.HEAD) {
            return original;
        }
        if (this.model == null) {
            this.model = new SantaHatModel(
                    Minecraft.getInstance().getEntityModels().bakeLayer(JetpackClientSetup.SANTA_HAT_LAYER));
        }
        return this.model;
    }
}
