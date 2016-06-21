#version 430

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

struct SurfaceProperties {
	vec3 position_cs, normal_cs, tangent_cs;
	vec4 ambient, diffuse, specular, emission;
	float shininess;
};

struct sg_MaterialParameters {
	vec4 ambient, diffuse, specular; //vec4 emission;?
	float shininess;
};

struct Photon{
	mat4 viewMat;
	vec4 diffuse;
	vec4 position_ws;
	vec4 position_ss;
	vec4 normal_ws;
};

smooth in vec3 normal_cs;
smooth in vec3 position_cs;
smooth in vec3 vertexColor;
flat in int ex_PolygonID;

layout(r32ui, binding = 0) restrict uniform uimageBuffer lightpatchTex;

layout (std430, binding = 1) buffer PhotonBuffer {
	Photon photons[];
};

uniform sg_LightSourceParameters sg_LightSource[8];
uniform int						 sg_lightCount;
uniform sg_MaterialParameters	 sg_Material;
uniform bool					 sg_useMaterials;
uniform int 					 photonID;

layout(location = 0) out vec4 outValue;
layout(location = 1) out vec4 outNormal;

void addLighting(in sg_LightSourceParameters light, in vec3 position_cs, in vec3 normal_cs, in float shininess, inout vec4 diffLightSum){
	// for DIRECTIONAL lights
	float distPixToLight = 0.0; 
	float attenuation = 1.0;
	vec3 pixToLight = -light.direction;
	
	// for POINT & SPOT lights
	if(light.type != DIRECTIONAL){ 
		pixToLight = light.position - position_cs;
		distPixToLight = length(pixToLight); 
		pixToLight = normalize(pixToLight); 
		attenuation	/= ( 	light.constant + light.linear * distPixToLight + light.quadratic * distPixToLight * distPixToLight);
	}
	// for SPOT lights
	if(light.type == SPOT){
		float spotDot = dot(pixToLight, -light.direction);
		float spotAttenuation;
		if(spotDot < light.cosCutoff) {
			spotAttenuation = 0.0;
		} else {
			spotAttenuation = pow(spotDot, light.exponent);
		}
		attenuation *= spotAttenuation;
	}
	// for ALL lights
	
	float norDotPixToLight = max(0.0, dot(normal_cs, pixToLight));
	if(norDotPixToLight != 0.0){
		diffLightSum += light.diffuse * norDotPixToLight * attenuation;
	}
}

void calcLighting(in uint sgLightNr, in SurfaceProperties surface, inout vec4 diffLightSum){
	sg_LightSourceParameters light = sg_LightSource[sgLightNr];
	light.position = (photons[photonID].viewMat * vec4(light.position, 1)).xyz;
	light.direction = (photons[photonID].viewMat * vec4(light.direction, 0)).xyz;
	
	addLighting(light,surface.position_cs, surface.normal_cs, surface.shininess, diffLightSum);

	diffLightSum = surface.diffuse * diffLightSum + surface.emission;
	
	diffLightSum.a = 1.0;
}

void sg_initSurfaceFromSGMaterial(inout SurfaceProperties surface){
	// Consider only diffuse part for now
	
	surface.ambient = vec4(0.0);
	surface.specular = vec4(0.0);
	surface.emission = vec4(0.0);
	surface.shininess = 0.0f;
	
	if(sg_useMaterials){	
		surface.diffuse = sg_Material.diffuse;
		surface.diffuse *= vec4(vertexColor, 0.0);
	} else {
		surface.diffuse = vec4(vertexColor, 0.0);
	}
	
	surface.position_cs = position_cs;
	surface.normal_cs = normal_cs;
	surface.tangent_cs = vec3(0.0);
}

void main(void){
	uint lightPatch = imageLoad(lightpatchTex, ex_PolygonID).x;
	uint lightID = 1;
	
	SurfaceProperties surface;
	sg_initSurfaceFromSGMaterial(surface);
	
	vec4 diffLightSum = vec4(0.0);
	
	Photon p = photons[photonID];
	if(length(p.normal_ws) > 0.01f){
		while(lightPatch != 0){
			uint lightIDBitMask = 1 << (lightID-1);
			uint lightIsUsed = lightPatch & lightIDBitMask;
			
			if(lightIsUsed != 0){
				calcLighting(lightID, surface, diffLightSum);
			}
			
			lightPatch &= ~(lightIDBitMask); 
			lightID++;
		}
	}
	
	//diffLightSum = vec4(normal_cs, 0.0); //Render only normal. Only for debug purposes!
	
	outValue = diffLightSum;
	outNormal = vec4(normal_cs, 0.0);
}