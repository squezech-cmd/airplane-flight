package com.customnpcs.registry;

import com.customnpcs.CustomNpcsMod;
import com.customnpcs.item.NpcSpawnerItem;
import com.customnpcs.item.NpcWandItem;
import net.minecraft.world.item.CreativeModeTabs;
import net.minecraft.world.item.Item;
import net.minecraft.world.item.Rarity;
import net.minecraftforge.eventbus.api.IEventBus;
import net.minecraftforge.registries.DeferredRegister;
import net.minecraftforge.registries.ForgeRegistries;
import net.minecraftforge.registries.RegistryObject;

/** The two items the whole mod hangs on: the spawner and the editor wand. */
public final class ModItems {
    public static final DeferredRegister<Item> ITEMS =
            DeferredRegister.create(ForgeRegistries.ITEMS, CustomNpcsMod.MOD_ID);

    public static final RegistryObject<Item> NPC_SPAWNER = ITEMS.register("npc_spawner",
            () -> new NpcSpawnerItem(new Item.Properties()
                    .stacksTo(1)
                    .rarity(Rarity.UNCOMMON)));

    public static final RegistryObject<Item> NPC_WAND = ITEMS.register("npc_wand",
            () -> new NpcWandItem(new Item.Properties()
                    .stacksTo(1)
                    .rarity(Rarity.RARE)));

    private ModItems() {
    }

    public static void register(IEventBus bus) {
        ITEMS.register(bus);
        bus.addListener(ModItems::addToCreativeTabs);
    }

    private static void addToCreativeTabs(net.minecraftforge.event.BuildCreativeModeTabContentsEvent event) {
        if (event.getTabKey() == CreativeModeTabs.SPAWN_EGGS) {
            event.accept(NPC_SPAWNER);
            event.accept(NPC_WAND);
        }
        if (event.getTabKey() == CreativeModeTabs.TOOLS_AND_UTILITIES) {
            event.accept(NPC_WAND);
        }
    }
}
