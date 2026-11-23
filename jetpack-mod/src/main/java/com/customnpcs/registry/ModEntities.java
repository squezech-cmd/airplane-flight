package com.customnpcs.registry;

import com.customnpcs.CustomNpcsMod;
import com.customnpcs.entity.CustomNpc;
import net.minecraft.resources.ResourceLocation;
import net.minecraft.world.entity.EntityType;
import net.minecraft.world.entity.MobCategory;
import net.minecraftforge.eventbus.api.IEventBus;
import net.minecraftforge.registries.DeferredRegister;
import net.minecraftforge.registries.ForgeRegistries;
import net.minecraftforge.registries.RegistryObject;

/** The one entity type the mod is built on. */
public final class ModEntities {
    public static final DeferredRegister<EntityType<?>> ENTITIES =
            DeferredRegister.create(ForgeRegistries.ENTITY_TYPES, CustomNpcsMod.MOD_ID);

    public static final RegistryObject<EntityType<CustomNpc>> CUSTOM_NPC =
            ENTITIES.register("custom_npc", () -> EntityType.Builder
                    .of(CustomNpc::new, MobCategory.CREATURE)
                    .sized(0.6F, 1.8F)
                    .clientTrackingRange(10)
                    .updateInterval(3)
                    .build(new ResourceLocation(CustomNpcsMod.MOD_ID, "custom_npc").toString()));

    private ModEntities() {
    }

    public static void register(IEventBus bus) {
        ENTITIES.register(bus);
    }
}
