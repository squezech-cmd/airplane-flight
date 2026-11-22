package com.jetpack;

import net.minecraft.world.entity.player.Player;

import java.util.Set;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;

/**
 * Keeps track of who is currently firing the jetpack.
 *
 * <p>There are two separate sets on purpose:
 * <ul>
 *     <li>{@code SERVER_THRUST} is filled from the {@link com.jetpack.network.ThrustPacket}
 *     so the server can burn fuel, spawn particles and keep track of fall distance.</li>
 *     <li>{@code CLIENT_THRUST} mirrors the local player's jump key. Player movement is
 *     computed on the client, so the item needs that information while it runs its
 *     client-side armor tick.</li>
 * </ul>
 */
public final class ThrustState {
    private static final Set<UUID> SERVER_THRUST = ConcurrentHashMap.newKeySet();
    private static final Set<UUID> CLIENT_THRUST = ConcurrentHashMap.newKeySet();

    private ThrustState() {
    }

    public static void setThrusting(Player player, boolean on) {
        update(SERVER_THRUST, player, on);
    }

    public static boolean isThrusting(Player player) {
        return SERVER_THRUST.contains(player.getUUID());
    }

    public static void setClientThrusting(Player player, boolean on) {
        update(CLIENT_THRUST, player, on);
    }

    public static boolean isClientThrusting(Player player) {
        return CLIENT_THRUST.contains(player.getUUID());
    }

    public static void clearClient() {
        CLIENT_THRUST.clear();
    }

    private static void update(Set<UUID> set, Player player, boolean on) {
        if (on) {
            set.add(player.getUUID());
        } else {
            set.remove(player.getUUID());
        }
    }
}
