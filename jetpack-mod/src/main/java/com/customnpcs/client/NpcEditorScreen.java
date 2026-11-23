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

import java.util.List;

/**
 * The NPC editor.
 *
 * <pre>
 *  +--------------------------------------------------+
 *  |  Editing "Тамара Львовна"                         |
 *  |  Name      [___________________________]          |
 *  |  Dialogue  [___________________________]          |
 *  |  Skin      [customnpcs:...   ] [ Steve ] [ Alex ] |
 *  |  [ Behaviour: Wander ]   [ Invulnerable: ON ]     |
 *  |  [ Scale: 100% ]         [ Hostile: OFF ]         |
 *  |  [ Give held item ]      [ Give helmet ]          |
 *  |  Held: nothing  Helmet: nothing                   |
 *  |  [ Cancel ]                    [ Save ]           |
 *  +--------------------------------------------------+
 * </pre>
 *
 * <p>Everything is applied on the server from {@link UpdateNpcPacket}; this
 * screen only collects values. Nothing here can change the world on its own,
 * which is why the screen is safe to open on any client.
 */
public class NpcEditorScreen extends Screen {
    /** Help lines shown at the bottom, one row per field. */
    private static final List<String> HELP = List.of(
            "Name shows above the NPC and in chat when it speaks.",
            "Skin: steve, alex, or a texture path such as customnpcs:textures/entity/guard.png",
            "Behaviour: Wander walks around home, Follow stays with you, Guard stands still.",
            "Hostile makes it attack players and monsters. Invulnerable protects it from damage.",
            "Right-click with an empty hand to hear the dialogue line.");

    private final CustomNpc npc;
    private EditBox nameField;
    private EditBox dialogueField;
    private EditBox skinField;

    private String name;
    private String dialogue;
    private String skin;
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
        this.skin = npc.skin();
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
        int top = this.height / 2 - 100;

        this.nameField = new EditBox(this.font, left, top, 300, 20, Component.literal("Name"));
        this.nameField.setMaxLength(32);
        this.nameField.setValue(this.name);
        this.nameField.setResponder(value -> this.name = value);
        addRenderableWidget(this.nameField);

        this.dialogueField = new EditBox(this.font, left, top + 34, 300, 20,
                Component.literal("Dialogue"));
        this.dialogueField.setMaxLength(256);
        this.dialogueField.setValue(this.dialogue);
        this.dialogueField.setResponder(value -> this.dialogue = value);
        addRenderableWidget(this.dialogueField);

        this.skinField = new EditBox(this.font, left, top + 68, 146, 20, Component.literal("Skin"));
        this.skinField.setMaxLength(96);
        this.skinField.setValue(this.skin);
        this.skinField.setResponder(value -> this.skin = value);
        addRenderableWidget(this.skinField);

        addRenderableWidget(Button.builder(Component.literal("Steve"), button -> {
                    this.skin = "steve";
                    this.skinField.setValue(this.skin);
                })
                .bounds(left + 154, top + 68, 70, 20).build());
        addRenderableWidget(Button.builder(Component.literal("Alex"), button -> {
                    this.skin = "alex";
                    this.skinField.setValue(this.skin);
                })
                .bounds(left + 230, top + 68, 70, 20).build());

        this.behaviourButton = Button.builder(behaviourLabel(), button -> {
                    this.behaviour = this.behaviour.next();
                    button.setMessage(behaviourLabel());
                })
                .bounds(left, top + 102, 146, 20).build();
        addRenderableWidget(this.behaviourButton);

        this.invulnerableButton = Button.builder(invulnerableLabel(), button -> {
                    this.invulnerable = !this.invulnerable;
                    button.setMessage(invulnerableLabel());
                })
                .bounds(left + 154, top + 102, 146, 20).build();
        addRenderableWidget(this.invulnerableButton);

        this.scaleButton = Button.builder(scaleLabel(), button -> {
                    this.scalePercent = this.scalePercent >= 200 ? 50 : this.scalePercent + 10;
                    button.setMessage(scaleLabel());
                })
                .bounds(left, top + 126, 146, 20).build();
        addRenderableWidget(this.scaleButton);

        this.hostileButton = Button.builder(hostileLabel(), button -> {
                    this.hostile = !this.hostile;
                    button.setMessage(hostileLabel());
                })
                .bounds(left + 154, top + 126, 146, 20).build();
        addRenderableWidget(this.hostileButton);

        addRenderableWidget(Button.builder(Component.literal("Give held item"), button -> {
                    Minecraft minecraft = Minecraft.getInstance();
                    if (minecraft.player != null) {
                        this.mainHand = minecraft.player.getMainHandItem().copy();
                    }
                })
                .bounds(left, top + 150, 146, 20).build());
        addRenderableWidget(Button.builder(Component.literal("Give helmet"), button -> {
                    Minecraft minecraft = Minecraft.getInstance();
                    if (minecraft.player != null) {
                        this.head = minecraft.player.getInventory().armor.get(3).copy();
                    }
                })
                .bounds(left + 154, top + 150, 146, 20).build());

        addRenderableWidget(Button.builder(Component.literal("Cancel"), button -> this.onClose())
                .bounds(left, top + 182, 146, 20).build());
        addRenderableWidget(Button.builder(Component.literal("Save"), button -> {
                    NetworkHandler.INSTANCE.sendToServer(new UpdateNpcPacket(
                            this.npc.getId(), this.name, this.dialogue, this.skin,
                            this.behaviour, this.scalePercent, this.invulnerable,
                            this.hostile, this.mainHand, this.head));
                    this.onClose();
                })
                .bounds(left + 154, top + 182, 146, 20).build());
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
        int left = this.width / 2 - 150;
        int top = this.height / 2 - 100;

        graphics.drawCenteredString(this.font,
                Component.literal("Editing " + this.npc.displayName()),
                this.width / 2, top - 16, 0xFFFFFF);
        graphics.drawString(this.font, "Name", left, top - 10, 0xA0A0A0, false);
        graphics.drawString(this.font, "Dialogue when spoken to", left, top + 24, 0xA0A0A0, false);
        graphics.drawString(this.font, "Skin", left, top + 58, 0xA0A0A0, false);
        graphics.drawString(this.font, "Held: " + describe(this.mainHand), left, top + 174,
                0x80C0FF, false);
        graphics.drawString(this.font, "Helmet: " + describe(this.head), left + 154, top + 174,
                0x80C0FF, false);

        super.render(graphics, mouseX, mouseY, partialTick);

        int helpTop = top + 210;
        for (int i = 0; i < HELP.size(); i++) {
            graphics.drawString(this.font, HELP.get(i), left, helpTop + i * 11,
                    0x707070, false);
        }
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
