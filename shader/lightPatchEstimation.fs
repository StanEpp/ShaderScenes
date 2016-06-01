#version 430

smooth in vec2 texCoord;

layout(binding = 0, r32ui) uniform uimageBuffer lightpatchTex;

// corresponding bit position is set to 1. Example: ID of light is 4 -> lightID = 000000000000000000000000001000 
uniform int lightID;

layout(location = 0) uniform usampler2D polygonIDTex;

void main(void){
	int polygonID = int(texture(polygonIDTex, texCoord).x);
	uvec4 bits = imageLoad(lightpatchTex, polygonID);
	bits.x = bits.x | uint(lightID);
	imageStore(lightpatchTex, polygonID, bits);
}