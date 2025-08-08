using Godot;
using System;

public partial class AddPointButton : Button {

    public override void _Ready() {
        Pressed += toggleAddPointPanel;
    }
    private void toggleAddPointPanel() {
        if (!AddPointEntry.Instance.Visible) {
            openPanel();
        } else {
            closePanel();
        }
    }

    public void openPanel() {
        if (!AddPointEntry.Instance.Visible) {
            AddPointEntry.Instance.openPanel();
        }
    }
    
    public void closePanel() {
        if (AddPointEntry.Instance.Visible) {
            AddPointEntry.Instance.closePanel();
        }
    }
}
