package com.jetpack.network;

import net.minecraft.network.FriendlyByteBuf;
import net.minecraft.server.level.ServerPlayer;
import net.minecraftforge.network.NetworkEvent;
import net.minecraftforge.network.PacketDistributor;

import java.util.List;
import java.util.UUID;
import java.util.function.Supplier;

/**
 * Client to server: 20 ms of microphone audio, already mu-law encoded.
 *
 * <p>This is the only voice packet a client ever sends. The server validates it
 * (microphone enabled, sane size), finds every listener in hearing range and
 * fans the audio out as {@link VoicePacket}s.
 */
public class VoiceFramePacket {
    private final UUID speaker;
    private final String speakerName;
    private final byte[] frame;
    private final float level;

    public VoiceFramePacket(UUID speaker, String speakerName, byte[] frame, float level) {
        this.speaker = speaker;
        this.speakerName = speakerName;
        this.frame = frame;
        this.level = level;
    }

    public VoiceFramePacket(FriendlyByteBuf buf) {
        this.speaker = buf.readUUID();
        this.speakerName = buf.readUtf(32);
        this.frame = buf.readByteArray(1024);
        this.level = buf.readFloat();
    }

    public void encode(FriendlyByteBuf buf) {
        buf.writeUUID(this.speaker);
        buf.writeUtf(this.speakerName, 32);
        buf.writeByteArray(this.frame);
        buf.writeFloat(this.level);
    }

    public void handle(Supplier<NetworkEvent.Context> contextSupplier) {
        NetworkEvent.Context context = contextSupplier.get();
        context.enqueueWork(() -> {
            ServerPlayer sender = context.getSender();
            if (sender == null || !sender.getUUID().equals(this.speaker)) {
                return; // never trust a client that speaks for somebody else
            }
            if (!com.jetpack.voice.VoiceState.maySpeak(sender)) {
                return; // muted, or voice chat switched off
            }
            if (this.frame.length == 0 || this.frame.length > 1024) {
                return;
            }

            List<ServerPlayer> listeners = com.jetpack.voice.VoiceState.listenersFor(sender);
            for (ServerPlayer listener : listeners) {
                NetworkHandler.INSTANCE.send(
                        PacketDistributor.PLAYER.with(() -> listener),
                        new VoicePacket(sender.getUUID(), sender.getGameProfile().getName(),
                                this.frame, this.level));
            }
        });
        context.setPacketHandled(true);
    }
}
