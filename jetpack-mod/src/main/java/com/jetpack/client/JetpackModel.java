package com.jetpack.client;

import net.minecraft.client.model.HumanoidModel;
import net.minecraft.client.model.geom.ModelPart;
import net.minecraft.client.model.geom.PartPose;
import net.minecraft.client.model.geom.builders.CubeListBuilder;
import net.minecraft.client.model.geom.builders.LayerDefinition;
import net.minecraft.client.model.geom.builders.MeshDefinition;
import net.minecraft.client.model.geom.builders.PartDefinition;
import net.minecraft.world.entity.LivingEntity;

import java.util.List;

/**
 * The jetpack that sits on the player's back: a steel spine plate, two red fuel
 * tanks and a pair of thruster nozzles with glowing flame tips.
 *
 * <p>The geometry lives inside the {@code body} part instead of as extra top level
 * parts. That way it inherits the torso's rotation for free (sneaking, swimming,
 * the walking bob) and it keeps rendering in poses where only the torso is drawn.
 * Every other part is left empty, so no vanilla player cubes are painted with the
 * jetpack texture.
 */
public class JetpackModel extends HumanoidModel<LivingEntity> {

    /**
     * Texture layout is 64x64. Each cube's uv offset matches the region painted by
     * {@code tools/generate_textures.py}: a box of {@code (w, h, d)} occupies
     * {@code (2*d + 2*w)} by {@code (d + h)} texture pixels.
     */
    public static LayerDefinition createBodyLayer() {
        MeshDefinition mesh = new MeshDefinition();
        PartDefinition root = mesh.getRoot();

        // The jetpack itself. Coordinates are relative to the torso pivot at y = 12,
        // exactly like Iron Jetpacks authors its model.
        root.addOrReplaceChild("body", CubeListBuilder.create()
                        // Central spine plate, 0.1 off the skin so the two never z-fight.
                        .texOffs(0, 0)
                        .addBox(-3.0F, 0.5F, 2.1F, 6, 10, 2)
                        // Twin fuel tanks.
                        .texOffs(18, 0)
                        .addBox(1.0F, 1.0F, 2.4F, 3, 8, 4)
                        .texOffs(34, 0)
                        .addBox(-4.0F, 1.0F, 2.4F, 3, 8, 4)
                        // Thruster nozzles under each tank.
                        .texOffs(0, 14)
                        .addBox(1.2F, 9.0F, 2.6F, 3, 2, 3)
                        .texOffs(14, 14)
                        .addBox(-4.2F, 9.0F, 2.6F, 3, 2, 3)
                        // Glowing flame cones under the nozzles.
                        .texOffs(28, 14)
                        .addBox(1.7F, 11.0F, 3.1F, 2, 1, 2)
                        .texOffs(38, 14)
                        .addBox(-3.7F, 11.0F, 3.1F, 2, 1, 2),
                PartPose.offset(0.0F, 12.0F, 0.0F));

        // Empty stubs so HumanoidModel can find every part it expects.
        root.addOrReplaceChild("head", CubeListBuilder.create(), PartPose.ZERO);
        root.addOrReplaceChild("hat", CubeListBuilder.create(), PartPose.ZERO);
        root.addOrReplaceChild("right_arm", CubeListBuilder.create(), PartPose.ZERO);
        root.addOrReplaceChild("left_arm", CubeListBuilder.create(), PartPose.ZERO);
        root.addOrReplaceChild("right_leg", CubeListBuilder.create(), PartPose.ZERO);
        root.addOrReplaceChild("left_leg", CubeListBuilder.create(), PartPose.ZERO);

        return LayerDefinition.create(mesh, 64, 64);
    }

    public JetpackModel(ModelPart root) {
        super(root);
    }

    @Override
    protected Iterable<ModelPart> headParts() {
        return List.<ModelPart>of();
    }

    @Override
    protected Iterable<ModelPart> bodyParts() {
        // Only the torso: it carries the whole jetpack, and the other parts are empty.
        return List.of(this.body);
    }
}
