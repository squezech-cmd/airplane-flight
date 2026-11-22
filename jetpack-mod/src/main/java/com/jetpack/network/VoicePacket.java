package com.jetpack.network;

import net.minecraft.network.FriendlyByteBuf;
import net.minecraftforge.network.NetworkEvent;

import java.util.ArrayList;
import java.util.List;
import java.util.UUID;
import java.util.function.Supplier;

/**
 * Server to client: one or more speakers' 20 ms of audio.
 *
 * <p>Every frame is stamped with the speaker's UUID and the RMS level of that
 * frame, so the receiving side can draw a live level meter without decoding
 * anything. Frames that arrive while a batch is still open are appended and
 * sent together, which keeps the packet rate around 25 per second even when
 * eight people are talking at once.
 */
public class VoicePacket {
    private static final int MAX_FRAMES = 16;

    private final UUID speaker;
    private final String speakerName;
    private final List<byte[]> frames = new ArrayList<>();
    private final List<Float> levels = new ArrayList<>();

    public VoicePacket(UUID speaker, String speakerName, byte[] frame, float level) {
        this.speaker = speaker;
        this.speakerName = speakerName;
        addFrame(frame, level);
    }

    private VoicePacket(UUID speaker, String speakerName) {
        this.speaker = speaker;
        this.speakerName = speakerName;
    }

    public VoicePacket(FriendlyByteBuf buf) {
        this.speaker = buf.readUUID();
        this.speakerName = buf.readUtf(32);
        int count = Math.min(MAX_FRAMES, buf.readVarInt());
        for (int i = 0; i < count; i++) {
            this.frames.add(buf.readByteArray(1024));
            this.levels.add(buf.readFloat());
        }
    }

    public void encode(FriendlyByteBuf buf) {
        buf.writeUUID(this.speaker);
        buf.writeUtf(this.speakerName, 32);
        buf.writeVarInt(this.frames.size());
        for (int i = 0; i < this.frames.size(); i++) {
            buf.writeByteArray(this.frames.get(i));
            buf.writeFloat(this.levels.get(i));
        }
    }

    /** Appends audio for the same speaker. Callers drop the packet when this returns false. */
    public boolean merge(VoicePacket other) {
        if (!this.speaker.equals(other.speaker) || this.frames.size() >= MAX_FRAMES) {
            return false;
        }
        this.frames.addAll(other.frames);
        this.levels.addAll(other.levels);
        return true;
    }

    public boolean isEmpty() {
        return this.frames.isEmpty();
    }

    private void addFrame(byte[] frame, float level) {
        this.frames.add(frame);
        this.levels.add(level);
    }

    public UUID getSpeaker() {
        return this.speaker;
    }

    public String getSpeakerName() {
        return this.speakerName;
    }

    public List<byte[]> getFrames() {
        return this.frames;
    }

    public List<Float> getLevels() {
        return this.levels;
    }

    public void handle(Supplier<NetworkEvent.Context> contextSupplier) {
        NetworkEvent.Context context = contextSupplier.get();
        context.enqueueWork(() -> net.minecraftforge.fml.DistExecutor.unsafeRunWhenOn(
                net.minecraftforge.api.distmarker.Dist.CLIENT, () -> () -> dispatch(this)));
        context.setPacketHandled(true);
    }

    /** Only ever called on a physical client. */
    private static void dispatch(VoicePacket packet) {
        com.jetpack.voice.VoiceClient.accept(packet);
    }
}
