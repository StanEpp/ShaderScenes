#version 430
#extension GL_NV_shader_atomic_float : require

struct Photon{
	float R, G, B, A;
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

	atomicAdd(photons[photonID].R, indirectLight.r);
	atomicAdd(photons[photonID].G, indirectLight.g);
	atomicAdd(photons[photonID].B, indirectLight.b);
	atomicAdd(photons[photonID].A, 1.f);
}
