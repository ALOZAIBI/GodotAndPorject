shader_type spatial;
render_mode cull_disabled, unshaded;

const vec3 unSelected = vec3(.1,.1,.25);
const vec3 selected = vec3(.5,.3,0);

varying vec3 col;
varying float alb;

//If the elementID's index is true the element is selected
uniform bool[1000] selectedElements;
//Custom0.r holds this element's ID

void vertex() {
	if(selectedElements[int(CUSTOM0.r)]){
		col = selected;
		alb = 0.95f;
	}
	else{
		col = unSelected;
		alb = 0.5f;
	}
}

void fragment() {
	ALBEDO = col;
	ALPHA = alb;
}