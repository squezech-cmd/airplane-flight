package com.customnpcs.network;

import net.minecraft.network.FriendlyByteBuf;
import net.minecraftforge.network.NetworkEvent;

import java.util.function.Supplier;

/** Server to client: "open the editor for NPC #id". */
public class OpenEditorPacket {
    private final int entityId;

    public OpenEditorPacket(int entityId) {
        this.entityId = entityId;
    }

    public OpenEditorPacket(FriendlyByteBuf buf) {
        this.entityId = buf.readVarInt();
    }

    public void encode(FriendlyByteBuf buf) {
        buf.writeVarInt(this.entityId);
    }

    public int getEntityId() {
        return this.entityId;
    }

    public void handle(Supplier<NetworkEvent.Context> contextSupplier) {
        NetworkEvent.Context context = contextSupplier.get();
        context.enqueueWork(() -> net.minecraftforge.fml.DistExecutor.unsafeRunWhenOn(
                net.minecraftforge.api.distmarker.Dist.CLIENT, () -> () -> open(this.entityId)));
        context.setPacketHandled(true);
    }

    private static void open(int entityId) {
        com.customnpcs.client.ClientPacketHandler.openEditor(entityId);
    }
}
