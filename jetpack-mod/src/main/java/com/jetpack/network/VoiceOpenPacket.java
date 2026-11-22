package com.jetpack.network;

import net.minecraft.network.FriendlyByteBuf;
import net.minecraftforge.network.NetworkEvent;

import java.util.function.Supplier;

/** Server to client: "open the voice settings screen". */
public class VoiceOpenPacket {
    public VoiceOpenPacket() {
    }

    public VoiceOpenPacket(FriendlyByteBuf buf) {
    }

    public void encode(FriendlyByteBuf buf) {
    }

    public void handle(Supplier<NetworkEvent.Context> contextSupplier) {
        NetworkEvent.Context context = contextSupplier.get();
        context.enqueueWork(() -> net.minecraftforge.fml.DistExecutor.unsafeRunWhenOn(
                net.minecraftforge.api.distmarker.Dist.CLIENT, () -> VoiceOpenPacket::open));
        context.setPacketHandled(true);
    }

    private static void open() {
        com.jetpack.voice.VoiceClient.openScreen();
    }
}
