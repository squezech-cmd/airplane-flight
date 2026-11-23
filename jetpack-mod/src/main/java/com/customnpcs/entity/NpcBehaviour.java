package com.customnpcs.entity;

/**
 * What an NPC should do when nobody is giving it orders.
 *
 * <p>Only three behaviours on purpose: a wandering villager, a companion that
 * stays with its owner, and a stationary guard. Anything more complex belongs
 * in a scripting layer, and a first version should be predictable.
 */
public enum NpcBehaviour {
    /** Wanders around its home position, avoiding water and falls. */
    WANDER("Wander"),
    /** Follows whoever owns it, staying a couple of blocks behind. */
    FOLLOW("Follow"),
    /** Stands still where it was placed, turning to face nearby players. */
    GUARD("Guard");

    private final String display;

    NpcBehaviour(String display) {
        this.display = display;
    }

    public String display() {
        return this.display;
    }

    public NpcBehaviour next() {
        NpcBehaviour[] values = values();
        return values[(this.ordinal() + 1) % values.length];
    }

    public static NpcBehaviour byOrdinal(int ordinal) {
        NpcBehaviour[] values = values();
        return ordinal >= 0 && ordinal < values.length ? values[ordinal] : WANDER;
    }
}
