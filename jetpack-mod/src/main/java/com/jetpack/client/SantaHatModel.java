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
 * A floppy Santa hat: white brim, red cone leaning back, white pompom on the tip.
 * Parented to the head so it follows every look direction.
 */
public class SantaHatModel extends HumanoidModel<LivingEntity> {

    public static LayerDefinition createHeadLayer() {
        MeshDefinition mesh = new MeshDefinition();
        PartDefinition root = mesh.getRoot();

        root.addOrReplaceChild("head", CubeListBuilder.create()
                        // White fur brim, a little wider than the head.
                        .texOffs(0, 0)
                        .addBox(-4.5F, -9.0F, -4.5F, 9, 2, 9)
                        // Main red cone, stepped inwards as it rises.
                        .texOffs(0, 12)
                        .addBox(-4.0F, -12.0F, -4.0F, 8, 3, 8)
                        .texOffs(0, 24)
                        .addBox(-3.0F, -14.0F, -3.0F, 6, 2, 6)
                        .texOffs(0, 33)
                        .addBox(-2.0F, -15.5F, -2.0F, 4, 2, 4)
                        // Tip leaning back towards the neck.
                        .texOffs(0, 40)
                        .addBox(-1.0F, -16.5F, -0.5F, 2, 1, 2)
                        // Pompom.
                        .texOffs(10, 40)
                        .addBox(-1.5F, -17.5F, 0.5F, 3, 3, 3),
                PartPose.ZERO);

        root.addOrReplaceChild("hat", CubeListBuilder.create(), PartPose.ZERO);
        root.addOrReplaceChild("body", CubeListBuilder.create(), PartPose.ZERO);
        root.addOrReplaceChild("right_arm", CubeListBuilder.create(), PartPose.ZERO);
        root.addOrReplaceChild("left_arm", CubeListBuilder.create(), PartPose.ZERO);
        root.addOrReplaceChild("right_leg", CubeListBuilder.create(), PartPose.ZERO);
        root.addOrReplaceChild("left_leg", CubeListBuilder.create(), PartPose.ZERO);

        return LayerDefinition.create(mesh, 64, 64);
    }

    public SantaHatModel(ModelPart root) {
        super(root);
    }

    @Override
    protected Iterable<ModelPart> headParts() {
        return List.of(this.head);
    }

    @Override
    protected Iterable<ModelPart> bodyParts() {
        return List.<ModelPart>of();
    }
}
