package com.jetpack.network;

import com.jetpack.ThrustState;
import net.minecraft.network.FriendlyByteBuf;
import net.minecraft.server.level.ServerPlayer;
import net.minecraftforge.network.NetworkEvent;

import java.util.function.Supplier;

public class ThrustPacket {
    private final boolean thrusting;

    public ThrustPacket(boolean thrusting) {
        this.thrusting = thrusting;
    }

    public ThrustPacket(FriendlyByteBuf buf) {
        this.thrusting = buf.readBoolean();
    }

    public void encode(FriendlyByteBuf buf) {
        buf.writeBoolean(thrusting);
    }

    public void handle(Supplier<NetworkEvent.Context> ctxSupplier) {
        NetworkEvent.Context ctx = ctxSupplier.get();
        ctx.enqueueWork(() -> {
            ServerPlayer player = ctx.getSender();
            if (player != null) {
                ThrustState.setThrusting(player, thrusting);
            }
        });
        ctx.setPacketHandled(true);
    }
}
