using Godot;
using System;

public partial class SelectionButtonColorer : Panel {
    
    public static SelectionButtonColorer Instance { get; private set; }
    
    [Export] private Button surfaceButton;
    [Export] private Button curveButton;
    [Export] private Button pointButton;
    
    // Colors for button states
    private readonly Color selectedColor = new Color(1.0f, 1.0f, 0.0f, 1.0f); // Yellow
    private readonly Color defaultColor = new Color(0.7f, 0.7f, 0.7f, 1.0f);  // Gray

    public override void _EnterTree() {
        Instance = this;
    }
    public override void _Ready() {

        // Set all buttons to black initially
        SetAllButtonsDefault();

        // Set the default selected button (Point mode is default)
        SetSelectedButton(ElementType.Point);
    }
    
    // Sets all buttons to black color
    private void SetAllButtonsDefault() {
        if (surfaceButton != null) {
            surfaceButton.Modulate = defaultColor;
        }
        if (curveButton != null) {
            curveButton.Modulate = defaultColor;
        }
        if (pointButton != null) {
            pointButton.Modulate = defaultColor;
        }
    }
    
    // Sets the specified button to yellow and others to black
    public void SetSelectedButton(ElementType selectionMode) {
        SetAllButtonsDefault();
        switch (selectionMode) {
            case ElementType.Surface:
                surfaceButton.Modulate = selectedColor;
                break;
            case ElementType.Curve:
                curveButton.Modulate = selectedColor;
                break;
            case ElementType.Point:
                pointButton.Modulate = selectedColor;
                break;
        }
    }
    
}
