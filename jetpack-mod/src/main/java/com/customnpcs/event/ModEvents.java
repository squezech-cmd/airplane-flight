package com.customnpcs.event;

import com.customnpcs.CustomNpcsMod;
import com.customnpcs.entity.CustomNpc;
import com.customnpcs.registry.ModEntities;
import net.minecraftforge.event.entity.EntityAttributeCreationEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;

/**
 * Mod-bus events.
 *
 * <p>{@link EntityAttributeCreationEvent} is an {@code IModBusEvent}, so it
 * must be subscribed on the mod bus; subscribing on the Forge bus, as is easy
 * to do by accident, silently leaves the NPC without attributes and the
 * entity throws the moment it spawns.
 */
@Mod.EventBusSubscriber(modid = CustomNpcsMod.MOD_ID, bus = Mod.EventBusSubscriber.Bus.MOD)
public final class ModEvents {
    private ModEvents() {
    }

    @SubscribeEvent
    public static void onAttributes(EntityAttributeCreationEvent event) {
        event.put(ModEntities.CUSTOM_NPC.get(), CustomNpc.createAttributes().build());
    }
}
