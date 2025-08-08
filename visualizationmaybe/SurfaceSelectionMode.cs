using Godot;
using System;

public partial class SurfaceSelectionMode : Button {
    public override void _Ready() {
        Pressed += SelectionManager.Instance.SelectionModeSurface;
    }
}
