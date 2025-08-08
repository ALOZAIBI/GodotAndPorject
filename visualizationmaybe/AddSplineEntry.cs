using Godot;
using System;

public partial class AddSplineEntry : Panel {
    public static AddSplineEntry Instance;

    [Export] private Button confirmAddSplineButton;

    public override void _Ready() {
        Instance = this;
        confirmAddSplineButton.Pressed += addTheSpline;
    }

    public void closePanel(bool clearSelection = true) {
        if (Visible) {
            Visible = false;
            stopListeningToPointSelectedSignal();
            if (clearSelection) {
                SelectionManager.Instance.selectOneType();
            }
            VisualizationManager.Instance.EndSplineAddingVisualization();
        }
    }

    public void openPanel() {
        GD.Print("Opening Spline Entry Panel");
        AddSegmentEntry.Instance.closePanel(false);
        AddArcEntry.Instance.closePanel(false);

        AddPointEntry.Instance.closePanel();
        AddFaceEntry.Instance.closePanel();

        Visible = true;
        visualizeAddSpline();

        listenToPointSelectedSignal();

    }

    private void visualizeAddSpline() {
        Godot.Collections.Array<int> pointIDs = [.. SelectionManager.Instance.selectedPoints];
        VisualizationManager.Instance.VisualizeAddSpline(pointIDs);
    }

    private void listenToPointSelectedSignal() {
        SelectionManager.Instance.PointSelected += visualizeAddSpline;
    }

    private void stopListeningToPointSelectedSignal() {
        SelectionManager.Instance.PointSelected -= visualizeAddSpline;
    }

    private void addTheSpline() {
        Godot.Collections.Array<int> pointIDs = [.. SelectionManager.Instance.selectedPoints];
        GD.Print("Adding spline with points: ", pointIDs);
        VisualizationManager.Instance.AddSpline(0, pointIDs); // Assuming shapeIndex is always 0 for now

        VisualizationManager.Instance.EndSplineAddingVisualization();
    }
}
