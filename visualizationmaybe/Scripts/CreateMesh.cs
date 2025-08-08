using Godot;
using System;
using System.Collections.Generic;
using System.IO;
public partial class CreateMesh : MeshInstance3D
{
    private int _rings = 5;
    private int _radialSegments = 5;
    private float _radius = 1;

    public override void _Ready()
    {
        if (Directory.Exists("step"))
        {
            GD.Print("REady");
            //Print the file names inside the "res://step" directory.
            string[] fileNames = Directory.GetFiles("step");
            foreach (string fileName in fileNames)
            {
                GD.Print(fileName);
            }
        }


        // Insert setting up the surface array and lists here.
        Godot.Collections.Array surfaceArray = [];
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        // C# arrays cannot be resized or expanded, so use Lists to create geometry.
        List<Vector3> verts = [];
        List<Vector2> uvs = [];
        List<Vector3> normals = [];
        List<int> indices = [];
        // Vertex indices.
        var thisRow = 0;
        var prevRow = 0;
        var point = 0;

        // Loop over rings.
        for (var i = 0; i < _rings + 1; i++)
        {
            var v = ((float)i) / _rings;
            var w = Mathf.Sin(Mathf.Pi * v);
            var y = Mathf.Cos(Mathf.Pi * v);

            // Loop over segments in ring.
            for (var j = 0; j < _radialSegments + 1; j++)
            {
                var u = ((float)j) / _radialSegments;
                var x = Mathf.Sin(u * Mathf.Pi * 2);
                var z = Mathf.Cos(u * Mathf.Pi * 2);
                var vert = new Vector3(x * _radius * w, y * _radius, z * _radius * w);
                verts.Add(vert);
                normals.Add(vert.Normalized());
                uvs.Add(new Vector2(u, v));
                point += 1;

                // Create triangles in ring using indices.
                if (i > 0 && j > 0)
                {
                    indices.Add(prevRow + j - 1);
                    indices.Add(prevRow + j);
                    indices.Add(thisRow + j - 1);

                    indices.Add(prevRow + j);
                    indices.Add(thisRow + j);
                    indices.Add(thisRow + j - 1);
                }
            }

            prevRow = thisRow;
            thisRow = point;
        }

        // Insert committing to the ArrayMesh here.
        // Convert Lists to arrays and assign to surface array
        surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(1, 1, 1) // white, or any color you prefer
        };


        var arrMesh = Mesh as ArrayMesh;
        if (arrMesh != null)
        {
            // Create mesh surface from mesh array
            // No blendshapes, lods, or compression used.
            arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
            arrMesh.SurfaceSetMaterial(0, material);
        }

        //Print vertex and index counts
        GD.Print("Vertices count: ", verts.Count);
        GD.Print("Indices count: ", indices.Count);
        //Print information about the mesh
        GD.Print("Mesh created with " + arrMesh.GetSurfaceCount() + " surfaces.");
        //Print the position of the mesh
        GD.Print("Mesh position: ", GlobalPosition);
        
    }
    
    public override void _Process(double delta)
    {

    }
}