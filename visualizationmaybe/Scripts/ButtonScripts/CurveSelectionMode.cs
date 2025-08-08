using Godot;
using System;

public partial class CurveSelectionMode : Button {
    public override void _Ready() { 
        Pressed += SelectionManager.Instance.SelectionModeCurve;
    }
}
