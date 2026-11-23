package com.customnpcs.item;

import com.customnpcs.entity.CustomNpc;
import com.customnpcs.entity.NpcBehaviour;
import com.customnpcs.network.NetworkHandler;
import com.customnpcs.network.OpenEditorPacket;
import net.minecraft.ChatFormatting;
import net.minecraft.core.BlockPos;
import net.minecraft.nbt.CompoundTag;
import net.minecraft.network.chat.Component;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.InteractionHand;
import net.minecraft.world.InteractionResult;
import net.minecraft.world.entity.EquipmentSlot;
import net.minecraft.world.entity.LivingEntity;
import net.minecraft.world.entity.MobSpawnType;
import net.minecraft.world.entity.player.Player;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.TooltipFlag;
import net.minecraft.world.item.context.UseOnContext;
import net.minecraft.world.level.Level;
import net.minecraftforge.network.PacketDistributor;

import java.util.List;

/**
 * The editor wand, and the fastest way to make a crowd of NPCs.
 *
 * <ul>
 *     <li>Right-click an NPC: open its editor.</li>
 *     <li>Sneak-right-click an NPC: copy appearance, gear and dialogue into
 *     the wand so the next placement clones it.</li>
 *     <li>Right-click a block: place a copy, or if the wand is empty, tell the
 *     player how to use it.</li>
 * </ul>
 */
public class NpcWandItem extends Item {
    private static final String TAG_NAME = "NpcName";
    private static final String TAG_SKIN = "Skin";
    private static final String TAG_BEHAVIOUR = "NpcBehaviour";
    private static final String TAG_SCALE = "NpcScale";
    private static final String TAG_DIALOGUE = "NpcDialogue";
    private static final String TAG_HOSTILE = "NpcHostile";
    private static final String TAG_MAIN = "NpcMainHand";
    private static final String TAG_HEAD = "NpcHead";

    public NpcWandItem(Properties properties) {
        super(properties);
    }

    @Override
    public InteractionResult interactLivingEntity(ItemStack stack, Player player,
                                                 LivingEntity target, InteractionHand hand) {
        if (!(target instanceof CustomNpc npc)) {
            return InteractionResult.PASS;
        }
        if (player.level().isClientSide) {
            return InteractionResult.SUCCESS;
        }
        if (!mayEdit(player, npc)) {
            player.sendSystemMessage(Component.literal("That NPC is not yours to edit.")
                    .withStyle(ChatFormatting.RED));
            return InteractionResult.CONSUME;
        }
        if (player.isShiftKeyDown()) {
            copyInto(stack, npc);
            player.sendSystemMessage(Component.literal("Copied " + npc.displayName()
                    + " into the wand").withStyle(ChatFormatting.AQUA));
        } else if (player instanceof ServerPlayer serverPlayer) {
            NetworkHandler.INSTANCE.send(PacketDistributor.PLAYER.with(() -> serverPlayer),
                    new OpenEditorPacket(npc.getId()));
        }
        return InteractionResult.CONSUME;
    }

    /**
     * Checks the server settings before an NPC can be opened or copied.
     *
     * <p>Operators override both switches: a server owner should never be
     * locked out of their own world by their own config file.
     */
    private static boolean mayEdit(Player player, CustomNpc npc) {
        if (player.hasPermissions(2)) {
            return true;
        }
        if (!com.customnpcs.NpcConfig.getBoolean(com.customnpcs.NpcConfig.ALLOW_EDITING)) {
            player.sendSystemMessage(Component.literal("Editing NPCs is disabled on this server.")
                    .withStyle(ChatFormatting.RED));
            return false;
        }
        if (!com.customnpcs.NpcConfig.getBoolean(com.customnpcs.NpcConfig.OWNER_ONLY_EDITING)) {
            return true;
        }
        return npc.isOwnerOrOp(player);
    }

    @Override
    public InteractionResult useOn(UseOnContext context) {
        Level level = context.getLevel();
        Player player = context.getPlayer();
        if (player == null) {
            return InteractionResult.PASS;
        }
        ItemStack stack = context.getItemInHand();
        if (player.isShiftKeyDown()) {
            if (!level.isClientSide) {
                player.sendSystemMessage(Component.literal(
                                "Sneak-right-click an NPC to copy it into the wand.")
                        .withStyle(ChatFormatting.GRAY));
            }
            return InteractionResult.sidedSuccess(level.isClientSide);
        }
        if (!hasCopy(stack)) {
            if (!level.isClientSide) {
                player.sendSystemMessage(Component.literal(
                                "The wand is empty. Sneak-right-click an NPC to copy it.")
                        .withStyle(ChatFormatting.GRAY));
            }
            return InteractionResult.CONSUME;
        }
        if (level.isClientSide) {
            return InteractionResult.SUCCESS;
        }
        if (!(level instanceof ServerLevel serverLevel)) {
            return InteractionResult.PASS;
        }
        BlockPos pos = context.getClickedPos().relative(context.getClickedFace());
        spawnCopy(serverLevel, player, stack, pos);
        return InteractionResult.CONSUME;
    }

    /** Places an NPC identical to the one stored in the wand. */
    private static void spawnCopy(ServerLevel level, Player player, ItemStack stack, BlockPos pos) {
        CompoundTag tag = stack.getTag();
        if (tag == null) {
            return;
        }
        CustomNpc npc = com.customnpcs.registry.ModEntities.CUSTOM_NPC.get().create(level);
        if (npc == null) {
            return;
        }
        npc.moveTo(pos.getX() + 0.5D, pos.getY(), pos.getZ() + 0.5D,
                player.getYRot() + 180.0F, 0.0F);
        npc.setHome(pos);
        npc.setOwner(player.getUUID());
        npc.finalizeSpawn(level, level.getCurrentDifficultyAt(pos), MobSpawnType.SPAWN_EGG, null, null);

        if (tag.contains(TAG_NAME)) {
            npc.setCustomNameDirect(tag.getString(TAG_NAME));
        }
        if (tag.contains(TAG_SKIN)) {
            npc.setSkin(tag.getString(TAG_SKIN));
        }
        if (tag.contains(TAG_BEHAVIOUR)) {
            npc.setBehaviour(NpcBehaviour.byOrdinal(tag.getInt(TAG_BEHAVIOUR)));
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
        if (tag.contains(TAG_MAIN)) {
            npc.setItemSlot(EquipmentSlot.MAINHAND, ItemStack.of(tag.getCompound(TAG_MAIN)));
        }
        if (tag.contains(TAG_HEAD)) {
            npc.setItemSlot(EquipmentSlot.HEAD, ItemStack.of(tag.getCompound(TAG_HEAD)));
        }

        level.addFreshEntity(npc);
        player.sendSystemMessage(Component.literal("Placed " + npc.displayName())
                .withStyle(ChatFormatting.GREEN));
        if (!player.isCreative()) {
            stack.shrink(1);
        }
    }

    /** Stores everything an NPC is made of into the wand. */
    private static void copyInto(ItemStack wand, CustomNpc npc) {
        CompoundTag tag = wand.getOrCreateTag();
        tag.putString(TAG_NAME, npc.displayName());
        tag.putString(TAG_SKIN, npc.skin());
        tag.putInt(TAG_BEHAVIOUR, npc.behaviour().ordinal());
        tag.putInt(TAG_SCALE, npc.scalePercent());
        tag.putString(TAG_DIALOGUE, npc.dialogue());
        tag.putBoolean(TAG_HOSTILE, npc.isHostile());
        ItemStack main = npc.getItemBySlot(EquipmentSlot.MAINHAND);
        if (!main.isEmpty()) {
            tag.put(TAG_MAIN, main.save(new CompoundTag()));
        } else {
            tag.remove(TAG_MAIN);
        }
        ItemStack head = npc.getItemBySlot(EquipmentSlot.HEAD);
        if (!head.isEmpty()) {
            tag.put(TAG_HEAD, head.save(new CompoundTag()));
        } else {
            tag.remove(TAG_HEAD);
        }
    }

    private static boolean hasCopy(ItemStack stack) {
        return stack.hasTag() && stack.getTag() != null && stack.getTag().contains(TAG_NAME);
    }

    @Override
    public void appendHoverText(ItemStack stack, @javax.annotation.Nullable Level level,
                                List<Component> tooltip, TooltipFlag flag) {
        tooltip.add(Component.literal("Right-click an NPC to edit it")
                .withStyle(ChatFormatting.GRAY));
        tooltip.add(Component.literal("Sneak-right-click an NPC to copy it")
                .withStyle(ChatFormatting.DARK_GRAY));
        if (hasCopy(stack)) {
            tooltip.add(Component.literal("Copied: " + stack.getTag().getString(TAG_NAME))
                    .withStyle(ChatFormatting.AQUA));
            tooltip.add(Component.literal("Right-click a block to place a copy")
                    .withStyle(ChatFormatting.DARK_GRAY));
        }
    }
}
