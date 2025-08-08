#ifndef GODOT_OCCT_H
#define GODOT_OCCT_H

#include "core/object/ref_counted.h"

#include <BRepPrimAPI_MakeBox.hxx>
#include <STEPControl_Writer.hxx>
#include <TopoDS_Shape.hxx>

class OCCT : public RefCounted {
	GDCLASS(OCCT, RefCounted);

protected:
	static void _bind_methods();

public:
    //This will include everything in the hello_step main
    void make_box(double d_x, double d_y, double d_z); 

	OCCT();
};

#endif // GODOT_OCCT_H