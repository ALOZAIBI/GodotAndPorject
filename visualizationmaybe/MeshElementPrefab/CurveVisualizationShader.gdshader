shader_type spatial;

varying vec3 col;


void vertex() {
	col = vec3(1,0.4f,0) ;
}

void fragment() {
	EMISSION = col;
}