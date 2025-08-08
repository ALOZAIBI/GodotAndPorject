using Godot;
using System;

public partial class ToggleSurfaceVisibility : Button {
    public override void _Ready() {
        this.Pressed += VisualizationManager.Instance.ToggleSurfacesVisibility;
    }
}
