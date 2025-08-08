using Godot;
using System;

public partial class UndoRedoGraphic : Control {
    public static UndoRedoGraphic Instance { get; private set; }
    [Export] private Button undoButton;
    [Export] private Button redoButton;

    //The different save states will be shown inside this scroll container as buttons
    [Export] private VBoxContainer vBoxContainer;

    public override void _EnterTree() {
        Instance = this;
    }

    public override void _Ready() {

        undoButton.Pressed += VisualizationManager.Instance.undo;
        redoButton.Pressed += VisualizationManager.Instance.redo;
    }

    public void updateStackView() {
        // Clear existing buttons (children of vBoxContainer
        foreach (Node child in vBoxContainer.GetChildren()) {
            child.QueueFree();
        }

        int currentUndoStackIndex = VisualizationManager.Instance.GetCurrentStatePosition();

        // Get the current save states from the OCC manager
        for (int i = 0; i < VisualizationManager.Instance.GetUndoStack().Length; i++) {
            string saveStateName = VisualizationManager.Instance.GetUndoStack()[i];
            Button saveStateButton = new Button();
            if (i == currentUndoStackIndex) {
                saveStateButton.Modulate = new Color(1.0f, 1.0f, 0.0f, 1.0f); // Yellow for current state
            }
            //Make the button Expand to fill the width of the VBoxContainer
            saveStateButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            saveStateButton.Text = saveStateName;
            
            // Capture the current value of i in a local variable to avoid closure issues
            int buttonIndex = i;
            saveStateButton.Pressed += () => VisualizationManager.Instance.LoadSaveState(buttonIndex);
            vBoxContainer.AddChild(saveStateButton);
        }
        
        
    }
}
