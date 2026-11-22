package com.jetpack.client;

import net.minecraft.client.gui.components.AbstractSliderButton;
import net.minecraft.network.chat.Component;

import java.util.function.Consumer;
import java.util.function.Function;

/**
 * A slider that shows a formatted value and reports changes as it moves.
 *
 * <p>{@code AbstractSliderButton} still updates slowly while the mouse is held,
 * so the label and the settings object are refreshed on both {@code applyValue}
 * and {@code onRelease}.
 */
public class VoiceSlider extends AbstractSliderButton {
    private final double min;
    private final double max;
    private final Function<Double, Component> label;
    private final Consumer<Double> onChange;
    private final Consumer<Double> onCommit;

    public VoiceSlider(int x, int y, int width, int height, double min, double max,
                       double value, Function<Double, Component> label,
                       Consumer<Double> onChange, Consumer<Double> onCommit) {
        super(x, y, width, height, Component.empty(), (value - min) / (max - min));
        this.min = min;
        this.max = max;
        this.label = label;
        this.onChange = onChange;
        this.onCommit = onCommit;
        updateMessage();
    }

    public double actualValue() {
        return this.min + (this.max - this.min) * this.value;
    }

    public void setActualValue(double actual) {
        this.value = (actual - this.min) / (this.max - this.min);
        this.value = Math.max(0.0D, Math.min(1.0D, this.value));
        updateMessage();
    }

    @Override
    protected void updateMessage() {
        setMessage(this.label.apply(actualValue()));
    }

    @Override
    protected void applyValue() {
        this.onChange.accept(actualValue());
    }

    @Override
    public void onRelease(double mouseX, double mouseY) {
        super.onRelease(mouseX, mouseY);
        this.onCommit.accept(actualValue());
    }
}
