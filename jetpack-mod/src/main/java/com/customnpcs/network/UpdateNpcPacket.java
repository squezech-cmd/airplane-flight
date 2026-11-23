package com.customnpcs.network;

import com.customnpcs.entity.CustomNpc;
import com.customnpcs.entity.NpcBehaviour;
import net.minecraft.nbt.CompoundTag;
import net.minecraft.network.FriendlyByteBuf;
import net.minecraft.network.chat.Component;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.EquipmentSlot;
import net.minecraft.world.item.ItemStack;
import net.minecraftforge.network.NetworkEvent;

import java.util.function.Supplier;

/**
 * Client to server: the edited values for one NPC.
 *
 * <p>The server is the authority: it checks that the sender is allowed to edit
 * this NPC before applying anything, and clamps the scale so a malformed packet
 * cannot create a giant.
 */
public class UpdateNpcPacket {
    public static final int FLAG_INVULNERABLE = 1;
    public static final int FLAG_HOSTILE = 2;

    private final int entityId;
    private final String name;
    private final String dialogue;
    private final String skin;
    private final int behaviour;
    private final int scalePercent;
    private final int flags;
    private final ItemStack mainHand;
    private final ItemStack head;

    public UpdateNpcPacket(int entityId, String name, String dialogue, String skin,
                           NpcBehaviour behaviour, int scalePercent, boolean invulnerable,
                           boolean hostile, ItemStack mainHand, ItemStack head) {
        this.entityId = entityId;
        this.name = name;
        this.dialogue = dialogue;
        this.skin = skin;
        this.behaviour = behaviour.ordinal();
        this.scalePercent = scalePercent;
        this.flags = (invulnerable ? FLAG_INVULNERABLE : 0) | (hostile ? FLAG_HOSTILE : 0);
        this.mainHand = mainHand;
        this.head = head;
    }

    public UpdateNpcPacket(FriendlyByteBuf buf) {
        this.entityId = buf.readVarInt();
        this.name = buf.readUtf(32);
        this.dialogue = buf.readUtf(256);
        this.skin = buf.readUtf(96);
        this.behaviour = buf.readVarInt();
        this.scalePercent = buf.readVarInt();
        this.flags = buf.readVarInt();
        CompoundTag tag = buf.readNbt();
        this.mainHand = tag != null && tag.contains("MainHand")
                ? ItemStack.of(tag.getCompound("MainHand")) : ItemStack.EMPTY;
        this.head = tag != null && tag.contains("Head")
                ? ItemStack.of(tag.getCompound("Head")) : ItemStack.EMPTY;
    }

    public void encode(FriendlyByteBuf buf) {
        buf.writeVarInt(this.entityId);
        buf.writeUtf(this.name, 32);
        buf.writeUtf(this.dialogue, 256);
        buf.writeUtf(this.skin, 96);
        buf.writeVarInt(this.behaviour);
        buf.writeVarInt(this.scalePercent);
        buf.writeVarInt(this.flags);
        CompoundTag tag = new CompoundTag();
        if (!this.mainHand.isEmpty()) {
            tag.put("MainHand", this.mainHand.save(new CompoundTag()));
        }
        if (!this.head.isEmpty()) {
            tag.put("Head", this.head.save(new CompoundTag()));
        }
        buf.writeNbt(tag);
    }

    public void handle(Supplier<NetworkEvent.Context> contextSupplier) {
        NetworkEvent.Context context = contextSupplier.get();
        context.enqueueWork(() -> {
            ServerPlayer sender = context.getSender();
            if (sender == null) {
                return;
            }
            Entity entity = sender.level().getEntity(this.entityId);
            if (!(entity instanceof CustomNpc npc)) {
                return;
            }
            if (!npc.isOwnerOrOp(sender)) {
                return;
            }
            npc.setCustomNameDirect(this.name.isBlank() ? "NPC" : this.name);
            npc.setDialogue(this.dialogue);
            npc.setSkin(this.skin);
            npc.setBehaviour(NpcBehaviour.byOrdinal(this.behaviour));
            npc.setScalePercent(Math.max(50, Math.min(200, this.scalePercent)));
            npc.setInvulnerableNpc((this.flags & FLAG_INVULNERABLE) != 0);
            npc.setHostile((this.flags & FLAG_HOSTILE) != 0);
            npc.setItemSlot(EquipmentSlot.MAINHAND, this.mainHand);
            npc.setItemSlot(EquipmentSlot.HEAD, this.head);
            sender.sendSystemMessage(Component.literal("Saved " + npc.displayName()));
        });
        context.setPacketHandled(true);
    }
}
