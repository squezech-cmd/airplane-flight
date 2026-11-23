package com.customnpcs;

import com.customnpcs.network.NetworkHandler;
import com.customnpcs.registry.ModEntities;
import com.customnpcs.registry.ModItems;
import net.minecraftforge.eventbus.api.IEventBus;
import net.minecraftforge.fml.common.Mod;
import net.minecraftforge.fml.event.lifecycle.FMLCommonSetupEvent;
import net.minecraftforge.fml.javafmlmod.FMLJavaModLoadingContext;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

/**
 * Custom NPCs for Minecraft 1.20.1.
 *
 * <p>The idea is the same one CustomNPCs has always had: no resource pack
 * needed, no config file to hand-edit. You get an NPC spawner item, you place
 * a character, and everything about them — name, skin, size, gear, behaviour,
 * dialogue — is edited in game with a menu.
 */
@Mod(CustomNpcsMod.MOD_ID)
public class CustomNpcsMod {
    public static final String MOD_ID = "customnpcs";
    public static final Logger LOGGER = LogManager.getLogger("CustomNPCs");

    public CustomNpcsMod() {
        IEventBus bus = FMLJavaModLoadingContext.get().getModEventBus();
        ModEntities.register(bus);
        ModItems.register(bus);
        bus.addListener(this::commonSetup);
        NetworkHandler.register();
    }

    private void commonSetup(FMLCommonSetupEvent event) {
        LOGGER.info("Custom NPCs loaded: right-click the ground with the NPC Spawner.");
    }
}
