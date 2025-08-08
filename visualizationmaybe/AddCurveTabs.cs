using Godot;
using System;

public partial class AddCurveTabs : Control {
    public static AddCurveTabs Instance;

    [Export] private Button segmentTabButton;
    [Export] private Button arcTabButton;
    [Export] private Button splineTabButton;

    private int currentTabIndex = 0;

    public override void _Ready() {
        Instance = this;
        segmentTabButton.Pressed += switchToSegmentTab;
        arcTabButton.Pressed += switchToArcTab;
        splineTabButton.Pressed += switchToSplineTab;
    }

    public void closeTabs() {
        AddSegmentEntry.Instance.closePanel();
        AddArcEntry.Instance.closePanel();
        AddSplineEntry.Instance.closePanel();
        Visible = false; // Hide the container( so will hide points etc etc)
    }

    public void openTabs() {
        Visible = true; // Show the container
        //We want to be able to select points since htat is the input used when creating curves
        SelectionManager.Instance.SelectionModePoint();
        switch (currentTabIndex) {
            case 0:
                AddSegmentEntry.Instance.openPanel();
                break;
            case 1:
                AddArcEntry.Instance.openPanel();
                break;
            case 2:
                AddSplineEntry.Instance.openPanel();
                break;
            default:
                GD.PrintErr("Invalid tab index: " + currentTabIndex);
                break;
        }
    }

    public void switchToSegmentTab() {
        currentTabIndex = 0;
        openTabs();
    }

    public void switchToArcTab() {
        currentTabIndex = 1;
        openTabs();
    }
    public void switchToSplineTab() {
        currentTabIndex = 2;
        openTabs();
    }

}
