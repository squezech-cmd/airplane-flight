package com.jetpack.voice;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import com.jetpack.JetpackMod;
import net.minecraft.client.Minecraft;

import java.io.IOException;
import java.io.Reader;
import java.io.Writer;
import java.nio.file.Files;
import java.nio.file.Path;

/**
 * The player's own voice chat preferences, stored in
 * {@code config/jetpack-voice.json} so they survive a restart.
 *
 * <p>These are all client side: the server never needs to know which
 * microphone you use, only whether it is on and how far you want to be heard.
 */
public final class VoiceSettings {
    private static final Gson GSON = new GsonBuilder().setPrettyPrinting().create();
    private static final String FILE_NAME = "jetpack-voice.json";
    private static VoiceSettings instance;

    // ---- transmitted to the server -------------------------------------
    /** Master switch for the whole feature on this client. */
    public boolean enabled = false;
    /** True while the push-to-talk key is held, or while open mic is on. */
    public boolean microphoneOn = false;
    /** Hearing range in blocks, 8..128. */
    public int range = 32;

    // ---- local only ----------------------------------------------------
    /** Push to talk instead of an always-open microphone. */
    public boolean pushToTalk = true;
    /** Keyboard key code used for push to talk, defaults to V. */
    public int pushToTalkKey = 86;
    /** Extra gain applied to the microphone, 0.25..3.0. */
    public float inputGain = 1.0F;
    /** Volume of other players, 0.0..1.0. */
    public float outputVolume = 0.8F;
    /** Noise gate: frames quieter than this are never sent. */
    public float noiseGate = 0.02F;
    /** Preferred capture device, empty means "system default". */
    public String inputDevice = "";
    /** Play your own microphone back so you can hear how you sound. */
    public boolean selfMonitor = false;
    /** Show a compact indicator with the speakers near you. */
    public boolean showHud = true;

    private VoiceSettings() {
    }

    public static VoiceSettings get() {
        if (instance == null) {
            instance = load();
        }
        return instance;
    }

    private static Path path() {
        return Minecraft.getInstance().gameDirectory.toPath().resolve("config").resolve(FILE_NAME);
    }

    private static VoiceSettings load() {
        Path path = path();
        if (Files.exists(path)) {
            try (Reader reader = Files.newBufferedReader(path)) {
                VoiceSettings loaded = GSON.fromJson(reader, VoiceSettings.class);
                if (loaded != null) {
                    loaded.sanitise();
                    return loaded;
                }
            } catch (Exception exception) {
                JetpackMod.LOGGER.warn("Could not read {}; starting voice settings from defaults",
                        path, exception);
            }
        }
        return new VoiceSettings();
    }

    public void sanitise() {
        this.range = VoiceConfig.clampRange(this.range);
        this.inputGain = clamp(this.inputGain, 0.25F, 3.0F);
        this.outputVolume = clamp(this.outputVolume, 0.0F, 1.0F);
        this.noiseGate = clamp(this.noiseGate, 0.0F, 0.5F);
        if (this.inputDevice == null) {
            this.inputDevice = "";
        }
        if (this.pushToTalkKey <= 0) {
            this.pushToTalkKey = 86;
        }
    }

    private static float clamp(float value, float min, float max) {
        return Math.max(min, Math.min(max, value));
    }

    public void save() {
        sanitise();
        Path path = path();
        try {
            Files.createDirectories(path.getParent());
            try (Writer writer = Files.newBufferedWriter(path)) {
                GSON.toJson(this, writer);
            }
        } catch (IOException exception) {
            JetpackMod.LOGGER.warn("Could not save voice settings to {}", path, exception);
        }
    }

    /** Copies what the server needs into the settings packet. */
    public boolean transmits() {
        return this.enabled && this.microphoneOn;
    }
}
