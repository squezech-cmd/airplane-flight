package com.jetpack;

import net.minecraft.world.entity.player.Player;

import java.util.Set;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;

public final class ThrustState {
    private static final Set<UUID> THRUSTING = ConcurrentHashMap.newKeySet();

    private ThrustState() {
    }

    public static void setThrusting(Player player, boolean on) {
        if (on) {
            THRUSTING.add(player.getUUID());
        } else {
            THRUSTING.remove(player.getUUID());
        }
    }

    public static boolean isThrusting(Player player) {
        return THRUSTING.contains(player.getUUID());
    }
}
