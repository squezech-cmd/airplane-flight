package com.jetpack.item;

import net.minecraft.ChatFormatting;
import net.minecraft.network.chat.Component;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.TooltipFlag;
import net.minecraft.world.level.Level;
import org.jetbrains.annotations.Nullable;

import java.util.List;

/** An item that carries a line or two of flavour text. */
public class TooltipItem extends Item {
    private final String[] lines;
    private final ChatFormatting colour;

    public TooltipItem(Properties properties, ChatFormatting colour, String... lines) {
        super(properties);
        this.colour = colour;
        this.lines = lines;
    }

    public TooltipItem(Properties properties, String... lines) {
        this(properties, ChatFormatting.GRAY, lines);
    }

    @Override
    public void appendHoverText(ItemStack stack, @Nullable Level level, List<Component> tooltip, TooltipFlag flag) {
        for (String line : this.lines) {
            tooltip.add(Component.literal(line).withStyle(this.colour));
        }
        super.appendHoverText(stack, level, tooltip, flag);
    }
}
