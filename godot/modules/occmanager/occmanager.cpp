#include "occmanager.h"
#include <STEPControl_Reader.hxx>
#include <BRepMesh_IncrementalMesh.hxx>
#include <BRepBuilderAPI_MakeVertex.hxx>
#include <BRepBuilderAPI_MakeEdge.hxx>
#include <BRepBuilderAPI_MakeWire.hxx>
#include <BRepCheck_Wire.hxx>
#include <BRepBuilderAPI_MakeFace.hxx>
#include <BRep_Builder.hxx>
#include <TopExp_Explorer.hxx>
#include <TopTools_IndexedMapOfShape.hxx>
#include <TopExp.hxx>
#include <TopoDS_Face.hxx>
#include <TopoDS.hxx>

#include <Poly_Triangulation.hxx>
#include <BRep_Tool.hxx>
#include <TColgp_Array1OfPnt.hxx>
#include <gp_Pnt.hxx>
#include <gp_Pln.hxx>
#include <gp_Vec.hxx>
#include <Standard_Handle.hxx>
#include <Geom_Line.hxx>
#include <Geom_TrimmedCurve.hxx>
#include <GC_MakeArcOfCircle.hxx>
#include <Geom_BSplineCurve.hxx>
#include <GeomAPI_PointsToBSpline.hxx>
#include <TColgp_Array1OfPnt.hxx>
#include <set>

OCCManager::OCCManager() {

}

void OCCManager::save_state(const String &state_name) {
    //If this isn't the most recent position, remove all later states first
    if ((position_in_stack != -1) && (position_in_stack < undo_shapes.size() - 1)) {
        undo_shapes.erase(undo_shapes.begin() + position_in_stack + 1, undo_shapes.end());
        undo_shape_vertices.erase(undo_shape_vertices.begin() + position_in_stack + 1, undo_shape_vertices.end());
        undo_shape_edges.erase(undo_shape_edges.begin() + position_in_stack + 1, undo_shape_edges.end());
        undo_state_names.erase(undo_state_names.begin() + position_in_stack + 1, undo_state_names.end());
    }

    // Save the current state of shapes, vertices, and edges to the undo stack
    undo_shapes.push_back(shapes);
    undo_shape_vertices.push_back(shape_vertices);
    undo_shape_edges.push_back(shape_edges);
    undo_state_names.push_back(state_name);

    position_in_stack = undo_shapes.size() - 1;
    print_undo_stack();
}

//Goes to previous state
void OCCManager::undo() {
    if (position_in_stack > 0) {
        load_state(position_in_stack - 1);
    }

}

//Goes to next state
void OCCManager::redo() {
    if (position_in_stack < undo_shapes.size() - 1) {
        load_state(position_in_stack + 1);
    }
}

//Goes to position state
bool OCCManager::load_state(int position) {
    if(position_in_stack == position){
        return false;
    }
    if (position < undo_shapes.size()) {
        shapes = undo_shapes[position];
        shape_vertices = undo_shape_vertices[position];
        shape_edges = undo_shape_edges[position];
    }
    position_in_stack = position;
    print_undo_stack();
    return true;
}

void OCCManager::print_undo_stack() {
    print_line("=== Undo Stack Debug ===");
    print_line("Stack size: " + String::num_int64(undo_shapes.size()));
    print_line("Current position: " + String::num_int64(position_in_stack));
    
    // Print each state in the stack with indicators
    for (int i = 0; i < undo_state_names.size(); i++) {
        String line = "State " + String::num_int64(i) + ": " + undo_state_names[i];

        if (i == position_in_stack) {
            line += "   <--Current";
        } else if (i == position_in_stack + 1 && position_in_stack + 1 < undo_state_names.size()) {
            line += "   <--Redo";
        } else if (i == position_in_stack - 1) {
            line += "   <--Undo";
        }
        
        print_line(line);
    }
    print_line("========================");
}

PackedStringArray OCCManager::get_undo_stack() const {
    PackedStringArray stack;
    for (const String &state_name : undo_state_names) {
        stack.push_back(state_name);
    }
    return stack;
}

int OCCManager::get_current_state_position() const {
    return position_in_stack;
}

void OCCManager::delete_shape(int shape_index){

    if(shape_index < 0 || shape_index >= shapes.size()) {
        ERR_PRINT("Shape index out of bounds");
        return;
    }


    shapes.erase(shapes.begin() + shape_index);
    shape_vertices.erase(shape_vertices.begin() + shape_index);
    shape_edges.erase(shape_edges.begin() + shape_index);

}

//If shape_index is -1, it will store the shape in a new index
void OCCManager::store_shape(const TopoDS_Shape &shape,int shape_index){
    if(shape_index < 0){
        shapes.push_back(shape);
        store_vertices(-1);
        store_edges(-1);
    }
    else{
        if(shape_index >= shapes.size()) {
            ERR_PRINT("Shape index out of bounds");
            return;
        }
        shapes[shape_index] = shape;
        store_vertices(shape_index);
        store_edges(shape_index);
    }

    // //Print the number of shapes/vertices/edges stored
    // print_line("Number of shapes stored: " + String::num_int64(shapes.size()));
    // print_line("Number of vertices stored: " + String::num_int64(shape_vertices.size()));
    // print_line("Number of edges stored: " + String::num_int64(shape_edges.size()));

    // //Number of vertices/edges for the first shape
    // if (shapes.size() > 0) {
    //     print_line("Number of vertices for the first shape: " + String::num_int64(shape_vertices[0].size()));
    //     print_line("Number of edges for the first shape: " + String::num_int64(shape_edges[0].size()));
    // }
}

void OCCManager::store_vertices(int shape_index) {
    TopoDS_Shape shape; 
    if(shape_index < 0){
        shape = shapes[shapes.size() - 1]; // Get the last shape if no index is specified
    }
    else{
        shape = shapes[shape_index];
    }

    std::vector<gp_Pnt> vertices;
    //So that we don't get duplicate vertices
    TopTools_IndexedMapOfShape vertex_map;
    TopExp::MapShapes(shape, TopAbs_VERTEX, vertex_map); // unique vertices only

    //This tolerance value is used to avoid the add_edge function from adding vertices.
    //So when we store vertices we wont store duplicates
    //This is probably a band aid fix but it's okay time is tight
    const double tolerance = 1e-6; 

    for (int i = 1; i <= vertex_map.Extent(); ++i) {
        TopoDS_Vertex vertex = TopoDS::Vertex(vertex_map.FindKey(i));
        gp_Pnt point = BRep_Tool::Pnt(vertex);

        // Check if this point is too close to an existing one
        bool is_duplicate = false;
        for (const auto& existing_point : vertices) {
            if (point.Distance(existing_point) < tolerance) {
                is_duplicate = true;
                break;
            }
        }
        
        if (!is_duplicate) {
            vertices.push_back(point);
        }
    }
    //Add the vertices to a new shape in shape_vertices
    if(shape_index < 0){
        shape_vertices.push_back(vertices);
    }
    else{
        if(shape_index >= shape_vertices.size() ) {
            ERR_PRINT("Shape index out of bounds for vertices storage");
            return;
        }
        if(shape_vertices.size() != shapes.size()) {
            ERR_PRINT("Shape vertices size does not match shapes size");
            return;
        }
        shape_vertices[shape_index] = vertices; // Update existing shape's vertices
    }


}

void OCCManager::store_edges(int shape_index) {
    TopoDS_Shape shape;
    if(shape_index < 0){
        shape = shapes[shapes.size() - 1]; // Get the last shape if no index is specified
    }
    else{
        shape = shapes[shape_index];
    }

    // So that we don't get duplicate edges
    TopTools_IndexedMapOfShape edge_map;
    TopExp::MapShapes(shape, TopAbs_EDGE, edge_map);   // unique edges only

    const double tolerance = 1e-6; 

    std::vector<TopoDS_Edge> edges;
    for (int i = 1; i <= edge_map.Extent(); ++i) {
        TopoDS_Edge edge = TopoDS::Edge(edge_map.FindKey(i));
        
        // Get edge endpoints for comparison
        TopoDS_Vertex v1, v2;
        TopExp::Vertices(edge, v1, v2);
        gp_Pnt p1 = BRep_Tool::Pnt(v1);
        gp_Pnt p2 = BRep_Tool::Pnt(v2);
        
        // Check if this edge is too close to an existing one
        bool is_duplicate = false;
        for (const auto& existing_edge : edges) {
            TopoDS_Vertex ev1, ev2;
            TopExp::Vertices(existing_edge, ev1, ev2);
            gp_Pnt ep1 = BRep_Tool::Pnt(ev1);
            gp_Pnt ep2 = BRep_Tool::Pnt(ev2);
            
            // Check if endpoints match (either direction)
            if ((p1.Distance(ep1) < tolerance && p2.Distance(ep2) < tolerance) ||
                (p1.Distance(ep2) < tolerance && p2.Distance(ep1) < tolerance)) {
                
                // Endpoints match, now check if the curves are geometrically identical
                Standard_Real first1, last1, first2, last2;
                Handle(Geom_Curve) curve1 = BRep_Tool::Curve(edge, first1, last1);
                Handle(Geom_Curve) curve2 = BRep_Tool::Curve(existing_edge, first2, last2);
                
                if (!curve1.IsNull() && !curve2.IsNull()) {
                    // Sample points along both curves to compare their geometry
                    bool curves_identical = true;
                    const int num_samples = 4;
                    
                    for (int s = 0; s <= num_samples && curves_identical; ++s) {
                        Standard_Real t1 = first1 + (last1 - first1) * s / num_samples;
                        Standard_Real t2 = first2 + (last2 - first2) * s / num_samples;
                        
                        gp_Pnt pt1 = curve1->Value(t1);
                        gp_Pnt pt2 = curve2->Value(t2);
                        
                        // If the curves are different, sample points will differ
                        if (pt1.Distance(pt2) > tolerance) {
                            curves_identical = false;
                        }
                    }
                    
                    if (curves_identical) {
                        is_duplicate = true;
                        break;
                    }
                } else {
                    // If we can't get curves, fall back to endpoint-only comparison
                    // This handles degenerate cases
                    is_duplicate = true;
                    break;
                }
            }
        }
        
        if (!is_duplicate) {
            edges.push_back(edge);
        }
    }
    print_line("Ready to store the edges into shape_edges");

    if (shape_index < 0) {
        shape_edges.push_back(edges);
    } else {
        if (shape_index >= shape_edges.size()) {
            ERR_PRINT("Shape index out of bounds for edges storage");
            return;
        }
        if (shape_edges.size() != shapes.size()) {
            ERR_PRINT("Shape edges size does not match shapes size");
            return;
        }
        shape_edges[shape_index] = edges; // Update existing shape's edges
    }


}

void OCCManager::import_step(const String &p_path) {
    //print_line("Importing STEP file");
	STEPControl_Reader reader;
	std::string native_path = p_path.utf8().get_data();
	
	if (reader.ReadFile(native_path.c_str()) != IFSelect_RetDone) {
		ERR_PRINT("Failed to read STEP file: " + p_path);
		return;
	}

	//print_line("STEP file read successfully, transferring entities...");
	
	// Get number of roots for diagnostics
	Standard_Integer nb_roots = reader.NbRootsForTransfer();
	//print_line("Number of root entities found: " + String::num_int64(nb_roots));
	
	if (nb_roots == 0) {
		ERR_PRINT("No transferable root entities found in STEP file");
		return;
	}
	
	// Try to transfer roots with more detailed error reporting
	Standard_Integer transfer_result = reader.TransferRoots();
	
	if (transfer_result == 0) {
		//print_line("Transfer failed, attempting alternative method...");
		
		// Try alternative transfer method for problematic files
		Standard_Integer nb_transferred = 0;
		
		for (Standard_Integer i = 1; i <= nb_roots; ++i) {
			if (reader.TransferOne(i)) {
				nb_transferred++;
			}
		}
		
		if (nb_transferred == 0) {
			ERR_PRINT("Failed to transfer any entities from STEP file");
			return;
		}
		
		//print_line("Successfully transferred " + String::num_int64(nb_transferred) + " out of " + String::num_int64(nb_roots) + " entities");
	} else {
		//print_line("Successfully transferred " + String::num_int64(transfer_result) + " entities");
	}
	
    //print_line("Getting combined shape...");
	TopoDS_Shape shape = reader.OneShape();
	
	if (shape.IsNull()) {
		ERR_PRINT("Resulting shape is null after transfer");
		return;
	}

    print_line("Shape obtained successfully, now storing shape...");
    //Save the shape
	store_shape(shape);

    //print_line("Shape imported successfully, now meshing...");
    mesh_shape(shapes.size() - 1);
}

void OCCManager::mesh_shape(int shape_index){
	BRepMesh_IncrementalMesh(shapes[shape_index], 0.1);
}



//Gets vertices for the visual mesh
PackedVector3Array OCCManager::get_visual_vertices(int shape_index) const {
    TopoDS_Shape shape = shapes[shape_index];
    PackedVector3Array vertices;

    TopExp_Explorer face_explorer(shape, TopAbs_FACE);
    while (face_explorer.More()) {
        TopoDS_Face face = TopoDS::Face(face_explorer.Current());
        TopLoc_Location loc;

        Handle(Poly_Triangulation) triangulation = BRep_Tool::Triangulation(face, loc);
        if (!triangulation.IsNull()) {
            const gp_Trsf& trsf = loc.Transformation();
            int num_nodes = triangulation->NbNodes();

            for (int i = 1; i <= num_nodes; ++i) { // OCC indices start at 1
                gp_Pnt p = triangulation->Node(i).Transformed(trsf);
                vertices.append(Vector3(p.X(), p.Y(), p.Z()));
            }
        }

        face_explorer.Next();
    }

    return vertices;
}

//Gets indices for the visual mesh
PackedInt32Array OCCManager::get_visual_indices(int shape_index) const {
    TopoDS_Shape shape = shapes[shape_index];
    PackedInt32Array indices;

    TopExp_Explorer face_explorer(shape, TopAbs_FACE);
    int vertex_offset = 0;

    while (face_explorer.More()) {
        TopoDS_Face face = TopoDS::Face(face_explorer.Current());
        TopLoc_Location loc;

        Handle(Poly_Triangulation) triangulation = BRep_Tool::Triangulation(face, loc);
        if (!triangulation.IsNull()) {
            int num_nodes = triangulation->NbNodes();
            int num_tris = triangulation->NbTriangles();

            // Check face orientation
            bool reverse_orientation = (face.Orientation() == TopAbs_REVERSED);

            for (int i = 1; i <= num_tris; ++i) {
                int n1, n2, n3;
                triangulation->Triangle(i).Get(n1, n2, n3);

                // OCC indices are 1-based, Godot expects 0-based
                if (reverse_orientation) {
                    // Reverse winding order for reversed faces
                    indices.append(vertex_offset + n1 - 1);
                    indices.append(vertex_offset + n3 - 1);
                    indices.append(vertex_offset + n2 - 1);
                } else {
                    indices.append(vertex_offset + n1 - 1);
                    indices.append(vertex_offset + n2 - 1);
                    indices.append(vertex_offset + n3 - 1);
                }
            }

            vertex_offset += num_nodes;
        }

        face_explorer.Next();
    }

    return indices;
}

//Gets the faces of the shape
//Look at .h file for the format of the returned array
Array OCCManager::get_faces(int shape_index) const {
    PackedVector3Array tris;
    //its size is tris/3. Each entry represents the ith triangle's face ID.
    //So first entry is the face ID of the first triangle(3 vertices) etc etc...
    PackedInt32Array tri_id;
    const TopoDS_Shape& shape = shapes[shape_index];

    TopTools_IndexedMapOfShape face_map;
    TopExp::MapShapes(shape, TopAbs_FACE, face_map);

    int currentID = 0; // Used to track the current face ID
    for (int i = 1; i <= face_map.Extent(); ++i) {
        TopoDS_Face face = TopoDS::Face(face_map.FindKey(i));

        TopLoc_Location loc;
        Handle(Poly_Triangulation) tri = BRep_Tool::Triangulation(face, loc);
        if (tri.IsNull()) continue;

        gp_Trsf tr = loc.Transformation();
        const Poly_Array1OfTriangle& triangles = tri->Triangles();
        bool reversed = (face.Orientation() == TopAbs_REVERSED);

        for (int t = triangles.Lower(); t <= triangles.Upper(); ++t) {
            Standard_Integer n1, n2, n3;
            triangles(t).Get(n1, n2, n3);
            if (reversed) std::swap(n2, n3);

            gp_Pnt p1 = tri->Node(n1).Transformed(tr);
            gp_Pnt p2 = tri->Node(n2).Transformed(tr);
            gp_Pnt p3 = tri->Node(n3).Transformed(tr);

            tris.append(Vector3(p1.X(), p1.Y(), p1.Z()));
            tris.append(Vector3(p2.X(), p2.Y(), p2.Z()));
            tris.append(Vector3(p3.X(), p3.Y(), p3.Z()));

            tri_id.append(currentID);
        }
        currentID++;
    }
    // //Print the number of triangles and faces
    // print_line("FROM OCCManager::get_faces");
    // print_line("Number of triangles: " + String::num_int64(tris.size()));
    // print_line("Number of faces: " + String::num_int64(tri_id.size()));

    Array ret;
    ret.append(tris);
    ret.append(tri_id);
    return ret;
}


//Gets occ vertices
PackedVector3Array OCCManager::get_vertices(int shape_index) const {
    PackedVector3Array vertices;

    for(const auto& vertex: shape_vertices[shape_index]) {
        vertices.append(Vector3(vertex.X(), vertex.Y(), vertex.Z()));
    }
    return vertices;
}




PackedVector3Array OCCManager::get_edges(int shape_index) const {
    PackedVector3Array edges;
    

    const int divisions = 32;


    for (const TopoDS_Edge& edge : shape_edges[shape_index]) {
        Standard_Real first, last;
        Handle(Geom_Curve) curve = BRep_Tool::Curve(edge, first, last);
        if (curve.IsNull()) continue;

        // Respect orientation so you don't sample reversed twice later
        if (edge.Orientation() == TopAbs_REVERSED)
            std::swap(first, last);

        gp_Pnt prev = curve->Value(first);
        for (int s = 1; s <= divisions; ++s) {
            Standard_Real t = first + (last - first) * s / divisions;
            gp_Pnt cur = curve->Value(t);
            edges.append(Vector3(prev.X(), prev.Y(), prev.Z()));
            edges.append(Vector3(cur.X(),  cur.Y(),  cur.Z()));
            prev = cur;
        }
    }
    return edges;
}

void OCCManager::add_point(int shape_index,double x, double y, double z){
    if (shape_index < 0 || shape_index >= shapes.size()) {
        ERR_PRINT("Invalid shape index");
        return;
    }
    // Create a point vertex
    gp_Pnt point(x, y, z);
    
    TopoDS_Vertex vertex = BRepBuilderAPI_MakeVertex(point);

    // Add the vertex to the existing shape using BooleanAPI or compound
    BRep_Builder builder;
    TopoDS_Compound compound;
    builder.MakeCompound(compound);
    builder.Add(compound, shapes[shape_index]);
    builder.Add(compound, vertex);

    // Re-mesh the modified shape
    mesh_shape(shape_index);

    //Update the stored value
    store_shape(compound,shape_index);
}

//The new shape will be in the last index of shapes
void OCCManager::visualize() {
    if(visualization_active){
        //If visualization is already active, delete the last shape
        end_visualization();
    }
    // Create an empty compound instead of a null shape
    BRep_Builder builder;
    TopoDS_Compound empty_compound;
    builder.MakeCompound(empty_compound);
    
    store_shape(empty_compound);

    visualization_active = true;
}

void OCCManager::end_visualization() {
    if(visualization_active){
        //Deletes the last shape
        if (shapes.empty()) {
            ERR_PRINT("No shapes to end visualization");
            return;
        }
        delete_shape(shapes.size() - 1);
        visualization_active = false;
    }
}

void OCCManager::add_edge(int shape_index, int firstPointID, int secondPointID) {
    if (shape_index < 0 || shape_index >= shapes.size()) {
        ERR_PRINT("Invalid shape index");
        return;
    }
    
    if (firstPointID < 0 || firstPointID >= shape_vertices[shape_index].size() ||
        secondPointID < 0 || secondPointID >= shape_vertices[shape_index].size()) {
        ERR_PRINT("Invalid point ID");
        return;
    }
    
    // Get existing points from stored vertices
    const gp_Pnt &point1 = shape_vertices[shape_index][firstPointID];
    const gp_Pnt &point2 = shape_vertices[shape_index][secondPointID];

    // Create an edge directly between the two points
    TopoDS_Edge edge = BRepBuilderAPI_MakeEdge(point1, point2);
    
    // Add the edge to the existing shape using compound
    BRep_Builder builder;
    TopoDS_Compound compound;
    builder.MakeCompound(compound);
    builder.Add(compound, shapes[shape_index]);
    builder.Add(compound, edge);
    
    // Re-mesh the modified shape
    mesh_shape(shape_index);

    // Update the stored value
    store_shape(compound, shape_index);
    // shapes[shape_index] = compound;
}

void OCCManager::add_arc_circle(int shape_index, int startPointID, int centerPointID, int endPointID){
    const gp_Pnt &centerPoint = shape_vertices[shape_index][centerPointID];
    const gp_Pnt &startPoint = shape_vertices[shape_index][startPointID];
    const gp_Pnt &endPoint = shape_vertices[shape_index][endPointID];

    Handle(Geom_TrimmedCurve) arc = GC_MakeArcOfCircle(startPoint, centerPoint, endPoint);
    TopoDS_Edge edge = BRepBuilderAPI_MakeEdge(arc);

    // Add the edge to the existing shape using compound
    BRep_Builder builder;
    TopoDS_Compound compound;
    builder.MakeCompound(compound);
    builder.Add(compound, shapes[shape_index]);
    builder.Add(compound, edge);

    // Re-mesh the modified shape
    mesh_shape(shape_index);
    // Update the stored value
    store_shape(compound, shape_index);
}

void OCCManager::add_spline(int shape_index, Array pointIDs) {
    if (shape_index < 0 || shape_index >= shapes.size()) {
        ERR_PRINT("Invalid shape index");
        return;
    }
    
    if (pointIDs.size() < 2) {
        ERR_PRINT("Need at least 2 points to create a spline curve");
        return;
    }
    
    // Check if all point IDs are valid
    for (int i = 0; i < pointIDs.size(); ++i) {
        int pointID = pointIDs[i];
        if (pointID < 0 || pointID >= shape_vertices[shape_index].size()) {
            ERR_PRINT("Invalid point ID: " + String::num_int64(pointID));
            return;
        }
    }
    
    // Create array of points for the spline
    TColgp_Array1OfPnt points(1, pointIDs.size());
    for (int i = 0; i < pointIDs.size(); ++i) {
        int pointID = pointIDs[i];
        const gp_Pnt &point = shape_vertices[shape_index][pointID];
        points(i + 1) = point; // OCC arrays are 1-based
    }
    
    try {
        // Create B-spline curve through the points
        GeomAPI_PointsToBSpline splineGenerator(points);
        
        if (!splineGenerator.IsDone()) {
            ERR_PRINT("Failed to generate B-spline curve");
            return;
        }
        
        Handle(Geom_BSplineCurve) splineCurve = splineGenerator.Curve();
        
        if (splineCurve.IsNull()) {
            ERR_PRINT("Generated spline curve is null");
            return;
        }
        
        // Create edge from the spline curve
        TopoDS_Edge edge = BRepBuilderAPI_MakeEdge(splineCurve);
        
        // Add the edge to the existing shape using compound
        BRep_Builder builder;
        TopoDS_Compound compound;
        builder.MakeCompound(compound);
        builder.Add(compound, shapes[shape_index]);
        builder.Add(compound, edge);
        
        // Re-mesh the modified shape
        mesh_shape(shape_index);
        
        // Update the stored value
        store_shape(compound, shape_index);
        
    } catch (const Standard_Failure& e) {
        ERR_PRINT("Exception while creating spline curve: " + String(e.GetMessageString()));
    }
}


void OCCManager::add_surface(int shape_index, Array edgeIds){
    std::vector<TopoDS_Edge> edges;
    for (int i = 0; i < edgeIds.size(); ++i) {
        int edgeId = edgeIds[i];
        if (edgeId < 0 || edgeId >= shape_edges[shape_index].size()) {
            ERR_PRINT("Invalid edge ID: " + String::num_int64(edgeId));
            continue;
        }
        edges.push_back(shape_edges[shape_index][edgeId]);
    }

    if (edges.empty()) {
        ERR_PRINT("No valid edges found");
        return;
    }

    BRepBuilderAPI_MakeWire wireBuilder;
    for (const TopoDS_Edge &edge : edges) {
        wireBuilder.Add(edge);
    }
    wireBuilder.Build();
    TopoDS_Wire wire = wireBuilder.Wire();

    //Check if the wire forms a closed loops
    BRepCheck_Wire checkWire(wire);

    if((!checkWire.Closed() == BRepCheck_NoError)) {
        ERR_PRINT("Wire is not closed, cannot create face");
        return;
    }

    // Create a face from the wire
    BRepBuilderAPI_MakeFace faceBuilder(wire,true);

    if(!faceBuilder.IsDone()) {
        ERR_PRINT("Failed to create face from wire: " + String::num_int64(faceBuilder.Error()));
        return;
    }
    TopoDS_Face face = faceBuilder.Face();
    if(face.IsNull()) {
        ERR_PRINT("Created face is null");
        return;
    }



    // Add the face to the existing shape using compound
    BRep_Builder builder;
    TopoDS_Compound compound;
    builder.MakeCompound(compound);
    builder.Add(compound, shapes[shape_index]);
    builder.Add(compound, face);

    // Re-mesh the modified shape
    mesh_shape(shape_index);

    // Update the stored value
    store_shape(compound, shape_index);
}


int OCCManager::get_shape_count() const {
	return shapes.size();
}

void OCCManager::_bind_methods() {
	ClassDB::bind_method(D_METHOD("import_step", "path"), &OCCManager::import_step);
	ClassDB::bind_method(D_METHOD("get_shape_count"), &OCCManager::get_shape_count);
	ClassDB::bind_method(D_METHOD("get_visual_vertices", "shape_index"), &OCCManager::get_visual_vertices);
	ClassDB::bind_method(D_METHOD("get_visual_indices", "shape_index"), &OCCManager::get_visual_indices);
    ClassDB::bind_method(D_METHOD("get_edges", "shape_index"), &OCCManager::get_edges);
    ClassDB::bind_method(D_METHOD("get_vertices", "shape_index"), &OCCManager::get_vertices);
    ClassDB::bind_method(D_METHOD("get_faces", "shape_index"), &OCCManager::get_faces);
    ClassDB::bind_method(D_METHOD("add_point", "shape_index", "x", "y", "z"), &OCCManager::add_point);
    ClassDB::bind_method(D_METHOD("add_edge", "shape_index", "firstPointID", "secondPointID"), &OCCManager::add_edge);
    ClassDB::bind_method(D_METHOD("add_arc", "shape_index", "startPointID", "centerPointID", "endPointID"), &OCCManager::add_arc_circle);
    ClassDB::bind_method(D_METHOD("add_spline", "shape_index", "pointIDs"), &OCCManager::add_spline);
    ClassDB::bind_method(D_METHOD("visualize"), &OCCManager::visualize);
    ClassDB::bind_method(D_METHOD("end_visualization"), &OCCManager::end_visualization);
    ClassDB::bind_method(D_METHOD("add_surface", "shape_index", "verticeIDs"), &OCCManager::add_surface);
    ClassDB::bind_method(D_METHOD("save_state", "state_name"), &OCCManager::save_state);
    ClassDB::bind_method(D_METHOD("load_state", "position"), &OCCManager::load_state);
    ClassDB::bind_method(D_METHOD("undo"), &OCCManager::undo);
    ClassDB::bind_method(D_METHOD("redo"), &OCCManager::redo);
    ClassDB::bind_method(D_METHOD("get_undo_stack"), &OCCManager::get_undo_stack);
    ClassDB::bind_method(D_METHOD("get_current_state_position"), &OCCManager::get_current_state_position);

}


//Run the below command both to run the godot editor and when buildingthe engine
//export LD_LIBRARY_PATH=/usr/local/lib:$LD_LIBRARY_PATH