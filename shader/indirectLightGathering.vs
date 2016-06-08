#version 430

struct Photon{
	mat4 viewMat;
	vec4 diffuse;
	vec4 position_ws;
	vec4 normal_ws;
};

in vec3 sg_Position;
in vec3 sg_Normal;
in vec3 sg_Color;
in uint sg_PolygonID;

layout (std430, binding = 1) buffer PhotonBuffer {
	Photon photons[];
};

uniform mat4 sg_matrix_cameraToWorld;
uniform mat4 sg_matrix_worldToCamera;
uniform mat4 sg_matrix_cameraToClipping;
uniform mat4 sg_matrix_modelToCamera;
uniform mat4 sg_matrix_modelToClipping;
uniform mat4 perspective;

uniform int photonID;

smooth out vec3 normal_cs;
smooth out vec3 position_cs;
smooth out vec3 vertexColor; 
flat out int ex_PolygonID;

void main(void){
	mat4 viewMat = photons[photonID].viewMat;
	mat4 modelToWorldMat = sg_matrix_cameraToWorld * sg_matrix_modelToCamera;
	
	ex_PolygonID = int(sg_PolygonID);
	vertexColor = sg_Color;
	
	position_cs = (viewMat * modelToWorldMat * vec4(sg_Position, 1.0)).xyz;
	normal_cs = normalize((viewMat * modelToWorldMat * vec4(sg_Normal, 0.0)).xyz);
	
	gl_Position = sg_matrix_cameraToClipping * viewMat * modelToWorldMat * vec4(sg_Position, 1.0);
}