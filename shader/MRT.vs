#version 430

in vec3 sg_Position;
in vec3 sg_Normal;

out vec4 position;
out vec3 normal;

uniform mat4 sg_matrix_cameraToWorld;
uniform mat4 sg_matrix_modelToCamera;
uniform mat4 sg_matrix_modelToClipping;

void main() {
	position = sg_matrix_cameraToWorld * sg_matrix_modelToCamera * vec4(sg_Position, 1.0);
	normal = normalize((sg_matrix_cameraToWorld * sg_matrix_modelToCamera * vec4(sg_Normal, 0.0)).xyz);
	gl_Position = sg_matrix_modelToClipping * vec4(sg_Position, 1.0);
}
