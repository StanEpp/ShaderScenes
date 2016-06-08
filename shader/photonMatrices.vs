#version 430

in vec3 sg_Position;
in vec2 sg_TexCoord0;
in uint sg_PhotonID;

flat out vec2 texCoord;
flat out uint photonID;

uniform mat4 sg_matrix_cameraToClipping;
uniform mat4 sg_matrix_modelToCamera;
uniform mat4 sg_matrix_modelToClipping;

void main() {
	texCoord = sg_TexCoord0;
	photonID = sg_PhotonID;
	gl_Position = sg_matrix_cameraToClipping * vec4(sg_Position, 1.0);
}
