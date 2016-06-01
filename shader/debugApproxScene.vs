#version 430

in vec3 sg_Position;
in vec3 sg_Normal;
in vec3 sg_Color;
in uint sg_PolygonID;

uniform mat4 sg_matrix_cameraToWorld;
uniform mat4 sg_matrix_modelToClipping;
uniform mat4 sg_matrix_modelToCamera;

flat out int ex_PolygonID;
smooth out vec3 ex_normal;

void main(void){
  ex_normal    = normalize((sg_matrix_cameraToWorld * sg_matrix_modelToCamera * vec4(sg_Normal, 0.0)).xyz);
  ex_PolygonID = int(sg_PolygonID);
  gl_Position  = sg_matrix_modelToClipping * vec4(sg_Position, 1);
}