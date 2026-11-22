package com.jetpack.voice;

/**
 * Server wide voice chat switches. These are the ones an admin cares about;
 * everything else lives in the player's own {@link VoiceSettings}.
 */
public final class VoiceConfig {
    /** Master switch for the whole voice chat. */
    public static boolean enabled = true;

    /** Default hearing range, in blocks, for players who have not chosen one. */
    public static int defaultRange = 32;

    /** Hard limits for the per-player range setting. */
    public static final int MIN_RANGE = 8;
    public static final int MAX_RANGE = 128;

    /** How many voice frames a second the server will relay per player. */
    public static final int MAX_FRAMES_PER_SECOND = 60;

    private VoiceConfig() {
    }

    public static int clampRange(int range) {
        return Math.max(MIN_RANGE, Math.min(MAX_RANGE, range));
    }
}
