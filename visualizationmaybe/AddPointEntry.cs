using Godot;
using System;

public partial class AddPointEntry : Panel {
    public static AddPointEntry Instance;
    [Export] private Button confirmAddPointButton;
    [Export] private SpinBox xInput;
    [Export] private SpinBox yInput;
    [Export] private SpinBox zInput;

    public override void _Ready() {
        Instance = this;
        confirmAddPointButton.Pressed += AddThePoint;
        xInput.ValueChanged += VisualizeAddPoint;
        yInput.ValueChanged += VisualizeAddPoint;
        zInput.ValueChanged += VisualizeAddPoint;
    }
    
    public void closePanel() {
        if (Visible) {
            Visible = false;
            VisualizationManager.Instance.EndPointAddingVisualization();
        }
    }

    public void openPanel() {
        if (!Visible) {
            Visible = true;
            //When you're gonna add a point, everything else is deselected
            SelectionManager.Instance.selectOneType();
            VisualizeAddPoint();
            //Hide the other panels
            AddCurveTabs.Instance.closeTabs();
            AddFaceEntry.Instance.closePanel();
        }
    }
    
    //The double is needed to match the signature of the function called by the ValueChanged event
    public void VisualizeAddPoint(double value = 0) {
        float x = (float)xInput.Value;
        float y = (float)yInput.Value;
        float z = (float)zInput.Value;
        VisualizationManager.Instance.VisualizeAddPoint(x, y, z);
    }

    private void AddThePoint() {
        VisualizationManager.Instance.AddPoint(0, xInput.Value, yInput.Value, zInput.Value);

        //Reset the input fields
        Visible = false; // Hide the panel after adding the point
        
        VisualizationManager.Instance.EndPointAddingVisualization();
    }
    
}
