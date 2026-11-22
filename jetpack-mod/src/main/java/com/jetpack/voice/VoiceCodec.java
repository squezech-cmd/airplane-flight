package com.jetpack.voice;

/**
 * A tiny speech codec used by the voice chat.
 *
 * <p>Microphones give us 48 kHz linear PCM, which is far more than a voice
 * needs. Three things happen here, in order:
 *
 * <ol>
 *     <li><b>Decimation</b> to 16 kHz wideband speech. Averaging three samples
 *     at a time is a crude but effective anti-alias filter.</li>
 *     <li><b>Companding</b> with the classic G.711 mu-law curve, which maps
 *     16 bit samples to one byte each. Telephony has used this since 1972 and
 *     speech still comes through clearly.</li>
 *     <li>The reverse, on the receiving side.</li>
 * </ol>
 *
 * <p>The result is 16 KB/s per speaker instead of 96 KB/s of raw audio, which
 * a normal Minecraft server can relay without breaking a sweat.
 */
public final class VoiceCodec {
    /** Samples per second after decimation. */
    public static final int SAMPLE_RATE = 16000;
    /** Microphone rate, decimated down to {@link #SAMPLE_RATE}. */
    public static final int MIC_RATE = 48000;
    public static final int DECIMATION = MIC_RATE / SAMPLE_RATE;
    /** 20 ms of audio, the size of one voice frame. */
    public static final int FRAME_SAMPLES = SAMPLE_RATE / 50;
    public static final int FRAME_BYTES = FRAME_SAMPLES;

    private static final int MU = 0xFF;
    private static final int BIAS = 0x84;
    private static final int CLIP = 32635;

    private VoiceCodec() {
    }

    // ------------------------------------------------------------- encode

    /**
     * Turns 20 ms of 48 kHz signed 16 bit little-endian PCM into 320 mu-law
     * bytes at 16 kHz.
     *
     * @param pcm48 buffer holding exactly {@code FRAME_SAMPLES * DECIMATION * 2} bytes
     * @param out   destination, must hold at least {@link #FRAME_BYTES} bytes
     */
    public static void encodeFrame(byte[] pcm48, byte[] out) {
        int source = 0;
        for (int i = 0; i < FRAME_SAMPLES; i++) {
            int sum = 0;
            for (int k = 0; k < DECIMATION; k++) {
                int lo = pcm48[source] & 0xFF;
                int hi = pcm48[source + 1];
                sum += (hi << 8) | lo;
                source += 2;
            }
            int sample = sum / DECIMATION;
            sample = Math.max(-32768, Math.min(32767, sample));
            out[i] = (byte) linearToMuLaw(sample);
        }
    }

    public static int linearToMuLaw(int sample) {
        int sign = (sample >> 8) & 0x80;
        if (sign != 0) {
            sample = -sample;
        }
        if (sample > CLIP) {
            sample = CLIP;
        }
        sample += BIAS;
        int exponent = 7;
        for (int mask = 0x4000; (sample & mask) == 0 && exponent > 0; mask >>= 1) {
            exponent--;
        }
        int mantissa = (sample >> (exponent + 3)) & 0x0F;
        return ~(sign | (exponent << 4) | mantissa) & MU;
    }

    // ------------------------------------------------------------- decode

    /** Expands mu-law bytes back into 16 kHz signed 16 bit little-endian PCM. */
    public static void decodeFrame(byte[] muLaw, int length, byte[] out) {
        for (int i = 0; i < length; i++) {
            int sample = muLawToLinear(muLaw[i] & 0xFF);
            out[i * 2] = (byte) (sample & 0xFF);
            out[i * 2 + 1] = (byte) ((sample >> 8) & 0xFF);
        }
    }

    public static int muLawToLinear(int muLaw) {
        int value = (~muLaw) & MU;
        int sign = value & 0x80;
        int exponent = (value >> 4) & 0x07;
        int mantissa = value & 0x0F;
        int sample = ((mantissa << 3) + BIAS) << exponent;
        sample -= BIAS;
        return sign != 0 ? -sample : sample;
    }

    // --------------------------------------------------------------- levels

    /** Root-mean-square of a mu-law frame, 0..1, for the level meter and VAD. */
    public static float frameLevel(byte[] muLaw, int length) {
        if (length <= 0) {
            return 0.0F;
        }
        double sum = 0.0D;
        for (int i = 0; i < length; i++) {
            double normalised = muLawToLinear(muLaw[i] & 0xFF) / 32768.0D;
            sum += normalised * normalised;
        }
        return (float) Math.sqrt(sum / length);
    }

    /** RMS of raw 48 kHz PCM, used by the microphone meter while recording. */
    public static float pcmLevel(byte[] pcm, int length) {
        if (length < 2) {
            return 0.0F;
        }
        double sum = 0.0D;
        int samples = length / 2;
        for (int i = 0; i < samples; i++) {
            int lo = pcm[i * 2] & 0xFF;
            int hi = pcm[i * 2 + 1];
            double value = ((hi << 8) | lo) / 32768.0D;
            sum += value * value;
        }
        return (float) Math.sqrt(sum / samples);
    }
}
