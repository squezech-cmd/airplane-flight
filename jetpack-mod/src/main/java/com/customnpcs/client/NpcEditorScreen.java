package com.customnpcs.client;

import com.customnpcs.entity.CustomNpc;
import com.customnpcs.entity.NpcBehaviour;
import com.customnpcs.network.NetworkHandler;
import com.customnpcs.network.UpdateNpcPacket;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.components.EditBox;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;
import net.minecraft.world.entity.EquipmentSlot;
import net.minecraft.world.item.ItemStack;

/**
 * The NPC editor.
 *
 * <pre>
 *  +-----------------------------------------------+
 *  |  Editing "Тамара Львовна"                      |
 *  |  Name      [____________________]              |
 *  |  Dialogue  [____________________]              |
 *  |  [ Behaviour: Wander ] [ Invulnerable: ON ]    |
 *  |  Scale  [========|=====]  100%                 |
 *  |  [ Hostile: OFF ]      [ Take held item ]      |
 *  |  [ Take helmet ]              [ Save ]  [ X ]  |
 *  +-----------------------------------------------+
 * </pre>
 *
 * <p>Everything is applied on the server from {@link UpdateNpcPacket}; this
 * screen only builds it.
 */
public class NpcEditorScreen extends Screen {
    private final CustomNpc npc;
    private EditBox nameField;
    private EditBox dialogueField;
    private Button behaviourButton;
    private Button invulnerableButton;
    private Button hostileButton;
    private Button scaleButton;

    private String name;
    private String dialogue;
    private NpcBehaviour behaviour;
    private int scalePercent;
    private boolean invulnerable;
    private boolean hostile;
    private ItemStack mainHand = ItemStack.EMPTY;
    private ItemStack head = ItemStack.EMPTY;

    public NpcEditorScreen(CustomNpc npc) {
        super(Component.literal("Edit NPC"));
        this.npc = npc;
        this.name = npc.displayName();
        this.dialogue = npc.dialogue();
        this.behaviour = npc.behaviour();
        this.scalePercent = npc.scalePercent();
        this.invulnerable = npc.isInvulnerableNpc();
        this.hostile = npc.isHostile();
        this.mainHand = npc.getItemBySlot(EquipmentSlot.MAINHAND).copy();
        this.head = npc.getItemBySlot(EquipmentSlot.HEAD).copy();
    }

    @Override
    protected void init() {
        int left = this.width / 2 - 150;
        int top = this.height / 2 - 90;

        this.nameField = new EditBox(this.font, left, top + 22, 300, 20, Component.literal("Name"));
        this.nameField.setMaxLength(32);
        this.nameField.setValue(this.name);
        this.nameField.setResponder(value -> this.name = value);
        addRenderableWidget(this.nameField);

        this.dialogueField = new EditBox(this.font, left, top + 58, 300, 20,
                Component.literal("Dialogue"));
        this.dialogueField.setMaxLength(256);
        this.dialogueField.setValue(this.dialogue);
        this.dialogueField.setResponder(value -> this.dialogue = value);
        addRenderableWidget(this.dialogueField);

        this.behaviourButton = Button.builder(behaviourLabel(), button -> {
                    this.behaviour = this.behaviour.next();
                    button.setMessage(behaviourLabel());
                })
                .bounds(left, top + 86, 146, 20).build();
        addRenderableWidget(this.behaviourButton);

        this.invulnerableButton = Button.builder(invulnerableLabel(), button -> {
                    this.invulnerable = !this.invulnerable;
                    button.setMessage(invulnerableLabel());
                })
                .bounds(left + 154, top + 86, 146, 20).build();
        addRenderableWidget(this.invulnerableButton);

        this.scaleButton = Button.builder(scaleLabel(), button -> {
                    this.scalePercent = this.scalePercent >= 200 ? 50 : this.scalePercent + 10;
                    button.setMessage(scaleLabel());
                })
                .bounds(left, top + 110, 146, 20).build();
        addRenderableWidget(this.scaleButton);

        this.hostileButton = Button.builder(hostileLabel(), button -> {
                    this.hostile = !this.hostile;
                    button.setMessage(hostileLabel());
                })
                .bounds(left + 154, top + 110, 146, 20).build();
        addRenderableWidget(this.hostileButton);

        addRenderableWidget(Button.builder(Component.literal("Give held item"), button -> {
                    Minecraft minecraft = Minecraft.getInstance();
                    if (minecraft.player != null) {
                        this.mainHand = minecraft.player.getMainHandItem().copy();
                    }
                })
                .bounds(left, top + 134, 146, 20).build());
        addRenderableWidget(Button.builder(Component.literal("Give helmet"), button -> {
                    Minecraft minecraft = Minecraft.getInstance();
                    if (minecraft.player != null) {
                        this.head = minecraft.player.getInventory().armor.get(3).copy();
                    }
                })
                .bounds(left + 154, top + 134, 146, 20).build());

        addRenderableWidget(Button.builder(Component.literal("Save"), button -> {
                    NetworkHandler.INSTANCE.sendToServer(new UpdateNpcPacket(
                            this.npc.getId(), this.name, this.dialogue, this.behaviour,
                            this.scalePercent, this.invulnerable, this.hostile,
                            this.mainHand, this.head));
                    this.onClose();
                })
                .bounds(left + 154, top + 166, 146, 20).build());
        addRenderableWidget(Button.builder(Component.literal("Cancel"), button -> this.onClose())
                .bounds(left, top + 166, 146, 20).build());
    }

    private Component behaviourLabel() {
        return Component.literal("Behaviour: " + this.behaviour.display());
    }

    private Component invulnerableLabel() {
        return Component.literal("Invulnerable: " + (this.invulnerable ? "ON" : "OFF"));
    }

    private Component hostileLabel() {
        return Component.literal("Hostile: " + (this.hostile ? "ON" : "OFF"));
    }

    private Component scaleLabel() {
        return Component.literal("Scale: " + this.scalePercent + "%");
    }

    @Override
    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        renderBackground(graphics);
        graphics.drawCenteredString(this.font,
                Component.literal("Editing " + this.npc.displayName()),
                this.width / 2, this.height / 2 - 104, 0xFFFFFF);
        graphics.drawString(this.font, "Name", this.width / 2 - 150,
                this.height / 2 - 90, 0xA0A0A0, false);
        graphics.drawString(this.font, "Dialogue when spoken to", this.width / 2 - 150,
                this.height / 2 - 74, 0xA0A0A0, false);
        super.render(graphics, mouseX, mouseY, partialTick);

        // held items preview
        int left = this.width / 2 - 150;
        int top = this.height / 2 + 76;
        graphics.drawString(this.font, "Held: " + describe(this.mainHand), left, top - 4,
                0x80C0FF, false);
        graphics.drawString(this.font, "Helmet: " + describe(this.head), left + 154, top - 4,
                0x80C0FF, false);
    }

    private static String describe(ItemStack stack) {
        return stack.isEmpty() ? "nothing" : stack.getHoverName().getString();
    }

    @Override
    public void onClose() {
        Minecraft.getInstance().setScreen(null);
    }

    @Override
    public boolean isPauseScreen() {
        return false;
    }
}
