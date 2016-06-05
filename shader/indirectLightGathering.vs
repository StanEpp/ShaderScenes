#version 430

in vec3 sg_Position;
in vec3 sg_Normal;
in vec3 sg_Color;
in uint sg_PolygonID;

uniform mat4 sg_matrix_cameraToWorld;
uniform mat4 sg_matrix_worldToCamera;
uniform mat4 sg_matrix_cameraToClipping;
uniform mat4 sg_matrix_modelToCamera;
uniform mat4 sg_matrix_modelToClipping;
uniform mat4 perspective;

smooth out vec3 normal_cs;
smooth out vec3 position_cs;
smooth out vec3 vertexColor; 

flat out int ex_PolygonID;

mat4 modelToWorld() {return sg_matrix_cameraToWorld * sg_matrix_modelToCamera;}

void main(void){
	vec3 E = vec3(12.5f, 14.89f, -31.0577f);
	vec3 C = E + vec3(0.f, 0.f, -1.f);
	vec3 Up = normalize(vec3(0.f, 0.f, 1.f));
	vec3 F = normalize(C - E);
	vec3 S = normalize(cross(F, Up));
	vec3 U = normalize(cross(S, F));
	
	mat4x4 mv = mat4x4(1.f);
	mv[0] = vec4(S, 0);
	mv[1] = vec4(U, 0);
	mv[2] = vec4(-1.f * F, 0);
	mv[3] = vec4(-1.f * E, 1);
	
	ex_PolygonID = int(sg_PolygonID);
	position_cs = (sg_matrix_modelToCamera * vec4(sg_Position, 1.0)).xyz;
	normal_cs = normalize((sg_matrix_modelToCamera * vec4(sg_Normal, 0.0)).xyz);
	vertexColor = sg_Color;
	gl_Position = sg_matrix_modelToClipping * vec4(sg_Position, 1.0);
}