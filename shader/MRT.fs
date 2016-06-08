#version 430

in vec4 position;
in vec3 normal;

out vec4 fragData[2];

void main(void) {
	// Store the depth value into the last component of the position.
	fragData[0] = vec4(position.xyz / position.w, gl_FragCoord.z);
	fragData[1] = vec4(normal, 1.f);
}
