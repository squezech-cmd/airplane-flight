package com.jetpack.client;

import com.jetpack.voice.MicrophoneCapture;
import com.jetpack.voice.VoiceClient;
import com.jetpack.voice.VoiceSettings;
import net.minecraft.ChatFormatting;
import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiGraphics;
import net.minecraft.client.gui.components.Button;
import net.minecraft.client.gui.screens.Screen;
import net.minecraft.network.chat.Component;

import java.util.List;

/**
 * The voice chat settings screen, opened with {@code V} or {@code /voice}.
 *
 * <pre>
 *  +----------------------------------------------+
 *  |  Voice chat                                  |
 *  |  [ Enabled           ]  [ Push to talk  ]    |
 *  |  [ Microphone ON/OFF ]                       |
 *  |  Mic level  [=======]   Range [====] 32 blk  |
 *  |  Volume     [=======]   Gate  [====]         |
 *  |  Device: [ System default          ]  < >    |
 *  |  [ Test microphone ]      Nearby: ...        |
 *  |                      [ Done ]                |
 *  +----------------------------------------------+
 * </pre>
 */
public class VoiceSettingsScreen extends Screen {
    private final Screen parent;
    private final VoiceSettings settings = VoiceSettings.get();

    private VoiceSlider micLevel;
    private VoiceSlider rangeSlider;
    private VoiceSlider volumeSlider;
    private VoiceSlider gateSlider;
    private Button enableButton;
    private Button micButton;
    private Button pushToTalkButton;
    private Button selfMonitorButton;
    private Button deviceButton;

    private int deviceIndex;
    private List<String> devices = List.of("");

    public VoiceSettingsScreen(Screen parent) {
        super(Component.literal("Voice chat"));
        this.parent = parent;
    }

    @Override
    protected void init() {
        this.settings.sanitise();
        this.devices = MicrophoneCapture.devices();
        this.deviceIndex = Math.max(0, this.devices.indexOf(this.settings.inputDevice));

        int left = this.width / 2 - 155;
        int right = this.width / 2 + 5;
        int row = this.height / 2 - 92;

        this.enableButton = Button.builder(label(), button -> {
                    this.settings.enabled = !this.settings.enabled;
                    if (!this.settings.enabled) {
                        this.settings.microphoneOn = false;
                    }
                    button.setMessage(label());
                    refreshMicButton();
                    pushSettings();
                })
                .bounds(left, row, 150, 20).build();
        addRenderableWidget(this.enableButton);

        this.pushToTalkButton = Button.builder(pushToTalkLabel(), button -> {
                    this.settings.pushToTalk = !this.settings.pushToTalk;
                    this.settings.microphoneOn = this.settings.enabled && !this.settings.pushToTalk;
                    button.setMessage(pushToTalkLabel());
                    refreshMicButton();
                    pushSettings();
                })
                .bounds(right, row, 150, 20).build();
        addRenderableWidget(this.pushToTalkButton);

        row += 24;
        this.micButton = Button.builder(micLabel(), button -> {
                    if (!this.settings.enabled) {
                        return;
                    }
                    this.settings.microphoneOn = !this.settings.microphoneOn;
                    button.setMessage(micLabel());
                    pushSettings();
                })
                .bounds(left, row, 300, 20).build();
        addRenderableWidget(this.micButton);

        row += 26;
        this.micLevel = new VoiceSlider(left, row, 145, 20, 0.25D, 3.0D, this.settings.inputGain,
                value -> Component.literal("Mic gain " + percent(value)),
                value -> this.settings.inputGain = value.floatValue(),
                value -> commit());
        addRenderableWidget(this.micLevel);

        this.rangeSlider = new VoiceSlider(right, row, 155, 20, 8.0D, 128.0D, this.settings.range,
                value -> Component.literal("Range " + (int) Math.round(value) + " blocks"),
                value -> this.settings.range = (int) Math.round(value),
                value -> commit());
        addRenderableWidget(this.rangeSlider);

        row += 24;
        this.volumeSlider = new VoiceSlider(left, row, 145, 20, 0.0D, 1.0D, this.settings.outputVolume,
                value -> Component.literal("Volume " + percent(value)),
                value -> this.settings.outputVolume = value.floatValue(),
                value -> commit());
        addRenderableWidget(this.volumeSlider);

        this.gateSlider = new VoiceSlider(right, row, 155, 20, 0.0D, 0.20D, this.settings.noiseGate,
                value -> Component.literal("Noise gate " + (int) Math.round(value * 100)),
                value -> this.settings.noiseGate = value.floatValue(),
                value -> commit());
        addRenderableWidget(this.gateSlider);

        row += 24;
        this.deviceButton = Button.builder(deviceLabel(), button -> {
                    if (!this.devices.isEmpty()) {
                        this.deviceIndex = (this.deviceIndex + 1) % this.devices.size();
                        this.settings.inputDevice = this.devices.get(this.deviceIndex);
                        button.setMessage(deviceLabel());
                    }
                })
                .bounds(left, row, 300, 20).build();
        addRenderableWidget(this.deviceButton);

        row += 24;
        this.selfMonitorButton = Button.builder(selfMonitorLabel(), button -> {
                    this.settings.selfMonitor = !this.settings.selfMonitor;
                    button.setMessage(selfMonitorLabel());
                    // the monitor line is opened when the microphone starts
                    MicrophoneCapture.stop();
                    if (this.settings.enabled) {
                        MicrophoneCapture.start();
                    }
                })
                .bounds(left, row, 145, 20).build();
        addRenderableWidget(this.selfMonitorButton);

        addRenderableWidget(Button.builder(Component.literal("HUD " + onOff(this.settings.showHud)),
                        button -> {
                            this.settings.showHud = !this.settings.showHud;
                            button.setMessage(Component.literal("HUD " + onOff(this.settings.showHud)));
                        })
                .bounds(right, row, 70, 20).build());

        addRenderableWidget(Button.builder(Component.literal("Done"), button -> this.onClose())
                .bounds(right + 80, row, 75, 20).build());
    }

    private void refreshMicButton() {
        if (this.micButton != null) {
            this.micButton.setMessage(micLabel());
        }
    }

    /** Called when a slider is released: persist and tell the server. */
    private void commit() {
        this.settings.save();
        VoiceClient.sendSettings();
    }

    private void pushSettings() {
        this.settings.save();
        VoiceClient.sendSettings();
        if (!this.settings.enabled) {
            MicrophoneCapture.stop();
        } else {
            MicrophoneCapture.start();
        }
    }

    private Component label() {
        return Component.literal("Voice chat: " + onOff(this.settings.enabled));
    }

    private Component pushToTalkLabel() {
        return Component.literal(this.settings.pushToTalk ? "Push to talk (V)" : "Open microphone");
    }

    private Component micLabel() {
        if (!this.settings.enabled) {
            return Component.literal("Microphone: off (enable voice chat first)")
                    .withStyle(ChatFormatting.GRAY);
        }
        return Component.literal("Microphone: " + onOff(this.settings.microphoneOn))
                .withStyle(this.settings.microphoneOn ? ChatFormatting.GREEN : ChatFormatting.GRAY);
    }

    private Component deviceLabel() {
        String name = this.settings.inputDevice;
        if (name == null || name.isEmpty()) {
            return Component.literal("Device: System default");
        }
        return Component.literal("Device: " + trim(name, 34));
    }

    private Component selfMonitorLabel() {
        return Component.literal("Monitor: " + onOff(this.settings.selfMonitor));
    }

    private static String onOff(boolean value) {
        return value ? "ON" : "OFF";
    }

    private static String percent(double value) {
        return Math.round(value * 100.0D) + "%";
    }

    private static String trim(String text, int max) {
        return text.length() <= max ? text : text.substring(0, max - 1) + "\u2026";
    }

    @Override
    public void render(GuiGraphics graphics, int mouseX, int mouseY, float partialTick) {
        renderBackground(graphics);
        graphics.drawCenteredString(this.font, this.title, this.width / 2, this.height / 2 - 118,
                0xFFFFFF);

        // live microphone meter so you can set the gain without guessing
        int meterX = this.width / 2 - 150;
        int meterY = this.height / 2 - 42;
        float level = MicrophoneCapture.level();
        graphics.fill(meterX, meterY, meterX + 300, meterY + 6, 0x60000000);
        int filled = (int) (300 * Math.min(1.0F, level * 3.0F));
        int colour = level > 0.6F ? 0xFF4444 : level > 0.15F ? 0x44DD66 : 0x8899AA;
        graphics.fill(meterX, meterY, meterX + filled, meterY + 6, 0xFF000000 | colour);

        String status = "Status: " + MicrophoneCapture.status()
                + "  |  dimension range " + VoiceClient.serverRange() + " blocks";
        graphics.drawString(this.font, status, meterX, meterY - 12, 0xA0A0A0, false);

        // who is close enough to hear you
        List<com.jetpack.network.VoicePeersPacket.Peer> nearby = VoiceClient.nearby();
        StringBuilder nearbyText = new StringBuilder("Nearby: ");
        if (nearby.isEmpty()) {
            nearbyText.append("nobody in range");
        } else {
            for (int i = 0; i < Math.min(4, nearby.size()); i++) {
                if (i > 0) {
                    nearbyText.append(", ");
                }
                com.jetpack.network.VoicePeersPacket.Peer peer = nearby.get(i);
                nearbyText.append(peer.name()).append(" ").append(peer.distance()).append('m');
            }
        }
        graphics.drawString(this.font, nearbyText.toString(), meterX,
                meterY + 12, 0x80C0FF, false);

        super.render(graphics, mouseX, mouseY, partialTick);
    }

    @Override
    public void onClose() {
        this.settings.save();
        VoiceClient.sendSettings();
        Minecraft.getInstance().setScreen(this.parent);
    }

    @Override
    public boolean isPauseScreen() {
        return false;
    }
}
