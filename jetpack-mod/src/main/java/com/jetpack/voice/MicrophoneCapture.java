package com.jetpack.voice;

import com.jetpack.JetpackMod;
import com.jetpack.network.NetworkHandler;
import com.jetpack.network.VoiceFramePacket;
import net.minecraft.client.Minecraft;

import javax.sound.sampled.AudioFormat;
import javax.sound.sampled.AudioSystem;
import javax.sound.sampled.DataLine;
import javax.sound.sampled.Mixer;
import javax.sound.sampled.SourceDataLine;
import javax.sound.sampled.TargetDataLine;
import java.util.ArrayList;
import java.util.List;

/**
 * Records the microphone on a background thread, encodes each 20 ms frame to
 * mu-law and hands it to the network layer.
 *
 * <p>Everything here is plain {@code javax.sound.sampled}; no native libraries
 * and no OpenAL tricks, so it works on Windows, macOS and Linux alike.
 */
public final class MicrophoneCapture implements Runnable {
    private static volatile MicrophoneCapture instance;

    private final VoiceSettings settings;
    private final byte[] pcm = new byte[VoiceCodec.FRAME_SAMPLES * VoiceCodec.DECIMATION * 2];
    private final byte[] encoded = new byte[VoiceCodec.FRAME_BYTES];
    private volatile boolean running;
    private volatile float lastLevel;
    private Thread thread;
    private TargetDataLine line;
    private SourceDataLine monitor;
    private String status = "idle";

    private MicrophoneCapture(VoiceSettings settings) {
        this.settings = settings;
    }

    public static synchronized void start() {
        if (instance != null) {
            if (instance.running) {
                return;
            }
            instance.running = false; // let a stopped thread unwind first
        }
        MicrophoneCapture capture = new MicrophoneCapture(VoiceSettings.get());
        capture.running = true;
        capture.thread = new Thread(capture, "Jetpack-Voice-Capture");
        capture.thread.setDaemon(true);
        capture.thread.setPriority(Thread.NORM_PRIORITY + 1);
        capture.thread.start();
        instance = capture;
    }

    public static synchronized void stop() {
        MicrophoneCapture capture = instance;
        instance = null;
        if (capture != null) {
            capture.running = false;
        }
    }

    public static boolean isRunning() {
        MicrophoneCapture capture = instance;
        return capture != null && capture.running;
    }

    public static float level() {
        MicrophoneCapture capture = instance;
        return capture == null ? 0.0F : capture.lastLevel;
    }

    public static String status() {
        MicrophoneCapture capture = instance;
        return capture == null ? "off" : capture.status;
    }

    /** Lists capture devices so the settings screen can offer a picker. */
    public static List<String> devices() {
        List<String> names = new ArrayList<>();
        names.add(""); // system default
        try {
            AudioFormat format = format();
            DataLine.Info info = new DataLine.Info(TargetDataLine.class, format);
            for (Mixer.Info mixerInfo : AudioSystem.getMixerInfo()) {
                Mixer mixer = AudioSystem.getMixer(mixerInfo);
                if (mixer.isLineSupported(info)) {
                    names.add(mixerInfo.getName());
                }
            }
        } catch (Exception exception) {
            JetpackMod.LOGGER.warn("Could not list microphones", exception);
        }
        return names;
    }

    private static AudioFormat format() {
        return new AudioFormat(AudioFormat.Encoding.PCM_SIGNED,
                VoiceCodec.MIC_RATE, 16, 1, 2, VoiceCodec.MIC_RATE, false);
    }

    @Override
    public void run() {
        try {
            open();
            this.status = "listening";
            while (this.running) {
                if (!shouldTransmit()) {
                    // keep draining the line so the buffer never overflows and
                    // the next word is not half a second late
                    if (this.line.available() > 0) {
                        this.line.skip(this.line.available());
                    }
                    sleepQuietly(15);
                    continue;
                }
                int read = this.line.read(this.pcm, 0, this.pcm.length);
                if (read < this.pcm.length) {
                    continue;
                }
                float rawLevel = VoiceCodec.pcmLevel(this.pcm, this.pcm.length);
                this.lastLevel = Math.min(1.0F, rawLevel * this.settings.inputGain);
                if (this.monitor != null) {
                    this.monitor.write(this.pcm, 0, this.pcm.length);
                }
                if (this.lastLevel < this.settings.noiseGate) {
                    continue;
                }
                applyGain(this.pcm, this.settings.inputGain);
                VoiceCodec.encodeFrame(this.pcm, this.encoded);

                Minecraft minecraft = Minecraft.getInstance();
                if (minecraft.player == null) {
                    continue;
                }
                byte[] payload = this.encoded.clone();
                VoiceFramePacket packet = new VoiceFramePacket(
                        minecraft.player.getUUID(),
                        minecraft.player.getGameProfile().getName(),
                        payload,
                        this.lastLevel);
                NetworkHandler.INSTANCE.sendToServer(packet);
            }
        } catch (Exception exception) {
            this.status = "error: " + exception.getMessage();
            JetpackMod.LOGGER.warn("Voice capture stopped: {}", exception.toString());
        } finally {
            close();
        }
    }

    private boolean shouldTransmit() {
        VoiceSettings current = VoiceSettings.get();
        if (!current.enabled || !current.microphoneOn) {
            return false;
        }
        Minecraft minecraft = Minecraft.getInstance();
        return minecraft.player != null && !minecraft.isPaused();
    }

    private void open() throws Exception {
        AudioFormat format = format();
        DataLine.Info info = new DataLine.Info(TargetDataLine.class, format);
        String wanted = this.settings.inputDevice;
        if (wanted != null && !wanted.isEmpty()) {
            for (Mixer.Info mixerInfo : AudioSystem.getMixerInfo()) {
                Mixer mixer = AudioSystem.getMixer(mixerInfo);
                if (mixerInfo.getName().equals(wanted) && mixer.isLineSupported(info)) {
                    this.line = (TargetDataLine) mixer.getLine(info);
                    break;
                }
            }
        }
        if (this.line == null) {
            this.line = (TargetDataLine) AudioSystem.getLine(info);
        }
        this.line.open(format, this.pcm.length * 4);
        this.line.start();

        if (this.settings.selfMonitor) {
            DataLine.Info outInfo = new DataLine.Info(SourceDataLine.class, format);
            this.monitor = (SourceDataLine) AudioSystem.getLine(outInfo);
            this.monitor.open(format, this.pcm.length * 4);
            this.monitor.start();
        }
    }

    private void close() {
        if (this.line != null) {
            try {
                this.line.stop();
                this.line.close();
            } catch (Exception ignored) {
                // shutting down anyway
            }
            this.line = null;
        }
        if (this.monitor != null) {
            try {
                this.monitor.stop();
                this.monitor.close();
            } catch (Exception ignored) {
                // shutting down anyway
            }
            this.monitor = null;
        }
        this.status = "closed";
    }

    private static void applyGain(byte[] pcm, float gain) {
        if (Math.abs(gain - 1.0F) < 0.01F) {
            return;
        }
        for (int i = 0; i < pcm.length; i += 2) {
            int lo = pcm[i] & 0xFF;
            int hi = pcm[i + 1];
            int sample = (hi << 8) | lo;
            sample = Math.max(-32768, Math.min(32767, Math.round(sample * gain)));
            pcm[i] = (byte) (sample & 0xFF);
            pcm[i + 1] = (byte) ((sample >> 8) & 0xFF);
        }
    }

    private static void sleepQuietly(long millis) {
        try {
            Thread.sleep(millis);
        } catch (InterruptedException interrupted) {
            Thread.currentThread().interrupt();
        }
    }
}
