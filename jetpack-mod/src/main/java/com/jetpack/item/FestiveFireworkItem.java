package com.jetpack.item;

import net.minecraft.ChatFormatting;
import net.minecraft.nbt.CompoundTag;
import net.minecraft.nbt.ListTag;
import net.minecraft.network.chat.Component;
import net.minecraft.sounds.SoundEvents;
import net.minecraft.sounds.SoundSource;
import net.minecraft.world.InteractionHand;
import net.minecraft.world.InteractionResultHolder;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.Items;
import net.minecraft.world.item.TooltipFlag;
import net.minecraft.world.level.Level;
import org.jetbrains.annotations.Nullable;

import java.util.List;

/**
 * A rocket that always bursts into a random festive pattern in red, green,
 * gold or ice blue. Launches straight up from the player.
 */
public class FestiveFireworkItem extends Item {
    private static final int[] COLOURS = {
            0xE23B4E, // red
            0x2E9B4F, // green
            0xFFD25E, // gold
            0x8FE3FF, // ice blue
            0xF2F2F2  // snow white
    };

    public FestiveFireworkItem(Properties properties) {
        super(properties);
    }

    @Override
    public InteractionResultHolder<ItemStack> use(Level level, Player player, InteractionHand hand) {
        ItemStack stack = player.getItemInHand(hand);

        level.playSound(null, player.getX(), player.getY(), player.getZ(),
                SoundEvents.FIREWORK_ROCKET_LAUNCH, SoundSource.PLAYERS, 1.0F, 1.0F);

        if (!level.isClientSide) {
            ItemStack rocket = buildRocket(level.getRandom());
            net.minecraft.world.entity.projectile.FireworkRocketEntity entity =
                    new net.minecraft.world.entity.projectile.FireworkRocketEntity(
                            level, rocket,
                            player.getX(), player.getEyeY() - 0.15D, player.getZ(), true);
            level.addFreshEntity(entity);
        }

        if (!player.getAbilities().instabuild) {
            stack.shrink(1);
        }
        return InteractionResultHolder.sidedSuccess(stack, level.isClientSide());
    }

    private static ItemStack buildRocket(net.minecraft.util.RandomSource random) {
        ItemStack rocket = new ItemStack(Items.FIREWORK_ROCKET);
        CompoundTag fireworks = rocket.getOrCreateTagElement("Fireworks");
        fireworks.putByte("Flight", (byte) (1 + random.nextInt(2)));

        ListTag explosions = new ListTag();

        CompoundTag main = new CompoundTag();
        int primary = COLOURS[random.nextInt(COLOURS.length)];
        int secondary = COLOURS[random.nextInt(COLOURS.length)];
        main.putIntArray("Colors", new int[] {primary, secondary});
        main.putInt("Type", random.nextInt(4) == 0 ? 1 : 0);
        main.putBoolean("Flicker", random.nextBoolean());
        main.putBoolean("Trail", random.nextBoolean());
        explosions.add(main);

        // A second, smaller burst makes the show feel grand.
        if (random.nextInt(3) != 0) {
            CompoundTag extra = new CompoundTag();
            extra.putIntArray("Colors", new int[] {COLOURS[random.nextInt(COLOURS.length)]});
            extra.putInt("Type", random.nextInt(3));
            extra.putBoolean("Trail", true);
            explosions.add(extra);
        }

        fireworks.put("Explosions", explosions);
        return rocket;
    }

    @Override
    public void appendHoverText(ItemStack stack, @Nullable Level level, List<Component> tooltip, TooltipFlag flag) {
        tooltip.add(Component.literal("Launches a random festive firework")
                .withStyle(ChatFormatting.LIGHT_PURPLE));
        super.appendHoverText(stack, level, tooltip, flag);
    }
}
