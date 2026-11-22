package com.jetpack.network;

import net.minecraft.network.FriendlyByteBuf;
import net.minecraft.server.level.ServerPlayer;
import net.minecraftforge.network.NetworkEvent;

import java.util.function.Supplier;

/**
 * Client to server: "my voice settings changed".
 *
 * <p>Only the parts the server has to know are sent: whether the microphone is
 * on, and how far the player wants to be heard. Everything else (input device,
 * microphone gain, output volume) is local and is saved by the client.
 */
public class VoiceSettingsPacket {
    private final boolean microphoneOn;
    private final int range;

    public VoiceSettingsPacket(boolean microphoneOn, int range) {
        this.microphoneOn = microphoneOn;
        this.range = range;
    }

    public VoiceSettingsPacket(FriendlyByteBuf buf) {
        this.microphoneOn = buf.readBoolean();
        this.range = buf.readVarInt();
    }

    public void encode(FriendlyByteBuf buf) {
        buf.writeBoolean(this.microphoneOn);
        buf.writeVarInt(this.range);
    }

    public void handle(Supplier<NetworkEvent.Context> contextSupplier) {
        NetworkEvent.Context context = contextSupplier.get();
        context.enqueueWork(() -> {
            ServerPlayer sender = context.getSender();
            if (sender != null) {
                // client to server: apply as that player's settings
                com.jetpack.voice.VoiceState.applySettings(sender, this.microphoneOn, this.range);
            } else {
                // server to client: remember what the server believes
                net.minecraftforge.fml.DistExecutor.unsafeRunWhenOn(
                        net.minecraftforge.api.distmarker.Dist.CLIENT,
                        () -> () -> com.jetpack.voice.VoiceClient.onServerSettings(this));
            }
        });
        context.setPacketHandled(true);
    }
}
