#version 430

in vec3 sg_Position;
in vec3 sg_Normal;
in vec3 sg_Color;
in uint sg_PolygonID;

uniform mat4 sg_matrix_cameraToWorld;
uniform mat4 sg_matrix_modelToCamera;
uniform mat4 sg_matrix_modelToClipping;

smooth out vec3 normal;
smooth out vec3 position;
smooth out vec3 color; 

flat out int ex_PolygonID;

void main(void){
  ex_PolygonID = int(sg_PolygonID);
  position = (sg_matrix_cameraToWorld * sg_matrix_modelToCamera * vec4(sg_Position, 1.0)).xyz;
  normal = normalize((sg_matrix_cameraToWorld * sg_matrix_modelToCamera * vec4(sg_Normal, 0.0)).xyz);
  color = sg_Color;
  gl_Position = sg_matrix_modelToClipping * vec4(sg_Position, 1.0);
}