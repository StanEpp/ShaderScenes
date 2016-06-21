#version 430

struct Photon{
	mat4 viewMat;
	vec4 diffuse;
	vec4 position_ws;
	vec4 position_ss;
	vec4 normal_ws;
};

flat in vec2 texCoord;
flat in uint photonID;

layout (std430, binding = 2) buffer PhotonBuffer {
	Photon photons[];
};

layout(binding = 0) uniform sampler2D posTexture;
layout(binding = 1) uniform sampler2D normalTexture;

out vec4 color;

void main(void) {
	vec3 Dir = texture(normalTexture, texCoord).xyz;
	//vec3 Dir = texture(normalTexture, vec2(0.5f, 0.5f)).xyz;
	photons[photonID].normal_ws = vec4(Dir, 0.f);
	if(length(Dir) < 0.01f){
		return;
	}
	Dir = normalize(Dir);
	
	//---------------------- Taken from: MinSG::Transformations::rotateToWorldDir() -----------------------
	vec3 relRight = vec3(0);
	vec3 relDir = -1.f * Dir;
	if(abs(relDir.y) < 0.99f ){
		relRight = cross(relDir, vec3(0, 1, 0));
	} else {
		relRight = cross(relDir, vec3(1, 0, 0));
	}
	
	vec3 Up = cross(relDir, -1.f * relRight);
	// -----------------------------------------------------------------------------------------------------
	
	
	vec3 Pos = texture(posTexture, texCoord).xyz;
	//vec3 Pos = texture(posTexture, vec2(0.5f, 0.5f)).xyz;
	
	// 
	// ---------------------- Taken from: Geometry::_Matrix3x3::setRotation() ---------------------------
	vec3 bZ = -1.f * Dir;
	vec3 bX = normalize(cross(Up, bZ));
	vec3 bY = cross(bZ, bX);
	
	mat4x4 mat = mat4x4(1.f);
	
	mat[0] = vec4(bX, 0);
	mat[1] = vec4(bY, 0);
	mat[2] = vec4(bZ, 0);
	mat[3] = vec4(Pos, 1);
	// -----------------------------------------------------------------------------------------------------

	
	photons[photonID].viewMat = inverse(mat);
	photons[photonID].position_ws = vec4(Pos, 1.f);
	photons[photonID].normal_ws = vec4(Dir, 0.f);
	
	//vec2 screenPos = texCoord * 2.f - vec2(1.f, 1.f);
	photons[photonID].position_ss = vec4(texCoord, 3.14f, 0.f);
	
	//color = vec4(1); //Only for debug purposes. It paints the pixel white which corresponds to this samplePoint on the screen texture.
	
}
