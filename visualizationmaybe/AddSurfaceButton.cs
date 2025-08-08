using Godot;
using System;

public partial class AddSurfaceButton : Button {
    public override void _Ready() {
        Pressed += toggleAddSurfacePanel;
    }

    private void toggleAddSurfacePanel() {
        if (!AddFaceEntry.Instance.Visible) {
            openPanel();
        } else {
            closePanel();
        }
    }
    public void openPanel() {
        if (!AddFaceEntry.Instance.Visible) {
            AddFaceEntry.Instance.openPanel();
        }
    }

    public void closePanel() {
        if (AddFaceEntry.Instance.Visible) {
            AddFaceEntry.Instance.closePanel();
        }
    }
}
