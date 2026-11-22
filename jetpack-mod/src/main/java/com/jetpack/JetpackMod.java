package com.jetpack;

import com.jetpack.network.NetworkHandler;
import net.minecraft.network.chat.Component;
import net.minecraft.world.item.CreativeModeTab;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.ItemStack;
import net.minecraftforge.common.MinecraftForge;
import net.minecraftforge.event.entity.player.PlayerEvent;
import net.minecraftforge.eventbus.api.IEventBus;
import net.minecraftforge.eventbus.api.SubscribeEvent;
import net.minecraftforge.fml.common.Mod;
import net.minecraftforge.fml.event.lifecycle.FMLCommonSetupEvent;
import net.minecraftforge.fml.javafmlmod.FMLJavaModLoadingContext;
import net.minecraftforge.registries.DeferredRegister;
import net.minecraftforge.registries.ForgeRegistries;
import net.minecraftforge.registries.RegistryObject;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

@Mod(JetpackMod.MOD_ID)
public class JetpackMod {
    public static final String MOD_ID = "jetpack";
    private static final Logger LOGGER = LogManager.getLogger();

    public static final DeferredRegister<Item> ITEMS = DeferredRegister.create(ForgeRegistries.ITEMS, MOD_ID);
    public static final DeferredRegister<CreativeModeTab> CREATIVE_TABS = DeferredRegister.create(ForgeRegistries.CREATIVE_MODE_TABS, MOD_ID);

    public static final RegistryObject<Item> JETPACK = ITEMS.register("jetpack", JetpackItem::new);

    public static final RegistryObject<CreativeModeTab> JETPACK_TAB = CREATIVE_TABS.register("jetpack_tab", () ->
            CreativeModeTab.builder()
                    .title(Component.translatable("itemGroup.jetpack"))
                    .icon(() -> new ItemStack(JETPACK.get()))
                    .displayItems((params, output) -> output.accept(JETPACK.get()))
                    .build());

    public JetpackMod() {
        IEventBus bus = FMLJavaModLoadingContext.get().getModEventBus();
        ITEMS.register(bus);
        CREATIVE_TABS.register(bus);
        bus.addListener(this::commonSetup);
        NetworkHandler.register();
        MinecraftForge.EVENT_BUS.register(this);
    }

    private void commonSetup(FMLCommonSetupEvent event) {
        LOGGER.info("Jetpack mod loaded! Grab the Jetpack from the creative inventory and hold SPACE to fly.");
    }

    @SubscribeEvent
    public void onPlayerLogout(PlayerEvent.PlayerLoggedOutEvent event) {
        ThrustState.setThrusting(event.getEntity(), false);
    }
}
