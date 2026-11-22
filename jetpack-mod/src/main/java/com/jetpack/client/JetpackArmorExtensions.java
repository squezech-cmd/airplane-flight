package com.jetpack.client;

import net.minecraft.client.Minecraft;
import net.minecraft.client.model.HumanoidModel;
import net.minecraft.world.entity.EquipmentSlot;
import net.minecraft.world.entity.LivingEntity;
import net.minecraft.world.item.ItemStack;
import net.minecraftforge.client.extensions.common.IClientItemExtensions;
import org.jetbrains.annotations.NotNull;

/**
 * Tells the armor renderer to draw {@link JetpackModel} instead of a diamond chestplate.
 *
 * <p>This class only ever exists on the client: {@code JetpackItem} registers it from
 * {@code initializeClient}, which Forge skips on a dedicated server.
 */
public class JetpackArmorExtensions implements IClientItemExtensions {
    private JetpackModel model;

    @Override
    public @NotNull HumanoidModel<?> getHumanoidArmorModel(LivingEntity entity, ItemStack stack,
                                                          EquipmentSlot slot, HumanoidModel<?> original) {
        if (slot != EquipmentSlot.CHEST) {
            return original;
        }
        if (this.model == null) {
            this.model = new JetpackModel(
                    Minecraft.getInstance().getEntityModels().bakeLayer(JetpackClientSetup.JETPACK_LAYER));
        }
        return this.model;
    }
}
