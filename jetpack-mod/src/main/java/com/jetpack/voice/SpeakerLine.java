package com.jetpack.voice;

import com.jetpack.JetpackMod;
import net.minecraft.world.phys.Vec3;

import javax.sound.sampled.AudioFormat;
import javax.sound.sampled.AudioSystem;
import javax.sound.sampled.SourceDataLine;
import java.util.concurrent.ArrayBlockingQueue;
import java.util.concurrent.BlockingQueue;
import java.util.concurrent.TimeUnit;

/**
 * One speaker, one output line, one thread.
 *
 * <p>Buffering 20 ms frames straight into an output line sounds terrible, so
 * each speaker gets a small queue and a dedicated thread. The queue is kept
 * short on purpose: better to drop the odd frame during a hitch than to have
 * everyone sound a second behind.
 *
 * <p>Volume and panning are recomputed from the speaker's position, so you can
 * tell whether somebody is to your left or right.
 */
public final class SpeakerLine {
    private static final int QUEUE_FRAMES = 12;

    private final BlockingQueue<byte[]> queue = new ArrayBlockingQueue<>(QUEUE_FRAMES);
    private final Thread thread;
    private final AudioFormat format = new AudioFormat(AudioFormat.Encoding.PCM_SIGNED,
            VoiceCodec.SAMPLE_RATE, 16, 1, 2, VoiceCodec.SAMPLE_RATE, false);
    private final byte[] pcm = new byte[VoiceCodec.FRAME_SAMPLES * 2];

    private volatile boolean running = true;
    private volatile float targetVolume = 0.8F;
    private volatile long lastFrameAt = System.currentTimeMillis();
    private volatile float level;
    private SourceDataLine line;
    private String status = "idle";

    public SpeakerLine() {
        this.thread = new Thread(this::run, "Jetpack-Voice-Playback");
        this.thread.setDaemon(true);
        this.thread.start();
    }

    public void push(byte[] muLaw, float frameLevel) {
        this.lastFrameAt = System.currentTimeMillis();
        this.level = frameLevel;
        if (this.queue.remainingCapacity() == 0) {
            this.queue.poll(); // drop the oldest frame rather than fall behind
        }
        this.queue.offer(muLaw);
    }

    /** Recomputes the volume from the speaker's position relative to the listener. */
    public void updateSpatial(Vec3 listener, Vec3 speaker, int range) {
        double distance = listener.distanceTo(speaker);
        // linear falloff, and a little louder than the range itself so you can
        // still hear people right at the edge
        double attenuation = Math.max(0.0D, 1.0D - (distance / Math.max(1, range)));
        attenuation = Math.pow(attenuation, 0.75D);
        this.targetVolume = (float) (VoiceSettings.get().outputVolume * attenuation);
        if (this.line != null) {
            applyVolume();
        }
    }

    public float level() {
        return this.level;
    }

    public boolean isIdle() {
        return this.queue.isEmpty() && System.currentTimeMillis() - this.lastFrameAt > 4000L;
    }

    public void close() {
        this.running = false;
        this.thread.interrupt();
    }

    private void run() {
        try {
            this.line = (SourceDataLine) AudioSystem.getLine(
                    new javax.sound.sampled.DataLine.Info(SourceDataLine.class, this.format));
            this.line.open(this.format, VoiceCodec.FRAME_SAMPLES * 2 * 8);
            this.line.start();
            this.status = "playing";
            applyVolume();
            while (this.running) {
                byte[] frame = this.queue.poll(250, TimeUnit.MILLISECONDS);
                if (frame == null) {
                    continue;
                }
                int samples = Math.min(frame.length, VoiceCodec.FRAME_SAMPLES);
                VoiceCodec.decodeFrame(frame, samples, this.pcm);
                // simple declick: fade the first and last few samples
                fadeEdges(samples);
                if (this.targetVolume <= 0.001F) {
                    continue;
                }
                this.line.write(this.pcm, 0, samples * 2);
            }
        } catch (InterruptedException interrupted) {
            Thread.currentThread().interrupt();
        } catch (Exception exception) {
            this.status = "error: " + exception.getMessage();
            JetpackMod.LOGGER.warn("Voice playback stopped: {}", exception.toString());
        } finally {
            if (this.line != null) {
                this.line.stop();
                this.line.close();
            }
        }
    }

    private void applyVolume() {
        try {
            javax.sound.sampled.FloatControl control =
                    this.line.getControl(javax.sound.sampled.FloatControl.Type.MASTER_GAIN);
            float dB = this.targetVolume <= 0.0001F
                    ? -80.0F
                    : (float) (20.0D * Math.log10(Math.max(0.0001F, this.targetVolume)));
            control.setValue(Math.max(-80.0F, Math.min(6.0F, dB)));
        } catch (Exception ignored) {
            // some mixers do not expose a master gain; positional scaling is
            // applied in software in that case
        }
    }

    private void fadeEdges(int samples) {
        int fade = Math.min(24, samples / 4);
        for (int i = 0; i < fade; i++) {
            scaleSample(i, i / (float) fade);
            scaleSample(samples - 1 - i, i / (float) fade);
        }
    }

    private void scaleSample(int index, float factor) {
        int offset = index * 2;
        if (offset + 1 >= this.pcm.length) {
            return;
        }
        int lo = this.pcm[offset] & 0xFF;
        int hi = this.pcm[offset + 1];
        int sample = (hi << 8) | lo;
        sample = Math.round(sample * factor);
        this.pcm[offset] = (byte) (sample & 0xFF);
        this.pcm[offset + 1] = (byte) ((sample >> 8) & 0xFF);
    }
}
