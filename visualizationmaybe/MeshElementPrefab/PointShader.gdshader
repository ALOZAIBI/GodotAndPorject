shader_type spatial;

const vec3 unSelected = vec3(.8,0,.8);
const vec3 selected = vec3(1,.6,0);

varying vec3 col;

//If the elementID's index is true the element is selected
uniform bool[1000] selectedElements;
//Custom0.r holds this element's ID
void vertex() {

	if(selectedElements[int(CUSTOM0.r)]){
		col = selected;
		POINT_SIZE = 8.0;
	}
	else{
		col = unSelected;
		POINT_SIZE = 5.0;
	}
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
