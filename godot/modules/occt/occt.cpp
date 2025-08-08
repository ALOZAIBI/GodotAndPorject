#include "occt.h"

void OCCT::make_box(double d_x, double d_y, double d_z) {
	// Create a box with the given dimensions
	BRepPrimAPI_MakeBox boxMaker(d_x, d_y, d_z);
	TopoDS_Shape shape = boxMaker.Shape();

	// Write the shape to a STEP file
	STEPControl_Writer writer;
	writer.Transfer(shape, STEPControl_AsIs);
	writer.Write("box.step");
}

void OCCT::_bind_methods() {

	ClassDB::bind_method(D_METHOD("make_box", "x","y","z"), &OCCT::make_box);
}

OCCT::OCCT() {
}

//Run the below command both to run the godot editor and when buildingthe engine
//export LD_LIBRARY_PATH=/usr/local/lib:$LD_LIBRARY_PATH