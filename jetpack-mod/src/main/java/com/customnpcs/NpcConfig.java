package com.customnpcs;

import net.minecraftforge.common.ForgeConfigSpec;
import net.minecraftforge.fml.ModLoadingContext;
import net.minecraftforge.fml.config.ModConfig;

/**
 * Server-side settings in {@code config/customnpcs-common.toml}.
 *
 * <p>These are the values a server owner needs to tune once, not per NPC.
 * Everything that belongs to a single character lives in the editor menu and
 * in the world save; this file is only about the rules of the whole server.
 */
public final class NpcConfig {
    private static final ForgeConfigSpec.Builder BUILDER = new ForgeConfigSpec.Builder();

    public static final ForgeConfigSpec.BooleanValue ALLOW_EDITING = BUILDER
            .comment("Whether players may open the NPC editor with the wand.",
                    "Turn this off on a public server and keep editing to operators only.")
            .define("allowEditing", true);

    public static final ForgeConfigSpec.BooleanValue OWNER_ONLY_EDITING = BUILDER
            .comment("Only the player who placed an NPC may edit it.",
                    "Operators can always edit everything.")
            .define("ownerOnlyEditing", true);

    public static final ForgeConfigSpec.IntValue DEFAULT_SCALE = BUILDER
            .comment("Default size of a newly placed NPC, in percent (50-200).")
            .defineInRange("defaultScale", 100, 50, 200);

    public static final ForgeConfigSpec.BooleanValue DEFAULT_INVULNERABLE = BUILDER
            .comment("Whether new NPCs start out unable to be hurt.")
            .define("defaultInvulnerable", true);

    public static final ForgeConfigSpec.IntValue MAX_NPCS_PER_PLAYER = BUILDER
            .comment("How many NPCs one player may have in the world.",
                    "0 means unlimited. Operators are not limited.")
            .defineInRange("maxNpcsPerPlayer", 50, 0, 10000);

    public static final ForgeConfigSpec.IntValue WAND_RANGE = BUILDER
            .comment("How far the wand can reach an NPC, in blocks.")
            .defineInRange("wandRange", 8, 1, 32);

    public static final ForgeConfigSpec SPEC = BUILDER.build();

    private NpcConfig() {
    }

    public static void register() {
        ModLoadingContext.get().registerConfig(ModConfig.Type.COMMON, SPEC, "customnpcs-common.toml");
    }

    /** Safe read: the spec is not loaded for a few ticks during startup. */
    public static boolean getBoolean(ForgeConfigSpec.BooleanValue value) {
        try {
            return value.get();
        } catch (IllegalStateException notLoadedYet) {
            return true;
        }
    }

    /** Safe read: the spec is not loaded for a few ticks during startup. */
    public static int getInt(ForgeConfigSpec.IntValue value, int fallback) {
        try {
            return value.get();
        } catch (IllegalStateException notLoadedYet) {
            return fallback;
        }
    }
}
