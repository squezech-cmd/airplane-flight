package com.customnpcs.item;

import com.customnpcs.entity.CustomNpc;
import com.customnpcs.entity.NpcBehaviour;
import com.customnpcs.registry.ModEntities;
import net.minecraft.ChatFormatting;
import net.minecraft.core.BlockPos;
import net.minecraft.network.chat.Component;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.world.InteractionResult;
import net.minecraft.world.entity.MobSpawnType;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.TooltipFlag;
import net.minecraft.world.item.context.UseOnContext;
import net.minecraft.world.level.Level;

import java.util.List;

/**
 * Places a fresh NPC where you right-click.
 *
 * <p>Shift-right-click cycles the behaviour without placing anything, which is
 * the quickest way to put down a guard and then a wanderer without opening a
 * menu in between. The name is taken from the item's NBT when it has one, so
 * clones made with the wand keep their identity.
 */
public class NpcSpawnerItem extends Item {
    static final String TAG_NAME = "NpcName";
    static final String TAG_BEHAVIOUR = "NpcBehaviour";
    static final String TAG_SKIN = "Skin";
    static final String TAG_SCALE = "NpcScale";
    static final String TAG_DIALOGUE = "NpcDialogue";
    static final String TAG_HOSTILE = "NpcHostile";

    public NpcSpawnerItem(Properties properties) {
        super(properties);
    }

    public static String configuredName(ItemStack stack) {
        return stack.hasTag() && stack.getTag() != null && stack.getTag().contains(TAG_NAME)
                ? stack.getTag().getString(TAG_NAME)
                : "";
    }

    public static NpcBehaviour configuredBehaviour(ItemStack stack) {
        return stack.hasTag() && stack.getTag() != null && stack.getTag().contains(TAG_BEHAVIOUR)
                ? NpcBehaviour.byOrdinal(stack.getTag().getInt(TAG_BEHAVIOUR))
                : NpcBehaviour.WANDER;
    }

    public static void setConfiguredName(ItemStack stack, String name) {
        stack.getOrCreateTag().putString(TAG_NAME, name);
    }

    public static void setConfiguredBehaviour(ItemStack stack, NpcBehaviour behaviour) {
        stack.getOrCreateTag().putInt(TAG_BEHAVIOUR, behaviour.ordinal());
    }

    @Override
    public InteractionResult useOn(UseOnContext context) {
        Level level = context.getLevel();
        Player player = context.getPlayer();
        ItemStack stack = context.getItemInHand();
        if (player == null) {
            return InteractionResult.PASS;
        }
        if (player.isShiftKeyDown()) {
            if (!level.isClientSide) {
                NpcBehaviour next = configuredBehaviour(stack).next();
                setConfiguredBehaviour(stack, next);
                player.sendSystemMessage(Component.literal("NPC behaviour: " + next.display())
                        .withStyle(ChatFormatting.AQUA));
            }
            return InteractionResult.sidedSuccess(level.isClientSide);
        }
        if (level.isClientSide) {
            return InteractionResult.SUCCESS;
        }
        if (!(level instanceof ServerLevel serverLevel)) {
            return InteractionResult.PASS;
        }
        BlockPos pos = context.getClickedPos().relative(context.getClickedFace());
        CustomNpc npc = spawnAt(serverLevel, pos, player, stack);
        if (npc == null) {
            return InteractionResult.FAIL;
        }
        player.sendSystemMessage(Component.literal("Spawned " + npc.displayName()
                        + " (" + npc.behaviour().display() + ")")
                .withStyle(ChatFormatting.GREEN));
        if (!player.isCreative()) {
            stack.shrink(1);
        }
        return InteractionResult.CONSUME;
    }

    /** Shared by the spawner item and the wand, so both make identical NPCs. */
    public static CustomNpc spawnAt(ServerLevel level, BlockPos pos, Player owner, ItemStack stack) {
        CustomNpc npc = ModEntities.CUSTOM_NPC.get().create(level);
        if (npc == null) {
            return null;
        }
        npc.moveTo(pos.getX() + 0.5D, pos.getY(), pos.getZ() + 0.5D,
                owner.getYRot() + 180.0F, 0.0F);
        npc.setHome(pos);
        npc.setOwner(owner.getUUID());
        npc.finalizeSpawn(level, level.getCurrentDifficultyAt(pos), MobSpawnType.SPAWN_EGG, null, null);

        if (stack.hasTag() && stack.getTag() != null) {
            var tag = stack.getTag();
            if (tag.contains(TAG_NAME)) {
                npc.setCustomNameDirect(tag.getString(TAG_NAME));
            }
            if (tag.contains(TAG_SKIN)) {
                npc.setSkin(tag.getString(TAG_SKIN));
            }
            if (tag.contains(TAG_SCALE)) {
                npc.setScalePercent(tag.getInt(TAG_SCALE));
            }
            if (tag.contains(TAG_DIALOGUE)) {
                npc.setDialogue(tag.getString(TAG_DIALOGUE));
            }
            if (tag.contains(TAG_HOSTILE)) {
                npc.setHostile(tag.getBoolean(TAG_HOSTILE));
            }
        }
        npc.setBehaviour(configuredBehaviour(stack));
        level.addFreshEntity(npc);
        return npc;
    }

    @Override
    public void appendHoverText(ItemStack stack, @javax.annotation.Nullable Level level,
                                List<Component> tooltip, TooltipFlag flag) {
        String name = configuredName(stack);
        tooltip.add(Component.literal(name.isBlank() ? "Unnamed NPC" : "Name: " + name)
                .withStyle(ChatFormatting.GRAY));
        tooltip.add(Component.literal("Behaviour: " + configuredBehaviour(stack).display())
                .withStyle(ChatFormatting.DARK_AQUA));
        tooltip.add(Component.literal("Right-click: place").withStyle(ChatFormatting.DARK_GRAY));
        tooltip.add(Component.literal("Shift-right-click: change behaviour")
                .withStyle(ChatFormatting.DARK_GRAY));
    }
}
