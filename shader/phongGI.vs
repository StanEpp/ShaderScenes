#version 130

struct VertexProperties {
	vec3 position_cs, normal_cs;
	float pointSize;
};

in vec4 sg_Color;
in vec3 sg_Normal;
in vec3 sg_Position;
in vec3 sg_Tangent;
in vec2 sg_TexCoord0;
in vec2 sg_TexCoord1;

uniform mat4 sg_shadowMatrix;
uniform mat4 sg_matrix_modelToClipping;
uniform mat4 sg_matrix_cameraToWorld;
uniform mat4 sg_matrix_modelToCamera;
uniform mat4 sg_matrix_worldToCamera;

uniform bool 	sg_textureEnabled[8];
uniform bool 	sg_specularMappingEnabled, sg_normalMappingEnabled;
uniform float	sg_pointSize;
uniform bool	sg_shadowEnabled;

out vec4 var_shadowCoord;
out vec3 var_normal_cs;
out vec4 var_position_hcs;
out vec4 var_vertexColor;
out vec2 var_texCoord0, var_texCoord1;
out vec3 var_tangent_cs, var_bitangent_cs;	// normal mapping


float sg_getPointSize()					{	return sg_pointSize;	}
vec4 sg_getVertexColor()				{	return sg_Color;		}
vec3 sg_getVertexPosition_ms()			{	return sg_Position;		}
vec3 sg_getVertexNormal_ms()			{	return sg_Normal;		}
vec3 sg_getVertexTangent_ms()			{	return sg_Tangent;		}

vec4 sg_cameraToWorld(in vec4 hcs)		{	return sg_matrix_cameraToWorld  * hcs;							}
vec4 sg_modelToClipping(in vec4 hms)	{	return sg_matrix_modelToClipping * hms; 						}
vec4 sg_modelToWorld(vec4 hms)			{	return sg_matrix_cameraToWorld * sg_matrix_modelToCamera * hms;	}
vec4 sg_modelToCamera(in vec4 hms)		{	return sg_matrix_modelToCamera * hms;							}
vec4 sg_worldToCamera(in vec4 hws)		{	return sg_matrix_worldToCamera * hws; 							}

void addVertexEffect(inout vec3 pos_ms, inout vec3 normal_ms, inout float pointSize){}

void provideSurfaceVars(in VertexProperties vec){
	var_vertexColor = sg_getVertexColor();
	if(sg_textureEnabled[0] || sg_specularMappingEnabled)
		var_texCoord0 = sg_TexCoord0;
	if(sg_textureEnabled[1])
		var_texCoord1 = sg_TexCoord1;
	if(sg_normalMappingEnabled){
		vec3 tangent_ms = sg_getVertexTangent_ms();
		var_tangent_cs = sg_modelToCamera( vec4(tangent_ms,0.0) ).xyz;
		var_bitangent_cs = sg_modelToCamera( vec4(cross(sg_getVertexNormal_ms(), tangent_ms),0.0) ).xyz; 
	}
}

void provideSurfaceEffectVars(in VertexProperties vec){}

void provideLightingVars(in VertexProperties vec){
	if(sg_shadowEnabled) {
//		var_shadowCoord = sg_shadowMatrix * sg_modelToWorld( vec4(sg_Position, 1.0) );
		var_shadowCoord = sg_shadowMatrix * sg_cameraToWorld( vec4(vec.position_cs,1.0) );
	}
}

void provideFragmentEffectVars(in VertexProperties vert){}

void main (void) {
    vec3 normal_ms = sg_getVertexNormal_ms();
    vec3 position_ms = sg_getVertexPosition_ms();
    float pointSize = sg_getPointSize();

    // optionally modify model space position, normal and point size
    addVertexEffect(position_ms, normal_ms,pointSize);

	VertexProperties vert;
	vert.position_cs = sg_modelToCamera(vec4(position_ms,1.0)).xyz;
	vert.normal_cs = sg_modelToCamera(vec4(normal_ms,0.0)).xyz; // \note the value is not normalized!

	provideSurfaceVars(vert);
	provideSurfaceEffectVars(vert);
	provideLightingVars(vert);
	provideFragmentEffectVars(vert);

	var_position_hcs = vec4(vert.position_cs,1.0);
	var_normal_cs = vert.normal_cs;
	gl_PointSize = pointSize;
	gl_Position = sg_modelToClipping(vec4(position_ms,1.0));
}

