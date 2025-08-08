shader_type spatial;
varying vec3 col;

//Custom0.r holds this curve's ID
//Custom0.g if = 0, when .r is not the same as my ID reset me to unselected
// if = 1 keep me selected. (When it is 1 we can visualize the fact that multiple edges are selected)
void vertex() {
	col = vec3(1,1,0);
	POINT_SIZE = 12.0;
}

void fragment() {
	vec2 p = (POINT_COORD * 2.0) - 1.0;
    float dist = dot(p, p);
    if (dist > 1.0) {
        discard;
    }
	EMISSION = col;
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
