package com.jetpack.network;

import com.jetpack.JetpackMod;
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
                new ResourceLocation(JetpackMod.MOD_ID, "main"),
                () -> PROTOCOL_VERSION,
                client -> true,
                server -> true);

        INSTANCE.registerMessage(nextId(), ThrustPacket.class,
                ThrustPacket::encode, ThrustPacket::new, ThrustPacket::handle);
        INSTANCE.registerMessage(nextId(), VoiceFramePacket.class,
                VoiceFramePacket::encode, VoiceFramePacket::new, VoiceFramePacket::handle);
        INSTANCE.registerMessage(nextId(), VoiceSettingsPacket.class,
                VoiceSettingsPacket::encode, VoiceSettingsPacket::new, VoiceSettingsPacket::handle);
        INSTANCE.registerMessage(nextId(), VoicePacket.class,
                VoicePacket::encode, VoicePacket::new, VoicePacket::handle);
        INSTANCE.registerMessage(nextId(), VoicePeersPacket.class,
                VoicePeersPacket::encode, VoicePeersPacket::new, VoicePeersPacket::handle);
        INSTANCE.registerMessage(nextId(), VoiceOpenPacket.class,
                VoiceOpenPacket::encode, VoiceOpenPacket::new, VoiceOpenPacket::handle);
    }
}
