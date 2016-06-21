#version 430
#extension GL_NV_shader_atomic_float : require

struct Photon{
	mat4 viewMat;
	vec4 diffuse;
	vec4 position_ws;
	vec4 position_ss;
	vec4 normal_ws;
};

smooth in vec2 texCoord;

layout (std430, binding = 1) buffer PhotonBuffer {
	Photon photons[];
};

uniform int photonID;

layout(location = 0) uniform sampler2D indirectLightTex;

void main(void){
	vec4 indirectLight = texture(indirectLightTex, texCoord);
	
	if (indirectLight.a < 0.1f) return;

	atomicAdd(photons[photonID].diffuse.r, indirectLight.r);
	atomicAdd(photons[photonID].diffuse.g, indirectLight.g);
	atomicAdd(photons[photonID].diffuse.b, indirectLight.b);
	atomicAdd(photons[photonID].diffuse.a, 1.f);
}
