#version 430

struct CompositeColor {
	vec4 ambient, diffuse, specular;
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

struct CompositeColor {	vec4 ambient, diffuse, specular;	};
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
	mat4 viewMat;
	vec4 diffuse;
	vec4 position_ws;
	vec4 normal_ws;
};

uniform bool sg_textureEnabled[8];
uniform bool sg_specularMappingEnabled, sg_normalMappingEnabled;
uniform sg_MaterialParameters			sg_Material;
uniform bool							sg_useMaterials;
uniform sg_LightSourceParameters		sg_LightSource[8];
uniform int								sg_lightCount;
uniform int 							sg_shadowTextureSize;
uniform sampler2D	sg_texture0;
uniform sampler2D	sg_texture1;
uniform sampler2D	sg_specularMap;
uniform sampler2D	sg_normalMap;
uniform sampler2D 	sg_shadowTexture;

layout(binding = 0) uniform isampler2D	samplingTexture;

layout (std430, binding = 1) buffer PhotonBuffer {
	Photon photons[];
};

uniform sampler2D lastColorBuffer;
uniform sampler2D lastDepthBuffer;
uniform mat4 sg_matrix_worldToCamera;
uniform mat4 sg_matrix_cameraToWorld;
uniform mat4 sg_matrix_cameraToClipping;  // eye->cam move to sg_helper!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
uniform mat4 sg_lastProjectionMatrix;  
uniform mat4 sg_lastProjectionMatrixInverse;  
uniform mat4 sg_eyeToLastEye;  

uniform vec2 last_viewportScale = vec2(1,1);
uniform vec2 last_viewportOffset = vec2(0,0);

uniform float sg_reflectionStrength = 0.0;

uniform mat4 invLastCamMatrix;  //!!!!!!!!!!!!!!!!!!!!!! world to eye
uniform bool sg_shadowEnabled;

in vec3 var_normal_cs;
in vec4 var_position_hcs;
in vec4	var_shadowCoord;
in vec4 var_vertexColor;
in vec2 var_texCoord0, var_texCoord1;
in vec3 var_tangent_cs, var_bitangent_cs;	// normal mapping

uniform vec2 _shadowSamplingPoints[16] = vec2[16](
	vec2(-0.573297,0.39484),
	vec2(-0.00673674,0.810868),
	vec2(-0.545758,-0.298327),
	vec2(-0.420092,-0.284146),
	vec2(-0.0740884,-0.321956),
	vec2(0.528959,-0.640733),
	vec2(-0.241788,0.662894),
	vec2(-0.167344,0.155723),
	vec2(0.555928,-0.820999),
	vec2(-0.781556,-0.506979),
	vec2(-0.434296,0.0980303),
	vec2(-0.403425,0.265021),
	vec2(-0.721056,-0.106324),
	vec2(-0.366311,-0.174337),
	vec2(0.541415,0.630838),
	vec2(0.0607513,0.528244)
);

uniform ivec2 _photonSamplingPos[9] = ivec2[9](
	ivec2(-1, 1),
	ivec2(0, 1),
	ivec2(1, 1),
	ivec2(-1, 0),
	ivec2(0, 0),
	ivec2(1, 0),
	ivec2(-1, -1),
	ivec2(0, -1),
	ivec2(1, -1)
);


int sg_getLightCount()					{	return sg_lightCount;						}
vec4 sg_cameraToWorld(in vec4 hcs)		{	return sg_matrix_cameraToWorld  * hcs;		}
vec4 sg_cameraToClipping(in vec4 hcs)	{	return sg_matrix_cameraToClipping * hcs; 	}
bool sg_isMaterialEnabled()				{	return sg_useMaterials;						}

void addLighting(in sg_LightSourceParameters light, in vec3 position_cs, in vec3 normal_cs, in float shininess, inout CompositeColor result){
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
	result.ambient += light.ambient * attenuation;
	
	float norDotPixToLight = max(0.0, dot(normal_cs, pixToLight));
	if(norDotPixToLight != 0.0){
		result.diffuse += light.diffuse * norDotPixToLight * attenuation;

		if(shininess>0.0){
			vec3 pixToEye = normalize(-position_cs);
			vec3 refl = reflect(-pixToLight, normal_cs);
			float eyeDotRefl = dot(pixToEye, refl);
			if(eyeDotRefl>0.0)
				result.specular += light.specular * pow(eyeDotRefl, shininess/4.0) * attenuation;
		}
	}
}
void sg_addLight(in int sgLightNr,in vec3 position_cs, in vec3 normal_cs, in float shininess, inout CompositeColor lightSum){
	vec3 n_cs = normal_cs;
	#ifdef SG_FS
	if(! gl_FrontFacing) 
		n_cs = -n_cs;
	#endif
	addLighting(sg_LightSource[sgLightNr],position_cs,n_cs,shininess,lightSum);
}
void initSurfaceColor_AmDiSp(inout SurfaceProperties surface,in vec4 c){
	surface.ambient = c;
	surface.diffuse = c;
	surface.specular = c;
}

void multSurfaceColor_AmDiSp(inout SurfaceProperties surface,in vec4 c){
	surface.ambient *= c;
	surface.diffuse *= c;
	surface.specular *= c;
}

void sg_initSurfaceFromSGMaterial(inout SurfaceProperties surface){
	surface.ambient = sg_Material.ambient;
	surface.diffuse = sg_Material.diffuse;
	surface.specular = sg_Material.specular;
	surface.emission = vec4(0.0);
	surface.shininess = sg_Material.shininess;
}

void calcSurfaceProperties(inout SurfaceProperties surface){

	// material
	if(sg_isMaterialEnabled()){
		sg_initSurfaceFromSGMaterial(surface);
	}else{
		initSurfaceColor_AmDiSp(surface,var_vertexColor);
		surface.emission = vec4(0.0);
		surface.shininess = 0.0;
	}
	// texture
	if(sg_textureEnabled[0])
		multSurfaceColor_AmDiSp(surface,texture2D(sg_texture0, var_texCoord0));
	if(sg_textureEnabled[1])
		multSurfaceColor_AmDiSp(surface,texture2D(sg_texture1, var_texCoord1));

	if(sg_specularMappingEnabled)
		surface.specular *= texture2D(sg_specularMap, var_texCoord0);
		
	if(sg_normalMappingEnabled){
		vec3 esTangent = normalize(var_tangent_cs);
		vec3 esBitangent = normalize(var_bitangent_cs);

		// Calculate eye->tangent space matrix
		mat3 tbnMat = mat3( esTangent, esBitangent, surface.normal_cs );
		vec3 tsNormal = texture2D(sg_normalMap,var_texCoord0).xyz - vec3(0.5,0.5,0.5);
		
		surface.normal_cs = normalize(tbnMat * tsNormal) ;
	}
}


vec3 hvec4ToVec3(in vec4 hvec){	return hvec.xyz/hvec.w;	}
vec3 lastEyePosToScreenPos(in vec3 pos_cs){
	return (hvec4ToVec3(sg_lastProjectionMatrix*vec4(pos_cs,1.0)) + vec3(1.0))*0.5;
}
vec3 screenPosToLastEyePos(in vec3 pos_screen){
	return hvec4ToVec3(sg_lastProjectionMatrixInverse*vec4(pos_screen*2.0-vec3(1.0),1.0));
}
float lastDepthBufferLookup(in vec2 screenPos){
	return texture2D(lastDepthBuffer, screenPos*last_viewportScale + last_viewportOffset).r;
}
vec4 lastColorBufferLookup(in vec2 screenPos){
	return texture2D(lastColorBuffer, screenPos*last_viewportScale + last_viewportOffset);
}
void addSurfaceEffects(inout SurfaceProperties surface){
	if( sg_reflectionStrength <= 0.0 )
		return;

	// project position into last frame's eye space
	vec3 pos_cs = hvec4ToVec3(invLastCamMatrix * sg_cameraToWorld(vec4(surface.position_cs,1.0) ));

	// project ray into last frame's eye space
	vec3 currentRay_cs = reflect( normalize(surface.position_cs) , surface.normal_cs);
	vec3 ray_cs = normalize( (invLastCamMatrix * sg_cameraToWorld(vec4(currentRay_cs,0.0) )).xyz);

	float f1 = 0.01;
	float f2 = -1.0;
	float f = 0.04;

	float closestDiff = -1;
	float closestF = -1;
	
	{	// search intersection range f1...f2
		float stepSize = 0.10;
		f = stepSize;
		
		for(int i=0;i<25;++i){ //*0.5
			vec3 sample_screen = lastEyePosToScreenPos(pos_cs + ray_cs*f);
			if(sample_screen.y<0.0||sample_screen.y>1.0||sample_screen.x<0.0||sample_screen.x>1.0){
				return;
			}
			
			float actualDepth_screen = lastDepthBufferLookup(sample_screen.xy);
			
			float diff = sample_screen.z-actualDepth_screen;
			if(diff>0.0002){
				f2 = f;
				break;
			}else{
				if(diff>closestDiff){
					closestDiff = diff;
					closestF = f;
				}

				f1 = f*0.7;
				f += stepSize;
				stepSize *= 1.25;
			}
		}
	}
	if(f2<0.0 && closestF>0.1 && true){ // not found, but chance to just jumped over
		f1 = closestF*0.9;
		float f2b = closestF*1.1;
		float stepSize = (f2b-f1)*0.1;
		for(f=f1;f<f2b;f+=stepSize){ 
			vec3 sample_screen = lastEyePosToScreenPos(pos_cs + ray_cs*f);		
			float actualDepth_screen = lastDepthBufferLookup( sample_screen.xy );		
			if(sample_screen.z-actualDepth_screen>0.0001){
				f2 = f*1.01;
				break;
			}
		}
	}
	if(f2<0.0){
		return;
	}
	{	// intersection found -> search best point
		vec3 best = vec3(0);
		float quality = 0.0;
		float error = 100.0;
				
		f = (f1+f2) * 0.5;
		
		float actualDepth_screen;
		for(int i=0;i<10;++i){
			vec3 sample_screen = lastEyePosToScreenPos(pos_cs + ray_cs*f);
			actualDepth_screen = lastDepthBufferLookup(sample_screen.xy);

			if(actualDepth_screen<sample_screen.z){
				f2 = f;
				f = (f1+f2) * 0.5;
			}else{
				f1 = f;
				f = (f1+f2) * 0.5;
			}
			float newError = abs(actualDepth_screen-sample_screen.z);
			if( newError<error){
				best = sample_screen;
				error = newError;
			}
			if(f2-f1<0.00001){
				break;
			}
		}
		vec3 intersection_cs = vec3(lastEyePosToScreenPos(pos_cs + ray_cs*f).xy,actualDepth_screen);
		vec3 intersectionRay_cs = screenPosToLastEyePos(intersection_cs)-pos_cs;
		quality = max(dot(normalize(intersectionRay_cs),ray_cs),0);
		if(length(intersectionRay_cs)<0.2){ // allow lower quality rays near tight corners
			quality = pow(quality,0.2-length(intersectionRay_cs));
		}
		float amount = min(sg_reflectionStrength *  pow(quality,200.0) *  smoothstep(length(pos_cs)*0.75,0.1,f),1.0);
		float borderDist = min(abs(mod(best.x+0.5,1.0)-0.5),abs(mod(best.y+0.5,1.0)-0.5));
		if(borderDist<0.15){
			amount /= pow(1.15-borderDist,50);
		}
		vec4 reflectedColor = lastColorBufferLookup( best.xy );
		surface.emission = mix(surface.emission,reflectedColor * surface.specular,amount);
	}
}

float getSingleShadowSample(in sampler2D shadowTexture, in vec3 coord, in vec2 offset) {
	float depth = texture2D(shadowTexture, coord.xy + (offset / sg_shadowTextureSize)).r;
	return (depth < coord.z) ? 0.0 : 1.0; 
}
float smooth2(in float edge0,in float edge1,in float x){
	float t = clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
	return t * t * (3.0 - 2.0 * t);
}
float getShadow() {
	if(!sg_shadowEnabled) 
		return 1.0;
	vec3 shadowPersp = var_shadowCoord.xyz / var_shadowCoord.w;
	float sum = 0.0;
	
	sum += getSingleShadowSample(sg_shadowTexture, shadowPersp, vec2(0.0,0.0));
	if(sum==1.0) // sample is lit
		return 1.0;
	
	sum += getSingleShadowSample(sg_shadowTexture, shadowPersp, vec2(0.0,4.0));
	sum += getSingleShadowSample(sg_shadowTexture, shadowPersp, vec2(0.0,-4.0));
	sum += getSingleShadowSample(sg_shadowTexture, shadowPersp, vec2(4.0,0.0));
	sum += getSingleShadowSample(sg_shadowTexture, shadowPersp, vec2(-4.0,0.0));
	
	if(sum<0.01){ // fully inside shadow
		return 0.0;
	}
	// shadow border -> do some sampling to reduce aliasing
//		color.ambient.g = sum/4.0; // debug, show border
	for(int i=0;i<16;++i)
		sum += getSingleShadowSample(sg_shadowTexture, shadowPersp, _shadowSamplingPoints[i]*1.5);

	// adjust the gradient
	sum = smooth2(0.0,11.0,sum);
	return sum;
}

void calcIndirectLighting(in SurfaceProperties surface, inout CompositeColor lightSum){
	sg_LightSourceParameters indirectPointLight;
	
	// Treat photons as point light sources
	indirectPointLight.type = POINT;
	indirectPointLight.diffuse = vec4(0);
	indirectPointLight.ambient = vec4(0);
	indirectPointLight.specular = vec4(0);
	indirectPointLight.constant = 1.f;
	indirectPointLight.linear = 0.f;
	indirectPointLight.quadratic = 0.f;
	
	ivec2 size = ivec2(12, 12);//TODO: Add size as uniform
	vec2 scPos = gl_FragCoord.xy - vec2(0.5);
	scPos.x /= 1280.f; scPos.y /= 720.f;
	
	ivec2 sPos = ivec2(int(float(size.x) * scPos.x), int(float(size.y) * scPos.y));
	
	for(int i = 0; i < 9; i++ ){
		int ID = texelFetch(samplingTexture, sPos + 2 * _photonSamplingPos[i], 0).x;
		//int ID = texelFetch(samplingTexture, ivec2(10,10) + _photonSamplingPos[i], 0).x;
		if(ID < 0) continue;
		Photon p = photons[i];
		if(p.diffuse.a < 0.1f) continue;
		indirectPointLight.position = (sg_matrix_worldToCamera * p.position_ws).xyz;
		indirectPointLight.direction = (sg_matrix_worldToCamera * p.normal_ws).xyz;
		indirectPointLight.diffuse.rgb = p.diffuse.rgb / p.diffuse.a;
		addLighting(indirectPointLight, surface.position_cs, surface.normal_cs, 0, lightSum);
	}
	
}

void calcLighting(in SurfaceProperties surface, out CompositeColor color){
	CompositeColor lightSum;
	lightSum.ambient = vec4(0.0);
	lightSum.diffuse = vec4(0.0);
	lightSum.specular = vec4(0.0);

	int lightCount = sg_getLightCount();

	if(lightCount==0){ // default lighting
		lightSum.ambient = vec4(0.3);
		lightSum.diffuse = vec4(0.7);
	}else{
		sg_addLight(0,surface.position_cs, surface.normal_cs, surface.shininess, lightSum);
		float s = getShadow();
		lightSum.diffuse *= s;
		lightSum.specular *= s;
	}
	for(int i = 1; i < 8; i++){
		if( i >= lightCount )
			break;
		sg_addLight(i,surface.position_cs, surface.normal_cs, surface.shininess, lightSum);
	}
	
	// Compute indirect lighting from photons
	calcIndirectLighting(surface, lightSum);

	lightSum.ambient.a = lightSum.diffuse.a = lightSum.specular.a = 1.0;

	color.ambient = surface.ambient * lightSum.ambient;
	color.diffuse = surface.diffuse * lightSum.diffuse + surface.emission;
	color.specular = surface.specular * lightSum.specular;
	}
		
void addFragmentEffect(in SurfaceProperties surface, inout CompositeColor color){}

out vec4 outColor;

void main (void) {
	SurfaceProperties surface;
	surface.position_cs = var_position_hcs.xyz / var_position_hcs.w;
	surface.normal_cs = normalize(var_normal_cs);
	
	calcSurfaceProperties(surface);				// get surface properties (material, textures, ...)
	addSurfaceEffects(surface);					// optionally add a surface effect (e.g. add snow)
	
	//surface.ambient = vec4(0);
	//surface.diffuse += vec4(indirectLight.xyz, 0);
	
	CompositeColor color;
	calcLighting(surface,color);				// add lighting and calculate color

	addFragmentEffect(surface,color);			// add effects (e.g. fog)
	outColor = color.ambient+color.diffuse+color.specular;	// combine color components into one color.
}
