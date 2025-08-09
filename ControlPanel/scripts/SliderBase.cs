using Godot;
using System;
public partial class SliderBase : HSlider
{

    private Label _valueLabel;

    public override void _Ready()
    {
        _valueLabel = GetNode<Label>("Value");

        if (_valueLabel != null)
        {
            ValueChanged += OnSliderValueChanged;
            OnSliderValueChanged(Value);
        }
    }

    private void OnSliderValueChanged(double value)
    {
        if (_valueLabel != null)
            _valueLabel.Text = Value.ToString("0.##");
    }

}
