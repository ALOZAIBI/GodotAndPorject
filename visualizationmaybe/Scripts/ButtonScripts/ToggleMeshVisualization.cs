using Godot;
using System;

public partial class ToggleMeshVisualization : Button {
    
    public override void _Ready() {
        Pressed += VisualizationManager.Instance.ToggleMeshesVisibility;
    }
}
