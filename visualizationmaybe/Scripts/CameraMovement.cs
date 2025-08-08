using Godot;
using System;

public partial class CameraMovement : Node3D
{

    [Export] public NodePath pivotPointPath;
    private Node3D pivotPoint;

    [Export] public NodePath cameraPath;
    private Camera3D camera;


    [Export] public float rotationSpeed = 1.0f; // Speed of rotation around the pivot point
    private float angle = 0.0f;

    private bool _middleMouse = false;
    private bool _panning = false;
    public override void _Ready()
    {
        pivotPoint = GetNode<Node3D>(pivotPointPath);
        if (pivotPoint == null)
        {
            GD.PrintErr("Pivot point not found at path: " + pivotPointPath);
        }
        camera = GetNode<Camera3D>(cameraPath);
        if (camera == null)
        {
            GD.PrintErr("Camera not found at path: " + cameraPath);
        }
    }

    public override void _UnhandledInput(InputEvent @event){
        if (@event is InputEventMouseButton mouseButton) {
            if (mouseButton.ButtonIndex == MouseButton.Middle) {
                _middleMouse = mouseButton.Pressed;

            }
            //Scroll wheel for zooming
            if (mouseButton.ButtonIndex == MouseButton.WheelUp || mouseButton.ButtonIndex == MouseButton.WheelDown) {
                float zoomFactor = mouseButton.ButtonIndex == MouseButton.WheelUp ? -3f : 3f;
                ZoomCamera(zoomFactor);
            }


        }

        if (@event is InputEventKey keyEvent)
        {
            if (keyEvent.Keycode == Key.Shift)
            {
                _panning = keyEvent.Pressed;
            }
        }


        if ( @event is InputEventMouseMotion mouseMotion)
        {
            if (_middleMouse && !_panning)
            {
                //Rotate camera around pivot point when middle mouse button is pressed
                RotateCameraAroundPivot(mouseMotion.Relative);
            }
            if( _middleMouse && _panning){
                PanCamera(mouseMotion.Relative);
            }
        }
    }
    
    private void ZoomCamera(float zoomFactor)
    {
        // Calculate the zoom direction based on camera's forward vector
        Vector3 zoomDirection = camera.GlobalTransform.Basis.Z * zoomFactor;
        
        // Move the camera position
        camera.GlobalPosition += zoomDirection;
    }
    
    private void PanCamera(Vector2 mouseDelta) {

        // Calculate the pan offset based on mouse movement in camera space
        Vector3 panOffset = new Vector3(-mouseDelta.X * rotationSpeed, mouseDelta.Y * rotationSpeed, 0);

        // Transform the offset to world space using camera's basis
        Vector3 worldOffset = camera.Transform.Basis * panOffset;

        // Move the camera position
        camera.GlobalPosition += worldOffset;
        pivotPoint.GlobalPosition += worldOffset;
    }

    private void RotateCameraAroundPivot(Vector2 mouseDelta)
    {
        if (pivotPoint == null || camera == null) return;

        // Calculate rotation based on mouse movement
        float horizontalRotation = -mouseDelta.X * 0.01f;
        float verticalRotation = -mouseDelta.Y * 0.01f;

        // Store current position relative to pivot
        Vector3 offset = camera.GlobalPosition - pivotPoint.GlobalPosition;

        // Rotate around Y axis (horizontal mouse movement)
        offset = offset.Rotated(Vector3.Up, horizontalRotation);

        // Rotate around local right axis (vertical mouse movement)
        Vector3 rightAxis = camera.Transform.Basis.X;
        offset = offset.Rotated(rightAxis, verticalRotation);

        // Apply new position
        camera.GlobalPosition = pivotPoint.GlobalPosition + offset;

        // Make camera look at pivot point
        camera.LookAt(pivotPoint.GlobalPosition, Vector3.Up);
    }

    public override void _Process(double delta)
    {
    }
}
