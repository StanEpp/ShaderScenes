#version 430

struct Photon{
	mat4 viewMat;
	vec4 diffuse;
	vec4 position_ws;
	vec4 normal_ws;
};

flat in vec2 texCoord;
flat in uint photonID;

layout (std430, binding = 1) buffer PhotonBuffer {
	Photon photons[];
};

layout(location = 0) uniform sampler2D posTexture;
layout(location = 1) uniform sampler2D normalTexture;

void main(void) {
	vec3 Dir = texture(normalTexture, texCoord).xyz;
	
	if(Dir.x < 0.01f && Dir.y < 0.01f && Dir.z < 0.01f){
		photons[photonID].viewMat = mat4(0.f);
		return;
	}
	
	vec3 Pos = texture(posTexture, texCoord).xyz;
	vec3 Up = vec3(0.f, 1.f, 0.f);
	
	vec3 bZ = normalize(-1.f * Dir);
	vec3 bX = normalize(cross(Up, bZ));
	vec3 bY = cross(bZ, bX);
	
	mat4x4 mat = mat4x4(1.f);
	
	mat[0] = vec4(bX, 0);
	mat[1] = vec4(bY, 0);
	mat[2] = vec4(bZ, 0);
	mat[3] = vec4(-1.f * Pos, 1);
	
	photons[photonID].viewMat = mat4x4(1.f);
	photons[photonID].position_ws = vec4(Pos, 1.f);
	photons[photonID].normal_ws = vec4(normalize(Dir), 0.f);
}
