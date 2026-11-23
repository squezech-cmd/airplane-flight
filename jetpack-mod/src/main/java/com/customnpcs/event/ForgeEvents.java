package com.customnpcs.event;

import com.customnpcs.CustomNpcsMod;
import com.customnpcs.command.NpcCommands;
import net.minecraftforge.event.RegisterCommandsEvent;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;

/** Forge-bus events: game events, not loader lifecycle. */
@Mod.EventBusSubscriber(modid = CustomNpcsMod.MOD_ID, bus = Mod.EventBusSubscriber.Bus.FORGE)
public final class ForgeEvents {
    private ForgeEvents() {
    }

    @SubscribeEvent
    public static void onRegisterCommands(RegisterCommandsEvent event) {
        NpcCommands.register(event.getDispatcher());
    }
}
