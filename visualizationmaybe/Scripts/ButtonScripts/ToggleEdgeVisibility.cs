using Godot;
using System;

public partial class ToggleEdgeVisibility : Button {

    public override void _Ready() {
        Pressed += VisualizationManager.Instance.ToggleEdgesVisibility;
    }
}
