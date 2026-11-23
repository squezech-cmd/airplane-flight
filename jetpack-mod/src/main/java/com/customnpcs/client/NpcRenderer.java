package com.customnpcs.client;

import com.customnpcs.entity.CustomNpc;
import com.mojang.blaze3d.vertex.PoseStack;
import net.minecraft.client.model.PlayerModel;
import net.minecraft.client.model.geom.ModelLayers;
import net.minecraft.client.renderer.entity.EntityRendererProvider;
import net.minecraft.client.renderer.entity.HumanoidMobRenderer;
import net.minecraft.resources.ResourceLocation;

/**
 * Renders an NPC as a player-shaped humanoid.
 *
 * <p>Using the vanilla player model is the whole trick: every NPC can wear
 * armour, hold items and animate like a player, with no custom model code and
 * no resource pack.
 *
 * <p>The skin field accepts either a preset ({@code steve}, {@code alex}) or a
 * full texture path such as {@code customnpcs:textures/entity/guard.png}, so a
 * server owner can drop their own 64x64 skins into a resource pack and point an
 * NPC at them. Unknown values fall back to Steve instead of crashing.
 */
public class NpcRenderer extends HumanoidMobRenderer<CustomNpc, PlayerModel<CustomNpc>> {
    private static final ResourceLocation STEVE =
            new ResourceLocation("textures/entity/steve.png");
    private static final ResourceLocation ALEX =
            new ResourceLocation("textures/entity/alex.png");

    private final boolean slim;

    public NpcRenderer(EntityRendererProvider.Context context, boolean slim) {
        super(context, new PlayerModel<>(context.bakeLayer(
                slim ? ModelLayers.PLAYER_SLIM : ModelLayers.PLAYER), slim), 0.5F);
        this.slim = slim;
    }

    @Override
    public ResourceLocation getTextureLocation(CustomNpc npc) {
        String skin = npc.skin();
        if (skin == null || skin.isBlank()) {
            return this.slim ? ALEX : STEVE;
        }
        if (skin.equalsIgnoreCase("steve")) {
            return STEVE;
        }
        if (skin.equalsIgnoreCase("alex")) {
            return ALEX;
        }
        if (skin.contains(":")) {
            ResourceLocation location = ResourceLocation.tryParse(skin);
            if (location != null) {
                return location;
            }
        }
        return this.slim ? ALEX : STEVE;
    }

    @Override
    protected void scale(CustomNpc npc, PoseStack poseStack, float partialTick) {
        float scale = npc.scalePercent() / 100.0F;
        poseStack.scale(scale, scale, scale);
    }
}
