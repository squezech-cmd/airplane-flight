package com.jetpack;

import com.jetpack.network.NetworkHandler;
import com.jetpack.registry.ModRegistry;
import net.minecraftforge.common.MinecraftForge;
import net.minecraftforge.event.entity.player.PlayerEvent;
import net.minecraftforge.eventbus.api.IEventBus;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;
import net.minecraftforge.fml.event.lifecycle.FMLCommonSetupEvent;
import net.minecraftforge.fml.javafmlmod.FMLJavaModLoadingContext;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

@Mod(JetpackMod.MOD_ID)
public class JetpackMod {
    public static final String MOD_ID = "jetpack";
    public static final Logger LOGGER = LogManager.getLogger();

    public JetpackMod() {
        IEventBus bus = FMLJavaModLoadingContext.get().getModEventBus();
        ModRegistry.register(bus);
        bus.addListener(this::commonSetup);
        NetworkHandler.register();
        MinecraftForge.EVENT_BUS.register(this);
    }

    private void commonSetup(FMLCommonSetupEvent event) {
        LOGGER.info("Jetpack + Christmas loaded! Hold SPACE to fly and check the Christmas creative tab.");
    }

    @SubscribeEvent
    public void onPlayerLogout(PlayerEvent.PlayerLoggedOutEvent event) {
        ThrustState.setThrusting(event.getEntity(), false);
    }

    @SubscribeEvent
    public void onRegisterCommands(net.minecraftforge.event.RegisterCommandsEvent event) {
        com.jetpack.command.VoiceCommands.register(event.getDispatcher());
    }
}
