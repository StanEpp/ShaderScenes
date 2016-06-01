#version 430

flat in uint ex_PolygonID;
smooth in vec3 normal;

layout(location = 0) out uvec4 outID;
layout(location = 1) out vec4 outNormal;

void main(void){
	outID = uvec4(ex_PolygonID, 0, 0, 0);
	outNormal = vec4(normal, 0);
}