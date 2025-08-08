using Godot;
using System;

public partial class PointSelectionMode : Button {
    public override void _Ready() { 
        Pressed += SelectionManager.Instance.SelectionModePoint;
    }
}
