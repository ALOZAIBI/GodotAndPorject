using Godot;
using System;

public partial class AddCurveButton : Button {

    public override void _Ready() {
        Pressed += toggleAddCurvePanel;
    }
    private void toggleAddCurvePanel() {
        if (!AddCurveTabs.Instance.Visible) {
            openPanel();
        } else {
            closePanel();
        }
    }

    public void openPanel() {
        AddCurveTabs.Instance.openTabs();
    }

    public void closePanel() {
        AddCurveTabs.Instance.closeTabs();
    }
}
