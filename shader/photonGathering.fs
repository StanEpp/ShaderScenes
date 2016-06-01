#version 430
#extension GL_NV_shader_atomic_float : require

const int DIRECTIONAL = 1;
const int POINT = 2;
const int SPOT = 3;

struct sg_LightSourceParameters {
	int type; 							// has to be DIRECTIONAL, POINT or SPOT
	vec3 position; 						// position of the light  ????????????????????????????????
	vec3 direction; 					// direction of the light, has to be normalized ????????????????????????????????
	vec4 ambient, diffuse, specular;	// light colors for all lights
	float constant, linear, quadratic;	// attenuations for point & spot lights
	float exponent, cosCutoff;			// spot light parameters
};

struct Photon{
	float R, G, B, A;
};

smooth in vec3 normal;
smooth in vec3 position;
smooth in vec3 color;
flat in int ex_PolygonID;

layout(r32ui, binding = 0) restrict uniform uimageBuffer lightpatchTex;

layout (std430, binding = 1) buffer PhotonBuffer {
	Photon photons[];
};

uniform sg_LightSourceParameters sg_LightSource[8];

uniform int photonID;

void main(void){
	uint lightPatch = imageLoad(lightpatchTex, ex_PolygonID).x;
	uint lightID = 1;
	vec3 indirectLight = vec3(0);
	
	while(lightPatch != 0){
		uint lightIDBitMask = 1 << (lightID-1);
		uint lightIsUsed = lightPatch & lightIDBitMask;
		
		if(lightIsUsed != 0){
			sg_LightSourceParameters light = sg_LightSource[lightID - 1];
			
			// TODO: Compute indirect light
			
			atomicAdd(photons[photonID].R, indirectLight.r);
			atomicAdd(photons[photonID].G, indirectLight.g);
			atomicAdd(photons[photonID].B, indirectLight.b);
		}
		
		lightPatch &= ~(lightIDBitMask); 
		lightID++;
	}
}