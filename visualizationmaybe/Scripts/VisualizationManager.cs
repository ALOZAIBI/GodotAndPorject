using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class VisualizationManager : Node3D {
    public static VisualizationManager Instance { get; private set; }
    private OccManager occManager;

    private List<MeshInstance3D> visualizationMeshes = new List<MeshInstance3D>();
 

    //Single mesh for edges per shape
    public List<MeshInstance3D> curveMeshes = new List<MeshInstance3D>();
    //Outerlist is the shape, inner list is the curves in that shape
    public List<List<CurveStruct>> curves = new List<List<CurveStruct>>();

    //Single mesh for points per shape
    public List<MeshInstance3D> pointMeshes = new List<MeshInstance3D>();
    //Outerlist is the shape, inner list is the points in that shape
    public List<List<PointStruct>> points = new List<List<PointStruct>>();

    //Single mesh for surfaces per shape
    public List <MeshInstance3D> surfaceMeshes = new List<MeshInstance3D>();
    //Outerlist is the shape, inner list is the surfaces in that shape
    public List<List<SurfaceStruct>> surfaces = new List<List<SurfaceStruct>>();
    [Export] public ShaderMaterial curveShaderMaterial;
    [Export] public ShaderMaterial curveVisualizationShaderMaterial;

    //Prefab used to visualize the addition of a curve
    [Export] private PackedScene curveVisualizationPrefab;
    
    private MeshInstance3D curveVisualizationObject;

    [Export] public ShaderMaterial pointShaderMaterial;
    //If used for visualizing adding a point, use shader
    [Export] private ShaderMaterial pointAddingVisualizationShaderMaterial;
    //Then save it in this mesh
    private MeshInstance3D pointAddingVisualizationObject;


    [Export] public ShaderMaterial surfaceShaderMaterial;
    [Export] private ShaderMaterial surfaceVisualizationShaderMaterial;

    private void DrawMesh(int shapeIndex = 0) {
        Vector3[] vertices = occManager.GetVisualVertices(shapeIndex);
        int[] indices = occManager.GetVisualIndices(shapeIndex);

        // Check if we have valid data to create a mesh
        if (vertices == null || vertices.Length == 0 || indices == null || indices.Length == 0) {
            GD.Print($"No mesh data available for shape {shapeIndex}");
            return;
        }

        var mesh = new ArrayMesh();
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Index] = indices;


        var material = new StandardMaterial3D {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoColor = new Color(1, 1, 1, 0.2f) // White color for the material
        };

        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        mesh.SurfaceSetMaterial(0, material);


        var mesh_instance = new MeshInstance3D();
        mesh_instance.Mesh = mesh;
        AddChild(mesh_instance);

        visualizationMeshes.Add(mesh_instance);
        mesh_instance.Visible = false; // By default, the mesh is not visible
    }

    //For now this updates alll shapes, later make it only update the desired shape
    public void Draw(bool debug = false) {

        foreach (var mesh in visualizationMeshes) {
            mesh.QueueFree();
        }
        visualizationMeshes.Clear();

        foreach (var mesh in curveMeshes) {
            mesh.QueueFree();
        }
        curveMeshes.Clear();

        foreach (var mesh in pointMeshes) {
            mesh.QueueFree();
        }
        pointMeshes.Clear();
        
        foreach (var mesh in surfaceMeshes) {
            mesh.QueueFree();
        }
        surfaceMeshes.Clear();

        for (int i = 0; i < occManager.GetShapeCount(); i++) {
            DrawMesh(i);

            //The visualization parameter is used when visualizing an addition
            //Here using the i == 1 we'll only visualize the second shape which is indeed used for visualization
            DrawEdges(i, i == 1);
            DrawPoints(i);
            DrawSurfaces(i, i == 1);

            if (debug) {
                GD.Print($"Drawing shape {i}");
                var points = occManager.GetVertices(i);
                for (int j = 0; j < points.Length; j++) {
                    GD.Print($"Point {j}: {points[j]}");
                }
            }
        }

    }
    private void DrawEdges(int shapeIndex = 0, bool visualization = false, bool debug = false) {
        Vector3[] edgePoints = occManager.GetEdges(shapeIndex);

        SurfaceTool st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Lines);

        //The R will hold the edge ID
        st.SetCustomFormat(0, SurfaceTool.CustomFormat.RFloat);
        int edgeID = 0;

        //Clears the appropriate shape in the curves list
        if (shapeIndex < curves.Count) {
            curves[shapeIndex].Clear();
        }
        for (int i = 0; i < edgePoints.Length; i += 2) {

            //Every edge is divided into 16 segments, so we increase the edgeID every 16 segments
            //This is to make sure that the edgeID is unique for each edge
            if (i % (32 * 2) == 0 && i > 0) {
                edgeID++;
            }


            //Add this shape (outer list) to the curves if not done yet
            if (curves.Count < occManager.GetShapeCount()) {
                curves.Add(new List<CurveStruct>());
            }
            curves[shapeIndex].Add(
                new CurveStruct(edgePoints[i], edgePoints[i + 1], edgeID));

            var property = new Color(edgeID, 0, 0, 0);
            st.SetCustom(0, property);
            st.AddVertex(edgePoints[i]);
            st.AddVertex(edgePoints[i + 1]);
        }

        var lineMesh = st.Commit();

        MeshInstance3D curveMeshInstance = new MeshInstance3D();
        curveMeshInstance.Mesh = lineMesh;
        if (visualization) {
            curveMeshInstance.MaterialOverride = curveVisualizationShaderMaterial;
        } else {
            curveMeshInstance.MaterialOverride = curveShaderMaterial;
        }

        AddChild(curveMeshInstance);
        curveMeshes.Add(curveMeshInstance);

        //Print the curves for debugging
        if (debug) {
            // GD.Print($"Shape {shapeIndex} has {curves[shapeIndex].Count} curves:");
            // foreach (var curve in curves[shapeIndex]) {
            //     GD.Print($"Curve ID: {curve.ElementID}, Start: {curve.start}, End: {curve.end}");
            // }
            //Print the number of curves in the shape
            GD.Print($"Shape {shapeIndex} has {curves[shapeIndex].Count} curves.");
        }

    }

    private void DrawPoints(int shapeIndex = 0) {
        Vector3[] listOfPoints = occManager.GetVertices(shapeIndex);
        SurfaceTool st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Points);
        //The R will hold the point ID
        st.SetCustomFormat(0, SurfaceTool.CustomFormat.RFloat);

        //Clears the appropriate shape in the points list
        if (shapeIndex < points.Count) {
            points[shapeIndex].Clear();
        }
        for (int i = 0; i < listOfPoints.Length; i++) {

            //Add this shape (outer list) to the points if not done yet
            if (points.Count < occManager.GetShapeCount()) {
                points.Add(new List<PointStruct>());
            }
            points[shapeIndex].Add(new PointStruct(listOfPoints[i], i));

            var property = new Color(i, 0, 0, 0);
            st.SetCustom(0, property);
            st.AddVertex(listOfPoints[i]);
        }

        var pointMesh = st.Commit();

        MeshInstance3D pointMeshInstance = new MeshInstance3D();
        pointMeshInstance.Mesh = pointMesh;
        pointMeshInstance.MaterialOverride = pointShaderMaterial;

        AddChild(pointMeshInstance);
        pointMeshes.Add(pointMeshInstance);
    }

    private void DrawSurfaces(int shapeIndex = 0, bool visualization = false,bool debug = false) {
        //FaceInfo[0] is a PackedVector3Array with the vertices of the triangles making up the face
        //FaceInfo[1] is a PackedInt32Array with the IDs per triangle
        var faceInfo = occManager.GetFaces(shapeIndex);
        Vector3[] tris = (Vector3[])faceInfo[0];
        int[] triIDs = (int[])faceInfo[1];

        SurfaceTool st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        //The R will hold the face ID
        st.SetCustomFormat(0, SurfaceTool.CustomFormat.RFloat);

        //Clears the appropriate shape in the surfaces list
        if (shapeIndex < surfaces.Count) {
            surfaces[shapeIndex].Clear();
        }

        for (int vertex = 0, triIDIndex = 0; vertex < tris.Length; vertex += 3, triIDIndex++) {
            //Add this shape (outer list) to the surfaces if not done yet
            if (surfaces.Count < occManager.GetShapeCount()) {
                surfaces.Add(new List<SurfaceStruct>());
            }

            surfaces[shapeIndex].Add(new SurfaceStruct(
                tris[vertex], tris[vertex + 1], tris[vertex + 2], triIDs[triIDIndex]));

            var property = new Color(triIDs[triIDIndex], 0, 0, 0);
            st.SetCustom(0, property);
            st.AddVertex(tris[vertex]);
            st.AddVertex(tris[vertex + 1]);
            st.AddVertex(tris[vertex + 2]);
        }

        var surfaceMesh = st.Commit();
        MeshInstance3D surfaceMeshInstance = new MeshInstance3D();
        surfaceMeshInstance.Mesh = surfaceMesh;

        if (visualization) {
            surfaceMeshInstance.MaterialOverride = surfaceVisualizationShaderMaterial;
        } else {
            surfaceMeshInstance.MaterialOverride = surfaceShaderMaterial;
        }

        AddChild(surfaceMeshInstance);
        surfaceMeshes.Add(surfaceMeshInstance);

        if (debug) {
            //Prin thte number of surfaces in the shape
            GD.Print($"Shape {shapeIndex} has {surfaces[shapeIndex].Count} surfaces.");
        }
    }
    

    public void ToggleMeshesVisibility() {
        foreach (var mesh in visualizationMeshes) {
            mesh.Visible = !mesh.Visible;
        }
    }
    public void ToggleSurfacesVisibility() {
        foreach (var surface in surfaceMeshes) {
            surface.Visible = !surface.Visible;
        }
    }
    public void ToggleEdgesVisibility() {

        foreach (var curve in curveMeshes) {
            curve.Visible = !curve.Visible;
        }
    }

    public void TogglePointsVisibility() {

        foreach (var pointMesh in pointMeshes) {
            pointMesh.Visible = !pointMesh.Visible;
        }
    }
    public void AddSegment(int shapeIndex, int firstPointID, int secondPointID) {
        occManager.AddEdge(shapeIndex, firstPointID, secondPointID);
        occManager.SaveState("Segment added");
        UndoRedoGraphic.Instance.updateStackView();
        Draw();
    }

    public void AddArc(int shapeIndex, int firstPointID, int centerPointID, int secondPointID, bool saveState = true) {
        occManager.AddArc(shapeIndex, firstPointID, centerPointID, secondPointID);
        if (saveState) {
            occManager.SaveState("Arc added");
            UndoRedoGraphic.Instance.updateStackView();
        }
        Draw();
    }
    public void VisualizeAddSegment(int firstPointID, int secondPointID, int shapeIndex = 0) {

        Vector3 firstPoint = points[shapeIndex][firstPointID].position;
        Vector3 secondPoint = points[shapeIndex][secondPointID].position;
        if (firstPoint == secondPoint) {
            GD.Print("Cannot visualize curve with same start and end point");
            EndSegmentAddingVisualization();
            return;
        }

        if (curveVisualizationObject == null) {
            curveVisualizationObject = curveVisualizationPrefab.Instantiate<MeshInstance3D>();
            curveVisualizationObject.Name = "CurveVisualizationObject";
            AddChild(curveVisualizationObject);
        }
        //Positions it correctly
        Vector3 midPoint = (firstPoint + secondPoint) / 2;
        Vector3 direction = (secondPoint - firstPoint);
        float length = direction.Length();
        //Angles it and scales it

        curveVisualizationObject.LookAtFromPosition(midPoint, secondPoint, Vector3.Back);
        curveVisualizationObject.Scale = new Vector3(0.3f, 0.3f, length);
    }
    public void EndSegmentAddingVisualization() {
        if (curveVisualizationObject != null) {
            curveVisualizationObject.QueueFree();
            curveVisualizationObject = null;
        }
    }
    public void AddPoint(int shapeIndex, double x, double y, double z,bool saveState = true) {
        occManager.AddPoint(shapeIndex, x, y, z);
        if (saveState) {
            occManager.SaveState("Point added");
            UndoRedoGraphic.Instance.updateStackView();
        }
        Draw();
    }
    
    //This is called whenever the value of the addPointEntry is modified
    public void VisualizeAddPoint(float x, float y, float z) {
        if (pointAddingVisualizationObject == null) {
            SurfaceTool st = new SurfaceTool();
            st.Begin(Mesh.PrimitiveType.Points);
            //Put one point at 0,0,0
            st.AddVertex(Vector3.Zero);

            pointAddingVisualizationObject = new MeshInstance3D();
            pointAddingVisualizationObject.Mesh = st.Commit();
            pointAddingVisualizationObject.MaterialOverride = pointAddingVisualizationShaderMaterial;

            AddChild(pointAddingVisualizationObject);
        }
        pointAddingVisualizationObject.Position = new Vector3((float)x, (float)y, (float)z);
    }

    //When the actual point is added, we remove the visualization object

    public void EndPointAddingVisualization() {
        if (pointAddingVisualizationObject != null) {
            pointAddingVisualizationObject.QueueFree();
            pointAddingVisualizationObject = null;
        }
    }
    //We don't need to give shape_index occManager autoamtically uses the last shape as visualization
    public void VisualizeAddArc(int firstPointID, int centerPointID, int secondPointID) {
        //Make sure that no two points are the same
        if (firstPointID == centerPointID || firstPointID == secondPointID || centerPointID == secondPointID) {
            GD.Print("Cannot visualize arc with same start, center or end point");
            return;
        }

        //Creates the empty shape for visualization
        occManager.Visualize();

        //Gets the last shape index since the visualizsing shape is always the last one
        int shapeIndex = occManager.GetShapeCount() - 1;
        //Now to visualize we'dd add an arc to the empty shape.
        //However to add an arc we need the points to exist in that shape
        //So we add the points to the shape
        //We're gonna assume we're visuallizing the first shape cuz for now we only use the first shape, and the seecond shape is onyl for visualizing
        Vector3 firstPoint = points[0][firstPointID].position;
        Vector3 centerPoint = points[0][centerPointID].position;
        Vector3 secondPoint = points[0][secondPointID].position;
        AddPoint(shapeIndex, firstPoint.X, firstPoint.Y, firstPoint.Z,false);
        AddPoint(shapeIndex, centerPoint.X, centerPoint.Y, centerPoint.Z,false);
        AddPoint(shapeIndex, secondPoint.X, secondPoint.Y, secondPoint.Z,false);
        //We created 3 points so the IDs are 0, 1, 2
        AddArc(shapeIndex, 0, 1, 2,false);

    }

    public void EndArcAddingVisualization() {
        occManager.EndVisualization();
        //Draw to update the view such that the deleted meshes are no longer displayed
        Draw();
    }

    public void AddFace(int shapeIndex, Godot.Collections.Array<int> edgeIDs) {
        occManager.AddSurface(shapeIndex, (Godot.Collections.Array)edgeIDs);

        //This is a very dirty workaround, we will add a dummy point just to trigger the update. I am not sure why we have to do this, but it works, and I need to progress fast, so ya...
        //The dummy point will be added exactly over the first point of the first shape, since in OCC saving the vertices we don't save duplicate points or points that share the same coordinate.
        AddPoint(shapeIndex, points[0][0].position.X, points[0][0].position.Y, points[0][0].position.Z,false);

        occManager.SaveState("Face added");
        UndoRedoGraphic.Instance.updateStackView();
        Draw(); // This should now include the face because of the point addition triggering refresh

    }

    public void AddSpline(int shapeIndex, Godot.Collections.Array<int> pointIDs,bool saveState = true) {
        GD.Print("Adding spline with points: ", pointIDs);
        occManager.AddSpline(shapeIndex, (Godot.Collections.Array)pointIDs);
        if (saveState) {
            occManager.SaveState("Spline added");
            UndoRedoGraphic.Instance.updateStackView();
        }
        Draw();
    }

    public void VisualizeAddSpline(Godot.Collections.Array<int> pointIDs) {
        //Make sure that no two points are the same
        if (pointIDs.Count < 2) {
            GD.Print("Cannot visualize spline with less than 2 points");
            return;
        }

        //End any existing visualization first
        int currentShapeCount = occManager.GetShapeCount();
        if (currentShapeCount > 1) {
            occManager.EndVisualization();
        }

        //Creates the empty shape for visualization
        occManager.Visualize();
        
        //Gets the last shape index since the visualizing shape is always the last one
        int shapeIndex = occManager.GetShapeCount() - 1;

        GD.Print("Visualizing spline with points: ", pointIDs);

        Godot.Collections.Array<int> shapePointIDs = new Godot.Collections.Array<int>();
        //Now to visualize we'dd add a spline to the empty shape.
        //However to add a spline we need the points to exist in that shape
        //So we add the points to the shape
        int newPointID = 0;
        foreach (var pointID in pointIDs) {
            Vector3 pointPosition = points[0][pointID].position;
            AddPoint(shapeIndex, pointPosition.X, pointPosition.Y, pointPosition.Z, false);
            //We update the pointIDS to reflect the newly created shape's poitns IDs, so 0 -> pointIDs.size()
            shapePointIDs.Add(newPointID);
            newPointID++;
        }
        //We created points so we can now add a spline with those points
        AddSpline(shapeIndex, shapePointIDs,false);
    }

    public void EndSplineAddingVisualization() {
        occManager.EndVisualization();
        //Draw to update the view such that the deleted meshes are no longer displayed
        Draw();
    }
    public void highlightElements() {
        highlightCurves(SelectionManager.Instance.selectedCurves);
        highlightPoints(SelectionManager.Instance.selectedPoints);
        highlightSurfaces(SelectionManager.Instance.selectedSurfaces);
    }

    //Highlights the selected curves
    private void highlightCurves(List<int> selectedCurves) {
        //Array with the indices representing the curves ID, if the index is true then the associated curve is highlighted
        var highlightArray = new Godot.Collections.Array<bool>();
        highlightArray.Resize(1000);
        highlightArray.Fill(false);
        foreach (var curve in selectedCurves) {
            highlightArray[curve] = true;
        }

        curveShaderMaterial.SetShaderParameter("selectedElements", highlightArray);
    }

    private void highlightPoints(List<int> selectedPoints) {
        //Array with the indices representing the points ID, if the index is true then the associated point is highlighted
        var highlightArray = new Godot.Collections.Array<bool>();
        highlightArray.Resize(1000);
        highlightArray.Fill(false);
        foreach (var point in selectedPoints) {
            highlightArray[point] = true;
        }

        pointShaderMaterial.SetShaderParameter("selectedElements", highlightArray);
    }

    private void highlightSurfaces(List<int> selectedSurfaces) {
        //Array with the indices representing the surfaces ID, if the index is true then the associated surface is highlighted
        var highlightArray = new Godot.Collections.Array<bool>();
        highlightArray.Resize(1000);
        highlightArray.Fill(false);
        foreach (var surface in selectedSurfaces) {
            highlightArray[surface] = true;
        }

        surfaceShaderMaterial.SetShaderParameter("selectedElements", highlightArray);
    }

    public void MultiSelectHighlighting(bool enable) {
        pointShaderMaterial.SetShaderParameter("multiSelect", enable);
        curveShaderMaterial.SetShaderParameter("multiSelect", enable);
    }

    public void undo() {
        occManager.Undo();
        UndoRedoGraphic.Instance.updateStackView();
        Draw();
    }

    public void redo() {
        occManager.Redo();
        UndoRedoGraphic.Instance.updateStackView();
        Draw();
    }

    public void LoadSaveState(int saveStateIndex) {
        GD.Print("Attempting to load save state: " + saveStateIndex);
        if (occManager.LoadState(saveStateIndex)) {
            UndoRedoGraphic.Instance.updateStackView();
            //After loading the save state, we need to redraw the meshes
            Draw();

            GD.Print($"Loaded save state {saveStateIndex}");
        }else {
            GD.Print($"Didn't load save state {saveStateIndex}");
        }
    }

    public string[] GetUndoStack() {
        return occManager.GetUndoStack();
    }

    public int GetCurrentStatePosition() {
        return occManager.GetCurrentStatePosition();
    }
    public override void _Ready() {
        GD.Print("VisualizationManager Ready");
        VisualizationManager.Instance = this;
        occManager = new OccManager();
        GD.Print("ShapesAndStuff Ready");
        GD.Print(occManager.GetShapeCount());

        var absPath = ProjectSettings.GlobalizePath("res://step/box_output.step");

        GD.Print("Before Import");

        occManager.ImportStep(absPath);

        occManager.SaveState("Initial import");
        UndoRedoGraphic.Instance.updateStackView();


        // for(int i = 0; i < occManager.GetShapeCount(); i++) {
        //     DrawMesh(i);
        //     DrawEdges(i);
        //     DrawPoints(i);
        // }
        GD.Print("Before Draw");

        Draw();

        GD.Print("After Draw");



        //To make the shader correctly start with nothing selected
        curveShaderMaterial.SetShaderParameter("selectedCurve", -1);
        pointShaderMaterial.SetShaderParameter("selectedPoint", -1);

        MultiSelectHighlighting(false);

        //By default mesh is not visible
        foreach (var mesh in visualizationMeshes) {
            mesh.Visible = false;
        }
    }
}
