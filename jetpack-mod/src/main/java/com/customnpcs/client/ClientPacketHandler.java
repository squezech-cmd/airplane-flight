package com.customnpcs.client;

import com.customnpcs.entity.CustomNpc;
import net.minecraft.client.Minecraft;
import net.minecraft.world.entity.Entity;

/** Runs on the client thread when the server asks for the editor to open. */
public final class ClientPacketHandler {
    private ClientPacketHandler() {
    }

    public static void openEditor(int entityId) {
        Minecraft minecraft = Minecraft.getInstance();
        if (minecraft.level == null) {
            return;
        }
        Entity entity = minecraft.level.getEntity(entityId);
        if (entity instanceof CustomNpc npc) {
            minecraft.setScreen(new NpcEditorScreen(npc));
        }
    }
}
