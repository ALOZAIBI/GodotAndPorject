shader_type spatial;

const vec3 unSelected = vec3(.4,.4,1);
const vec3 selected = vec3(1,.6,0);

varying vec3 col;

//If the elementID's index is true the element is selected
uniform bool[1000] selectedElements;
//Custom0.r holds this element's ID

void vertex() {
	if(selectedElements[int(CUSTOM0.r)]){
		col = selected;
	}
	else{
		col = unSelected;
	}
}

void fragment() {
	EMISSION = col;
}