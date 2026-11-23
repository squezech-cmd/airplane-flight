package com.customnpcs.event;

import com.customnpcs.command.NpcCommands;
import com.customnpcs.entity.CustomNpc;
import com.customnpcs.registry.ModEntities;
import net.minecraftforge.event.RegisterCommandsEvent;
import net.minecraftforge.event.entity.EntityAttributeCreationEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;

/** Attribute registration and the command tree. */
public final class ModEvents {
    private ModEvents() {
    }

    @SubscribeEvent
    public static void onAttributes(EntityAttributeCreationEvent event) {
        event.put(ModEntities.CUSTOM_NPC.get(), CustomNpc.createAttributes().build());
    }

    @SubscribeEvent
    public static void onRegisterCommands(RegisterCommandsEvent event) {
        NpcCommands.register(event.getDispatcher());
    }
}
