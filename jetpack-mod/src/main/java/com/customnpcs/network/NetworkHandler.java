package com.customnpcs.network;

import com.customnpcs.CustomNpcsMod;
import net.minecraft.resources.ResourceLocation;
import net.minecraftforge.network.NetworkRegistry;
import net.minecraftforge.network.simple.SimpleChannel;

public final class NetworkHandler {
    private static final String PROTOCOL_VERSION = "1";
    public static SimpleChannel INSTANCE;
    private static int packetId = 0;

    private static int nextId() {
        return packetId++;
    }

    private NetworkHandler() {
    }

    public static void register() {
        INSTANCE = NetworkRegistry.newSimpleChannel(
                new ResourceLocation(CustomNpcsMod.MOD_ID, "main"),
                () -> PROTOCOL_VERSION,
                client -> true,
                server -> true);

        INSTANCE.registerMessage(nextId(), OpenEditorPacket.class,
                OpenEditorPacket::encode, OpenEditorPacket::new, OpenEditorPacket::handle);
        INSTANCE.registerMessage(nextId(), UpdateNpcPacket.class,
                UpdateNpcPacket::encode, UpdateNpcPacket::new, UpdateNpcPacket::handle);
    }
}
