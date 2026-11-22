package com.jetpack.network;

import net.minecraft.network.FriendlyByteBuf;
import net.minecraftforge.network.NetworkEvent;

import java.util.List;
import java.util.function.Supplier;

/**
 * Server to client: who is in hearing range right now.
 *
 * <p>Used for the "nearby" list in the settings screen and for the on-screen
 * list of people you can talk to. Sent once a second.
 */
public class VoicePeersPacket {
    public record Peer(String name, int distance, float level, boolean speaking) {
    }

    private final int range;
    private final List<Peer> peers;

    public VoicePeersPacket(int range, List<Peer> peers) {
        this.range = range;
        this.peers = peers;
    }

    public VoicePeersPacket(FriendlyByteBuf buf) {
        this.range = buf.readVarInt();
        int count = buf.readVarInt();
        this.peers = new java.util.ArrayList<>(count);
        for (int i = 0; i < count; i++) {
            this.peers.add(new Peer(buf.readUtf(32), buf.readVarInt(), buf.readFloat(), buf.readBoolean()));
        }
    }

    public void encode(FriendlyByteBuf buf) {
        buf.writeVarInt(this.range);
        buf.writeVarInt(this.peers.size());
        for (Peer peer : this.peers) {
            buf.writeUtf(peer.name(), 32);
            buf.writeVarInt(peer.distance());
            buf.writeFloat(peer.level());
            buf.writeBoolean(peer.speaking());
        }
    }

    public void handle(Supplier<NetworkEvent.Context> contextSupplier) {
        NetworkEvent.Context context = contextSupplier.get();
        context.enqueueWork(() -> net.minecraftforge.fml.DistExecutor.unsafeRunWhenOn(
                net.minecraftforge.api.distmarker.Dist.CLIENT, () -> () -> dispatch(this)));
        context.setPacketHandled(true);
    }

    private static void dispatch(VoicePeersPacket packet) {
        com.jetpack.voice.VoiceClient.acceptPeers(packet);
    }

    public int getRange() {
        return this.range;
    }

    public List<Peer> getPeers() {
        return this.peers;
    }
}
