using Godot;
using System;

public partial class TogglePointVisibility : Button {
    public override void _Ready() {
        this.Pressed += VisualizationManager.Instance.TogglePointsVisibility;
    }
}
