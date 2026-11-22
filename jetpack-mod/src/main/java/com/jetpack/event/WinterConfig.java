package com.jetpack.event;

/**
 * Small runtime switchboard the commands flip. Kept separate so the tick
 * handlers stay readable.
 */
public final class WinterConfig {
    /** Ambient snowfall + ground frost around players. */
    public static boolean snowfallEnabled = true;
    /** Set by the command to run a storm right now. */
    public static long forcedStormStart = -1L;
    /** How long a forced storm lasts, in ticks. */
    public static int forcedStormLength = 20 * 45;
    /** Storms scheduled automatically every few minutes. */
    public static boolean autoStorms = true;

    private WinterConfig() {
    }
}
