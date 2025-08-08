using Godot;
using System;

public partial class AddFaceEntry : Panel {
    public static AddFaceEntry Instance;

    [Export] private Button confirmAddFaceButton;
    public override void _Ready() {
        Instance = this;
        confirmAddFaceButton.Pressed += addTheSurface;
    }

    public void closePanel(bool clearSelection = true) {
        if (Visible) {
            Visible = false;
            // stopListeningToPointSelectedSignal();
            if (clearSelection) {
                SelectionManager.Instance.selectOneType();
            }
            // VisualizationManager.Instance.EndFaceAddingVisualization();
        }
    }

    public void openPanel() {
        // Hide the other panels
        // Don't clear selection when changing to a diff tab
        AddCurveTabs.Instance.closeTabs();
        AddPointEntry.Instance.closePanel();

        Visible = true;

        SelectionManager.Instance.SelectionModeCurve();
        // visualizeAddFace();

        // Update the OptionButtons whenever a point is selected
        // listenToPointSelectedSignal();
    }


    private void addTheSurface() {
        Godot.Collections.Array<int> edgeIDs = [.. SelectionManager.Instance.selectedCurves];
        GD.Print("Adding face with edges: ", edgeIDs);
        VisualizationManager.Instance.AddFace(0, edgeIDs); // Assuming shapeIndex is always 0 for now
    }


}
