using Godot;
public struct CurveStruct {
    public Vector3 start;
    public Vector3 end;

    public int ElementID;

    public CurveStruct(Vector3 start, Vector3 end, int elementID) {
        this.start = start;
        this.end = end;
        this.ElementID = elementID;
    }
}

public struct PointStruct {
    public Vector3 position;
    public int ElementID;

    public PointStruct(Vector3 position, int elementID) {
        this.position = position;
        this.ElementID = elementID;
    }
}

public struct SurfaceStruct {
    public Vector3 vertex1;
    public Vector3 vertex2;
    public Vector3 vertex3;
    public int ElementID;

    public SurfaceStruct(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, int elementID) {
        this.vertex1 = vertex1;
        this.vertex2 = vertex2;
        this.vertex3 = vertex3;
        this.ElementID = elementID;
    }
}
//Create an enum with point curve face volume
public enum ElementType {
    Point,
    Curve,
    Surface,
    Volume,
    None
}
