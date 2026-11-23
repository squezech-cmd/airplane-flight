package com.customnpcs.command;

import com.customnpcs.entity.CustomNpc;
import com.customnpcs.entity.NpcBehaviour;
import com.customnpcs.registry.ModEntities;
import com.mojang.brigadier.CommandDispatcher;
import com.mojang.brigadier.arguments.IntegerArgumentType;
import com.mojang.brigadier.arguments.StringArgumentType;
import com.mojang.brigadier.context.CommandContext;
import net.minecraft.ChatFormatting;
import net.minecraft.commands.CommandSourceStack;
import net.minecraft.commands.Commands;
import net.minecraft.core.BlockPos;
import net.minecraft.network.chat.Component;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.world.entity.MobSpawnType;
import net.minecraft.world.phys.Vec3;

import java.util.List;

/**
 * {@code /npc} — spawn, list, edit and remove NPCs from the console or chat.
 *
 * <pre>
 * /npc spawn &lt;name&gt; [behaviour]
 * /npc remove            remove the nearest NPC within 8 blocks
 * /npc removeall         every NPC in this world
 * /npc list              coordinates and behaviour of every NPC here
 * /npc behaviour &lt;wander|follow|guard&gt;
 * /npc name &lt;name&gt;
 * /npc dialogue &lt;text&gt;
 * /npc scale &lt;50-200&gt;
 * /npc hostile &lt;on|off&gt;
 * /npc invulnerable &lt;on|off&gt;
 * </pre>
 *
 * <p>Everything except {@code list} applies to the nearest NPC to the caller,
 * which keeps the common case short while staying scriptable.
 */
public final class NpcCommands {
    private static final double NEAREST_RADIUS = 12.0D;

    private NpcCommands() {
    }

    public static void register(CommandDispatcher<CommandSourceStack> dispatcher) {
        dispatcher.register(Commands.literal("npc")
                .requires(source -> source.hasPermission(2))
                .then(Commands.literal("spawn")
                        .then(Commands.argument("name", StringArgumentType.greedyString())
                                .executes(context -> spawn(context, StringArgumentType.getString(context, "name")))))
                .then(Commands.literal("list").executes(NpcCommands::list))
                .then(Commands.literal("remove").executes(context -> removeNearest(context, false)))
                .then(Commands.literal("removeall").executes(context -> removeNearest(context, true)))
                .then(Commands.literal("behaviour")
                        .then(Commands.literal("wander").executes(context -> setBehaviour(context, NpcBehaviour.WANDER)))
                        .then(Commands.literal("follow").executes(context -> setBehaviour(context, NpcBehaviour.FOLLOW)))
                        .then(Commands.literal("guard").executes(context -> setBehaviour(context, NpcBehaviour.GUARD))))
                .then(Commands.literal("name")
                        .then(Commands.argument("name", StringArgumentType.greedyString())
                                .executes(context -> setName(context, StringArgumentType.getString(context, "name")))))
                .then(Commands.literal("dialogue")
                        .then(Commands.argument("text", StringArgumentType.greedyString())
                                .executes(context -> setDialogue(context, StringArgumentType.getString(context, "text")))))
                .then(Commands.literal("scale")
                        .then(Commands.argument("percent", IntegerArgumentType.integer(50, 200))
                                .executes(context -> setScale(context,
                                        IntegerArgumentType.getInteger(context, "percent")))))
                .then(Commands.literal("hostile")
                        .then(Commands.literal("on").executes(context -> setHostile(context, true)))
                        .then(Commands.literal("off").executes(context -> setHostile(context, false))))
                .then(Commands.literal("invulnerable")
                        .then(Commands.literal("on").executes(context -> setInvulnerable(context, true)))
                        .then(Commands.literal("off").executes(context -> setInvulnerable(context, false))))
                .executes(context -> reply(context.getSource(),
                        "Usage: /npc spawn <name> | /npc list | /npc behaviour guard", ChatFormatting.GRAY)));
    }

    // --------------------------------------------------------------- spawn

    private static int spawn(CommandContext<CommandSourceStack> context, String name) {
        CommandSourceStack source = context.getSource();
        ServerLevel level = source.getLevel();
        Vec3 pos = source.getPosition();
        BlockPos blockPos = BlockPos.containing(pos);

        CustomNpc npc = ModEntities.CUSTOM_NPC.get().create(level);
        if (npc == null) {
            return 0;
        }
        npc.moveTo(blockPos.getX() + 0.5D, blockPos.getY(), blockPos.getZ() + 0.5D,
                source.getRotation().y, 0.0F);
        npc.setCustomNameDirect(name);
        npc.setHome(blockPos);
        npc.finalizeSpawn(level, level.getCurrentDifficultyAt(blockPos),
                MobSpawnType.COMMAND, null, null);
        if (source.getEntity() != null) {
            npc.setOwner(source.getEntity().getUUID());
        }
        level.addFreshEntity(npc);
        return reply(source, "Spawned NPC " + name + " at " + blockPos.toShortString(),
                ChatFormatting.GREEN);
    }

    // -------------------------------------------------------------- lookup

    /** The NPC closest to the command source, or null when none is nearby. */
    private static CustomNpc nearest(CommandSourceStack source) {
        ServerLevel level = source.getLevel();
        Vec3 pos = source.getPosition();
        List<CustomNpc> candidates = level.getEntitiesOfClass(CustomNpc.class,
                new net.minecraft.world.phys.AABB(pos, pos).inflate(NEAREST_RADIUS));
        CustomNpc best = null;
        double bestDistance = Double.MAX_VALUE;
        for (CustomNpc npc : candidates) {
            double distance = npc.distanceToSqr(pos);
            if (distance < bestDistance) {
                bestDistance = distance;
                best = npc;
            }
        }
        return best;
    }

    // --------------------------------------------------------------- edits

    private static int list(CommandContext<CommandSourceStack> context) {
        CommandSourceStack source = context.getSource();
        Vec3 pos = source.getPosition();
        List<CustomNpc> npcs = source.getLevel().getEntitiesOfClass(CustomNpc.class,
                new net.minecraft.world.phys.AABB(pos, pos).inflate(128.0D));
        if (npcs.isEmpty()) {
            return reply(source, "No NPCs within 128 blocks", ChatFormatting.GRAY);
        }
        reply(source, "NPCs nearby:", ChatFormatting.AQUA);
        for (CustomNpc npc : npcs) {
            source.sendSuccess(() -> Component.literal(String.format(
                            "  %s — %s, %d%%, %s",
                            npc.displayName(),
                            npc.behaviour().display(),
                            npc.scalePercent(),
                            npc.blockPosition().toShortString()))
                    .withStyle(ChatFormatting.GRAY), false);
        }
        return npcs.size();
    }

    private static int removeNearest(CommandContext<CommandSourceStack> context, boolean all) {
        CommandSourceStack source = context.getSource();
        if (all) {
            Vec3 pos = source.getPosition();
            List<CustomNpc> npcs = source.getLevel().getEntitiesOfClass(CustomNpc.class,
                    new net.minecraft.world.phys.AABB(pos, pos).inflate(256.0D));
            npcs.forEach(net.minecraft.world.entity.Entity::discard);
            return reply(source, "Removed " + npcs.size() + " NPCs", ChatFormatting.RED);
        }
        CustomNpc npc = nearest(source);
        if (npc == null) {
            return reply(source, "No NPC within " + (int) NEAREST_RADIUS + " blocks",
                    ChatFormatting.RED);
        }
        String name = npc.displayName();
        npc.discard();
        return reply(source, "Removed " + name, ChatFormatting.RED);
    }

    private static int setBehaviour(CommandContext<CommandSourceStack> context, NpcBehaviour behaviour) {
        CustomNpc npc = nearest(context.getSource());
        if (npc == null) {
            return noNpc(context.getSource());
        }
        npc.setBehaviour(behaviour);
        return reply(context.getSource(), npc.displayName() + " now "
                + behaviour.display().toLowerCase(), ChatFormatting.AQUA);
    }

    private static int setName(CommandContext<CommandSourceStack> context, String name) {
        CustomNpc npc = nearest(context.getSource());
        if (npc == null) {
            return noNpc(context.getSource());
        }
        npc.setCustomNameDirect(name);
        return reply(context.getSource(), "Renamed to " + name, ChatFormatting.AQUA);
    }

    private static int setDialogue(CommandContext<CommandSourceStack> context, String text) {
        CustomNpc npc = nearest(context.getSource());
        if (npc == null) {
            return noNpc(context.getSource());
        }
        npc.setDialogue(text);
        return reply(context.getSource(), npc.displayName() + " will say: " + text,
                ChatFormatting.AQUA);
    }

    private static int setScale(CommandContext<CommandSourceStack> context, int percent) {
        CustomNpc npc = nearest(context.getSource());
        if (npc == null) {
            return noNpc(context.getSource());
        }
        npc.setScalePercent(percent);
        return reply(context.getSource(), npc.displayName() + " scaled to " + percent + "%",
                ChatFormatting.AQUA);
    }

    private static int setHostile(CommandContext<CommandSourceStack> context, boolean hostile) {
        CustomNpc npc = nearest(context.getSource());
        if (npc == null) {
            return noNpc(context.getSource());
        }
        npc.setHostile(hostile);
        return reply(context.getSource(), npc.displayName()
                + (hostile ? " will fight" : " is peaceful"), ChatFormatting.AQUA);
    }

    private static int setInvulnerable(CommandContext<CommandSourceStack> context, boolean invulnerable) {
        CustomNpc npc = nearest(context.getSource());
        if (npc == null) {
            return noNpc(context.getSource());
        }
        npc.setInvulnerableNpc(invulnerable);
        return reply(context.getSource(), npc.displayName()
                + (invulnerable ? " cannot be harmed" : " can be harmed"), ChatFormatting.AQUA);
    }

    // -------------------------------------------------------------- helpers

    private static int noNpc(CommandSourceStack source) {
        return reply(source, "No NPC within " + (int) NEAREST_RADIUS + " blocks",
                ChatFormatting.RED);
    }

    private static int reply(CommandSourceStack source, String message, ChatFormatting colour) {
        source.sendSuccess(() -> Component.literal(message).withStyle(colour), false);
        return 1;
    }
}
