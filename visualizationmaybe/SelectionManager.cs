using Godot;
using System;
using System.Collections.Generic;

public partial class SelectionManager : Node3D {

    public static SelectionManager Instance { get; private set; }
    private Camera3D camera;

    [Export] private PackedScene debugRayCast;
    //This should be equal to the size of the array in the CurveShader - 1 since in the shader the first index is reserved to state the size

    public List<int> selectedCurves = new List<int>();

    //Contains the ids of the selected points
    public List<int> selectedPoints = new List<int>();

    public List<int> selectedSurfaces = new List<int>();

    [Signal] public delegate void PointSelectedEventHandler();

    private bool controlPressed = false;

    private ElementType _selectionMode;
    public ElementType selectionMode
    {
        get
        {
            return _selectionMode;
        }
        set
        {
            _selectionMode = value;
            SelectionButtonColorer.Instance.SetSelectedButton(value);
        }
    }

    public override void _Ready() {
        Instance = this;
        camera = GetViewport().GetCamera3D();
        selectionMode = ElementType.Point;
    }
    public void SelectionModeCurve() {
        selectionMode = ElementType.Curve;
    }
    public void SelectionModePoint() {
        selectionMode = ElementType.Point;
    }
    public void SelectionModeSurface() {
        selectionMode = ElementType.Surface;
    }


    //Left click to select element.
    //Hold ctrl to select multiple elements.
    public override void _UnhandledInput(InputEvent @event) {


        //If control is pressed, able to select multiple elements
        if (@event is InputEventKey keyEvent && keyEvent.Keycode == Key.Ctrl) {
            if (keyEvent.Pressed) {
                controlPressed = true;
            } else {
                //We can stop holding control but the previously selected stuff remain highlighted that's why we dont set multiSelect to false as well
                controlPressed = false;
            }
        }

        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left) {
            switch (selectionMode) {
                case ElementType.Curve:
                    var selectedC = findClosestCurve(20);
                    selectCurve((selectedC.shapeID, selectedC.curveID));
                    break;
                case ElementType.Point:
                    var selectedP = findClosestPoint(20f);
                    //Select point
                    selectPoint((selectedP.shapeID, selectedP.pointID), controlPressed, debug: false);
                    break;
                case ElementType.Surface:
                    var selectedS = findClosestSurface(20f);
                    selectSurface((selectedS.shapeID, selectedS.surfaceID));
                    break;
                default:
                    GD.Print("Unknown selection mode");
                    break;
            }
        }

    }


    //Marks the last selected curve for now, if we want multi select use a hold shift flag in the shader 
    private void selectCurve((int shapeId, int edgeId) edgeInfo) {
        if (edgeInfo.shapeId == -1 || edgeInfo.edgeId == -1) {
            selectOneType();

            GD.Print("No edge selected");
            return;
        }

        //If holding control we simply add the selection
        if (controlPressed) {
            selectedCurves.Add(edgeInfo.edgeId); // Add the selected edge ID to the list
            //TODO Remove duplicates
        } else {
            selectedCurves.Clear(); // Clear previous selections
            selectedCurves.Add(edgeInfo.edgeId); // Add the new selection
        }

        //To ensure that we're only selecting curves, we clear the rest
        selectOneType(ElementType.Curve);

    }

    private void selectSurface((int shapeId, int surfaceId) surfaceInfo) {
        if (surfaceInfo.shapeId == -1 || surfaceInfo.surfaceId == -1) {
            selectOneType();
            GD.Print("No surface selected");
            return;
        }

        //If holding control we simply add the selection
        if (controlPressed) {
            selectedSurfaces.Add(surfaceInfo.surfaceId); // Add the selected surface ID to the list
            //TODO Remove duplicates
        } else {
            selectedSurfaces.Clear(); // Clear previous selections
            selectedSurfaces.Add(surfaceInfo.surfaceId); // Add the new selection
        }

        //To ensure that we're only selecting surfaces, we clear the rest
        selectOneType(ElementType.Surface);
    }

    public void selectPoint((int shapeId, int pointId) pointInfo, bool multiSelect = false, bool debug = false) {
        if (pointInfo.shapeId == -1 || pointInfo.pointId == -1) {
            selectOneType();
            GD.Print("No point selected");
            return;
        }
        //If holding control we simply add the selection
        if (multiSelect) {
            selectedPoints.Add(pointInfo.pointId); // Add the selected point ID to the list
            //TODO Remove duplicates
        } else {
            selectedPoints.Clear(); // Clear previous selections
            selectedPoints.Add(pointInfo.pointId); // Add the new selection
        }

        if (debug) GD.Print("-----------------------");

        //Print the selected points
        if (debug) foreach (var point in selectedPoints) {
                GD.Print($"Selected Point ID: {point}");
            }

        //If we select a point we deselect everything else and highlight the point
        selectOneType(ElementType.Point);

        //Emit signal that a point has been selected
        EmitSignal(SignalName.PointSelected);
    }


    //Clears all selection except the specified element type
    public void selectOneType(ElementType except = ElementType.None) {
        switch (except) {
            case ElementType.Curve:
                selectedPoints.Clear();
                selectedSurfaces.Clear();
                break;
            case ElementType.Point:
                selectedCurves.Clear();
                selectedSurfaces.Clear();
                break;
            case ElementType.Surface:
                selectedPoints.Clear();
                selectedCurves.Clear();
                break;
            case ElementType.None:
                selectedCurves.Clear();
                selectedPoints.Clear();
                selectedSurfaces.Clear();
                break;
            default:
                GD.Print("Unknown element type");
                break;
        }
        //Update the highlighting
        VisualizationManager.Instance.highlightElements();

    }

    //https://math.stackexchange.com/questions/1905533/find-perpendicular-distance-from-point-to-line-in-3d
    //Distance of point to ray
    private float distanceFromSegment(Vector3 rayStart, Vector3 rayEnd, Vector3 point) {
        Vector3 direction = (rayEnd - rayStart) / rayEnd.DistanceTo(rayStart);
        Vector3 startToPoint = point - rayStart;
        float dot = direction.Dot(startToPoint);
        Vector3 projection = rayStart + dot * direction;
        return projection.DistanceTo(point);
    }

    //Find closest point to cursor
    //Returns the shape ID, point ID, and element type
    //If no point is found within the margin, returns -1 for both shape and point
    private (int shapeID, int pointID, ElementType elementType) findClosestPoint(float margin) {
        int rayMaker = 500;
        var mousePos = GetViewport().GetMousePosition();
        var from = camera.ProjectRayOrigin(mousePos);
        var to = from + camera.ProjectRayNormal(mousePos) * rayMaker;

        float minDistance = float.MaxValue;
        int closestShapeId = -1;
        int closestElementId = -1;

        for (int shapeIndex = 0; shapeIndex < VisualizationManager.Instance.points.Count; shapeIndex++) {
            var shape = VisualizationManager.Instance.points[shapeIndex];
            foreach (PointStruct point in shape) {
                float distance = distanceFromSegment(from, to, point.position);
                if (distance < minDistance) {
                    minDistance = distance;
                    closestShapeId = shapeIndex;
                    closestElementId = point.ElementID;
                }
                //Print debug information (Distance and the current element id)
                // GD.Print($"Distance: {distance}, Element ID: {point.ElementID}");
            }
        }

        if (minDistance > margin) {
            // GD.Print($"No point found within the specified margin: {margin}");
            // GD.Print($"Closest Point: Shape ID: {closestShapeId}, Element ID: {closestElementId}, Distance: {minDistance}");
            // Return -1 for both shape and element

            return (-1, -1, ElementType.None); // No point found within the specified margin
        }

        // GD.Print($"Closest Point: Shape ID: {closestShapeId}, Element ID: {closestElementId}, Distance: {minDistance}");
        return (closestShapeId, closestElementId, ElementType.Point);

    }
    //https://stackoverflow.com/questions/541150/connect-two-line-segments/11427699#11427699
    private float distanceBetweenSegments(Vector3 A1, Vector3 A2, Vector3 B1, Vector3 B2) {
        float minDistance = float.MaxValue;

        //Distance of A1 from line B
        float distance = distanceFromSegment(B1, B2, A1);
        if (distance < minDistance) {
            minDistance = distance;
        }
        //Distance of A2 from line B
        distance = distanceFromSegment(B1, B2, A2);
        if (distance < minDistance) {
            minDistance = distance;
        }
        //Distance of B1 from line A
        distance = distanceFromSegment(A1, A2, B1);
        if (distance < minDistance) {
            minDistance = distance;
        }
        //Distance of B2 from line A
        distance = distanceFromSegment(A1, A2, B2);
        if (distance < minDistance) {
            minDistance = distance;
        }

        return minDistance;
    }

    //Find closest curve to cursor
    //Returns the shape ID, curve ID, and element type
    //If no curve is found within the margin, returns -1 for both shape and curve
    private (int shapeID, int curveID, ElementType elementType) findClosestCurve(float margin) {
        int rayMaker = 500;

        var mousePos = GetViewport().GetMousePosition();
        var from = camera.ProjectRayOrigin(mousePos);
        var to = from + camera.ProjectRayNormal(mousePos) * rayMaker;

        float minDistance = float.MaxValue;
        int closestShapeId = -1;
        int closestElementId = -1;
        //Distance between 2 segments is found by finding the smallest distance between every point and the opposing segment
        for (int shapeIndex = 0; shapeIndex < VisualizationManager.Instance.curves.Count; shapeIndex++) {
            var shape = VisualizationManager.Instance.curves[shapeIndex];
            foreach (CurveStruct curve in shape) {
                float distance = distanceBetweenSegments(from, to, curve.start, curve.end);
                if (distance < minDistance) {
                    minDistance = distance;
                    closestShapeId = shapeIndex;
                    closestElementId = curve.ElementID;
                }
                // //Print debug information (Distance and the current element id)
                // GD.Print($"Distance: {distance}, Element ID: {curve.ElementID}");
            }
        }
        if (minDistance > margin) {
            // GD.Print($"No curve found within the specified margin: {margin}");
            // GD.Print($"Closest Curve: Shape ID: {closestShapeId}, Element ID: {closestElementId}, Distance: {minDistance}");
            // Return -1 for both shape and element
            return (-1, -1, ElementType.None); // No curve found within the specified margin
        }

        // GD.Print($"Closest Curve: Shape ID: {closestShapeId}, Element ID: {closestElementId}, Distance: {minDistance}");
        return (closestShapeId, closestElementId, ElementType.Curve);
    }

    //Find closest Surface to cursor
    //Returns the shape ID, surface ID, and element type
    //If no surface is found within the margin, returns -1 for both shape and surface
    //This method works but you can't select surfaces that are behind, only selects the first face 
    private (int shapeID, int surfaceID, ElementType elementType) findClosestSurface(float margin) {
        var mousePos = GetViewport().GetMousePosition();
        var rayOrigin = camera.ProjectRayOrigin(mousePos);
        var rayDirection = camera.ProjectRayNormal(mousePos)*500;

        float closestDistance = float.MaxValue;
        int closestShapeId = -1;
        int closestElementId = -1;

        for (int shapeIndex = 0; shapeIndex < VisualizationManager.Instance.surfaces.Count; shapeIndex++) {
            var shape = VisualizationManager.Instance.surfaces[shapeIndex];
            foreach (SurfaceStruct surface in shape) {
                // Ray-triangle intersection
                var intersection = RayTriangleIntersect(rayOrigin, rayDirection, 
                    surface.vertex1, surface.vertex2, surface.vertex3);
                
                if (intersection.hit && intersection.distance < closestDistance) {
                    closestDistance = intersection.distance;
                    closestShapeId = shapeIndex;
                    closestElementId = surface.ElementID;
                }
            }
        }

        if (closestDistance == float.MaxValue) {
            return (-1, -1, ElementType.None);
        }

        GD.Print($"Closest Surface: Shape ID: {closestShapeId}, Element ID: {closestElementId}, Distance: {closestDistance}");
        return (closestShapeId, closestElementId, ElementType.Surface);
    }

    private (bool hit, float distance) RayTriangleIntersect(Vector3 rayOrigin, Vector3 rayDir, 
        Vector3 v0, Vector3 v1, Vector3 v2) {
        
        const float EPSILON = 0.0000001f;
        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;
        Vector3 h = rayDir.Cross(edge2);
        float a = edge1.Dot(h);
        
        if (a > -EPSILON && a < EPSILON) return (false, 0); // Ray parallel to triangle
        
        float f = 1.0f / a;
        Vector3 s = rayOrigin - v0;
        float u = f * s.Dot(h);
        
        if (u < 0.0f || u > 1.0f) return (false, 0);
        
        Vector3 q = s.Cross(edge1);
        float v = f * rayDir.Dot(q);
        
        if (v < 0.0f || u + v > 1.0f) return (false, 0);
        
        float t = f * edge2.Dot(q);
        
        if (t > EPSILON) return (true, t);
        return (false, 0);
    }
}