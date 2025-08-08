using Godot;
using System;

public partial class AddArcEntry : Panel {
    public static AddArcEntry Instance;

    [Export] private Button confirmAddArcButton;
    [Export] private OptionButton firstPointInput;
    [Export] private OptionButton centerPointInput;
    [Export] private OptionButton secondPointInput;

    public override void _Ready() {
        Instance = this;
        confirmAddArcButton.Pressed += addTheCurve;

        firstPointInput.ItemSelected += optionSelect;
        firstPointInput.ItemSelected += visualizeAddArc;

        centerPointInput.ItemSelected += optionSelect;
        centerPointInput.ItemSelected += visualizeAddArc;

        secondPointInput.ItemSelected += optionSelect;
        secondPointInput.ItemSelected += visualizeAddArc;
    }

    public void closePanel(bool clearSelection = true) {
        if (Visible) {
            Visible = false;
            stopListeningToPointSelectedSignal();
            if (clearSelection) {
                SelectionManager.Instance.selectOneType();
            }
            VisualizationManager.Instance.EndArcAddingVisualization();
        }
    }

    public void openPanel() {
        // Hide the other panels
        //Don't clear selection when changing to a diff tab
        AddSegmentEntry.Instance.closePanel(false);
        AddSplineEntry.Instance.closePanel(false);
        
        AddFaceEntry.Instance.closePanel();
        AddPointEntry.Instance.closePanel();

        Visible = true;

        setOptionButtonsItems();
        visualizeAddArc();

        //Update the OptionButtons whenever a point is selected
        listenToPointSelectedSignal();
    }

    private void setOptionButtonItems(OptionButton optionButton) {
        optionButton.Clear();
        for (int i = 0; i < VisualizationManager.Instance.points.Count; i++) {
            for (int j = 0; j < VisualizationManager.Instance.points[i].Count; j++) {
                // For now work with only 1 shape so we simply assume the elementID is its ID in general.
                optionButton.AddItem($"Point {VisualizationManager.Instance.points[i][j].ElementID}",
                    VisualizationManager.Instance.points[i][j].ElementID);
            }
        }
    }

    private void setOptionButtonsItems() {
        setOptionButtonItems(firstPointInput);
        setOptionButtonItems(centerPointInput);
        setOptionButtonItems(secondPointInput);
        // Update the OptionButtons to reflect the currently selected points
        updatePointInputs();
    }
    //This alternative is just to match the signature for the signal pointSelected
    private void altVisualizeAddArc() {
        visualizeAddArc();
    }
    private void visualizeAddArc(long thisIsJustUsedToMatchSignature = -1) {
        int firstPointID = firstPointInput.GetSelectedId();
        int centerPointID = centerPointInput.GetSelectedId();
        int secondPointID = secondPointInput.GetSelectedId();

        VisualizationManager.Instance.VisualizeAddArc(firstPointID, centerPointID, secondPointID);
    }

    private void updatePointInputs() {
        // Check if there are points that are already selected, if so make them selected in the OptionButton
        if (SelectionManager.Instance.selectedPoints.Count >= 1) {
            firstPointInput.Select(SelectionManager.Instance.selectedPoints[0]);
        }
        if (SelectionManager.Instance.selectedPoints.Count >= 2) {
            centerPointInput.Select(SelectionManager.Instance.selectedPoints[1]);
        }
        if (SelectionManager.Instance.selectedPoints.Count >= 3) {
            secondPointInput.Select(SelectionManager.Instance.selectedPoints[2]);
        }
    }

    private void listenToPointSelectedSignal() {
        SelectionManager.Instance.PointSelected += updatePointInputs;
        SelectionManager.Instance.PointSelected += altVisualizeAddArc;
    }
    private void stopListeningToPointSelectedSignal() {
        SelectionManager.Instance.PointSelected -= updatePointInputs;
        SelectionManager.Instance.PointSelected -= altVisualizeAddArc;
    }

    //When an item from the option button is selected, update the SelectionManager selectin appropriately
    private void optionSelect(long thisIsJustUsedToMatchSignature) {
        GD.Print("Option selected, updating selection in SelectionManager");
        //We start by clearing the selection
        SelectionManager.Instance.selectOneType();

        //Then selecting what's in each OptionButton
        if (firstPointInput.GetSelectedId() != -1) {
            (int shapeId, int pointId) pointInfo = (0, firstPointInput.GetSelectedId());
            GD.Print($"Selecting first point: {pointInfo}");
            SelectionManager.Instance.selectPoint(pointInfo);
        } else return;
        if (centerPointInput.GetSelectedId() != -1) {
            (int shapeId, int pointId) pointInfo = (0, centerPointInput.GetSelectedId());
            SelectionManager.Instance.selectPoint(pointInfo, true);
        } else return;
        if (secondPointInput.GetSelectedId() != -1) {
            (int shapeId, int pointId) pointInfo = (0, secondPointInput.GetSelectedId());
            SelectionManager.Instance.selectPoint(pointInfo,true);
        } else return;
    }
    private void addTheCurve() {
        int firstPointID = firstPointInput.GetSelectedId();
        int centerPointID = centerPointInput.GetSelectedId();
        int secondPointID = secondPointInput.GetSelectedId();

        if (firstPointID == centerPointID || centerPointID == secondPointID || firstPointID == secondPointID) {
            GD.Print("Cannot add an arc with the same point selected twice.");
            return;
        }
        VisualizationManager.Instance.AddArc(0, firstPointID, centerPointID, secondPointID);

        VisualizationManager.Instance.EndArcAddingVisualization();

        // AddCurveTabs.Instance.closeTabs();

    }

}
