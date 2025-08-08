using Godot;
using System;

public partial class AddSegmentEntry : Panel {

    public static AddSegmentEntry Instance;
    [Export] private Button confirmAddCurveButton;
    [Export] private OptionButton firstPointInput;
    [Export] private OptionButton secondPointInput;


    public override void _Ready() {
        Instance = this;
        confirmAddCurveButton.Pressed += addTheCurve;

        firstPointInput.ItemSelected += optionSelect;
        firstPointInput.ItemSelected += visualizeAddSegment;

        secondPointInput.ItemSelected += optionSelect;
        secondPointInput.ItemSelected += visualizeAddSegment;
    }

    public void closePanel(bool clearSelection = true) {
        if (Visible) {
            Visible = false;
            stopListeningToPointSelectedSignal();
            if (clearSelection) {
                SelectionManager.Instance.selectOneType();
            }
            VisualizationManager.Instance.EndSegmentAddingVisualization();
        }
    }

    public void openPanel() {

        //Hide the other panels
        //Don't clear selection when changing to a diff tab
        AddArcEntry.Instance.closePanel(false);
        AddSplineEntry.Instance.closePanel(false);
        AddPointEntry.Instance.closePanel();
        
        AddFaceEntry.Instance.closePanel();

        Visible = true;
        setOptionButtonsItems();
        visualizeAddSegment();

        //Start visualization for adding a curve
        listenToPointSelectedSignal();
        
    }
    private void setOptionButtonItems(OptionButton optionButton) {
        optionButton.Clear();
        for (int i = 0; i < VisualizationManager.Instance.points.Count; i++) {
            for (int j = 0; j < VisualizationManager.Instance.points[i].Count; j++) {
                //For now work with only 1 shape so we simply assume the elementID is its ID in general.
                optionButton.AddItem($"Point {VisualizationManager.Instance.points[i][j].ElementID}",
                    VisualizationManager.Instance.points[i][j].ElementID);
            }
        }

    }

    public void setOptionButtonsItems() {
        setOptionButtonItems(firstPointInput);
        setOptionButtonItems(secondPointInput);
        updatePointInputs();
    }

    //This alternative is just to match the signature for the signal pointSelected
    private void altVisualizeAddSegment() {
        visualizeAddSegment();
    }

    private void visualizeAddSegment(long thisIsJustUsedToMatchSignature = -1) {
        GD.Print("Visualizing add curve");
        int firstPointID = firstPointInput.GetSelectedId();
        int secondPointID = secondPointInput.GetSelectedId();

        VisualizationManager.Instance.VisualizeAddSegment(firstPointID, secondPointID);
    }

    //When the pointselected signal is received, update the selected point in the OptionButton
    private void updatePointInputs() {
        //Check if there are points that are already selected, if so make them selected in the OptionButton
        if (SelectionManager.Instance.selectedPoints.Count >= 1) {
            firstPointInput.Select(SelectionManager.Instance.selectedPoints[0]);
        }
        if (SelectionManager.Instance.selectedPoints.Count >= 2) {
            secondPointInput.Select(SelectionManager.Instance.selectedPoints[1]);
        }
    }
    private void listenToPointSelectedSignal() {
        GD.Print("Listening to point selected signal");
        SelectionManager.Instance.PointSelected += updatePointInputs;
        SelectionManager.Instance.PointSelected += altVisualizeAddSegment;
    }

    private void stopListeningToPointSelectedSignal() {
        GD.Print("Stopped listening to point selected signal");
        SelectionManager.Instance.PointSelected -= updatePointInputs;
        SelectionManager.Instance.PointSelected -= altVisualizeAddSegment;
    }

    //When an item from the option button is selected, update the SelectionManager selectin appropriately
    private void optionSelect(long thisIsJustUsedToMatchSignature) {
        //We start by clearing the selection
        SelectionManager.Instance.selectOneType();

        //Then selecting what's in each OptionButton
        if (firstPointInput.GetSelectedId() != -1) {
            (int shapeId, int pointId) pointInfo = (0, firstPointInput.GetSelectedId());
            GD.Print($"Selecting first point: {pointInfo}");
            SelectionManager.Instance.selectPoint(pointInfo);

            if (secondPointInput.GetSelectedId() != -1) {
                (int shapeId, int pointId) pointInfo2 = (0, secondPointInput.GetSelectedId());
                SelectionManager.Instance.selectPoint(pointInfo2, true);
            }
        
        }

    }

    private void addTheCurve() {
        int firstPointID = firstPointInput.GetSelectedId();
        int secondPointID = secondPointInput.GetSelectedId();

        if (firstPointID == secondPointID) {
            GD.Print("Cannot add a curve with the same point selected twice.");
            return;
        }
        VisualizationManager.Instance.AddSegment(0, firstPointID, secondPointID);

        VisualizationManager.Instance.EndSegmentAddingVisualization();

        // AddCurveTabs.Instance.closeTabs();

    }
}
