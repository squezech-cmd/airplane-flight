package com.jetpack.item;

import com.jetpack.client.SantaHatExtensions;
import net.minecraft.ChatFormatting;
import net.minecraft.network.chat.Component;
import net.minecraft.sounds.SoundEvents;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.EquipmentSlot;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.item.ArmorItem;
import net.minecraft.world.item.ArmorMaterials;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.TooltipFlag;
import net.minecraft.world.level.Level;
import net.minecraftforge.client.extensions.common.IClientItemExtensions;
import org.jetbrains.annotations.Nullable;

import java.util.List;
import java.util.function.Consumer;

/**
 * A fluffy red hat with a white brim and a dangling pompom. Wearing it makes
 * every jetpack flight feel like a sleigh ride.
 */
public class SantaHatItem extends ArmorItem {
    public SantaHatItem(Properties properties) {
        super(ArmorMaterials.LEATHER, Type.HELMET, properties);
    }

    @Override
    public void inventoryTick(ItemStack stack, Level level, Entity entity, int slotIndex, boolean selected) {
        super.inventoryTick(stack, level, entity, slotIndex, selected);

        if (level.isClientSide || !(entity instanceof Player player)) {
            return;
        }
        if (!(player.getItemBySlot(EquipmentSlot.HEAD).getItem() instanceof SantaHatItem)) {
            return;
        }
        // A soft jingle every couple of seconds while the hat is worn.
        if (player.tickCount % 50 == 0) {
            level.playSound(null, player.blockPosition(), SoundEvents.NOTE_BLOCK_BELL.get(),
                    net.minecraft.sounds.SoundSource.PLAYERS, 0.18F, 1.9F);
        }
    }

    @Nullable
    @Override
    public String getArmorTexture(ItemStack stack, Entity entity, EquipmentSlot slot, String type) {
        return "jetpack:textures/models/armor/santa_hat_layer_1.png";
    }

    @Override
    public void initializeClient(Consumer<IClientItemExtensions> consumer) {
        consumer.accept(new SantaHatExtensions());
    }

    @Override
    public void appendHoverText(ItemStack stack, @Nullable Level level, List<Component> tooltip, TooltipFlag flag) {
        tooltip.add(Component.literal("Jingles softly while worn").withStyle(ChatFormatting.RED));
        super.appendHoverText(stack, level, tooltip, flag);
    }
}
