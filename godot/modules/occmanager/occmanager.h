#ifndef GODOT_OCCManager_H
#define GODOT_OCCManager_H

#include <vector>
#include "core/object/ref_counted.h"

#include <TopoDS_Shape.hxx>
#include <TopoDS_Edge.hxx>

class OCCManager : public RefCounted {
	GDCLASS(OCCManager, RefCounted);

private:
	//Contains a list of OCC shapes
	std::vector<TopoDS_Shape> shapes;
	//Contains a list of lists of vertices. The 0th index contains the vertices of the 0th shape etc etc...
	std::vector<std::vector<gp_Pnt>> shape_vertices;
	//A list of list of edges
	std::vector<std::vector<TopoDS_Edge>> shape_edges;

	//---------------------------UNDO STUFF---------------------------
	//Basically contains a list of all the previous lists for each point in the undo stack
	std::vector<std::vector<TopoDS_Shape>> undo_shapes;
	std::vector<std::vector<std::vector<gp_Pnt>>> undo_shape_vertices;
	std::vector<std::vector<std::vector<TopoDS_Edge>>> undo_shape_edges;
	std::vector<String> undo_state_names;
	int position_in_stack = -1;

	bool visualization_active = false;

	//A 2d vector of ints.
	//The outer vector is for each shape.
	//The inner vector. Its index is the curve ID, its value is the last index of the visual edges that correspond to that curve.
	//So [0][0] = 20, means the first 

	void mesh_shape(int shape_index);

	//Saves the shape in the list + its points in the shape_points vector
	//By default it saves it in a new index, but if shape_index is specified, it saves it in that index (Updating information of a prexisting shape)
	void store_shape(const TopoDS_Shape &shape,int shape_index = -1);

	void delete_shape(int shape_index);

	void store_vertices(int shape_index = -1);

	void store_edges(int shape_index = -1);



	void print_undo_stack();

protected:
	static void _bind_methods();


public:
	//Imports a step file and stores in shapes
	void import_step(const String &p_path);

	int get_shape_count() const;

	PackedVector3Array get_visual_vertices(int shape_index) const;
	//Returns the indices of the triangles for the visual mesh
	PackedInt32Array get_visual_indices(int shape_index) const;

	//Returns a tuple
	//[0] = PackedVector3Array representing the triangles
	//[1] = PackedInt32Array holding the face ID of each triangle
	Array get_faces(int shape_index) const;
	

	PackedVector3Array get_edges(int shape_index) const;
	PackedVector3Array get_vertices(int shape_index) const;

	void add_point(int shape_index,double x,double y, double z);
	void add_edge(int shape_index,int firstPointID, int secondPointID);
	void add_arc_circle(int shape_index, int startPointID, int endPointID, int centerPointID);
	void add_spline(int shape_index, Array pointIDs);


	void add_surface(int shape_index, Array edgeIds);
	
	//Adds a shape that will be used purely for visualization
	//If currently visualizing then this is called, it'll delete the previous visualization then visualize once more
	//This will visualize stuff that are tough to visualize without OCC. For example visualizing adding point and simple segment is easy, but for arcs etc etc we use this.
	void visualize();

	void end_visualization();

	//This is public so that we can call it from godot.
	//And this is only the case because of the very dirty add face workaround
	//Otherwise putting it as private would've been best
	void save_state(const String &state_name);

	//This one is public in the case we want to jump to a specific state without going through each step of undo/redo
	//Returns true if the stats is changed and false if the state is already at that position
	bool load_state(int position);

	void undo();

	void redo();

	PackedStringArray get_undo_stack() const;

	int get_current_state_position() const;
	OCCManager();
};

#endif // GODOT_OCCManager_H