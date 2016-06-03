#version 430

in vec3 sg_Position;
in vec2 sg_TexCoord0;

uniform mat4 sg_matrix_modelToClipping;

smooth out vec2 texCoord;

void main(void){
	texCoord = sg_TexCoord0;
	gl_Position = sg_matrix_modelToClipping * vec4(sg_Position, 1.0);
}