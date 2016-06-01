#version 430

uint Log2(uint x){ if(x == 0) { return 0; } else { return uint(log2(x)) + 1; } }

flat in int ex_PolygonID;
smooth in  vec3  ex_normal;

layout(binding = 0, r32ui) restrict uniform uimageBuffer lightpatchTex;

out vec4 color;

vec4 solidColoring(vec4 solidColor = vec4(0.25), float brigthness = 0.25f){
  vec3 lightDir = vec3(1.0, 0.0, 0.0);
  vec3 lightDir2 = vec3(0.0, 1.0, 0.0);
  vec3 lightDir3 = vec3(0.0, 0.0, 1.0);
  float diffuseInt1 = abs(dot(normalize(ex_normal), -lightDir)); 
  float diffuseInt2 = abs(dot(normalize(ex_normal), lightDir)); 
  float diffuseInt3 = abs(dot(normalize(ex_normal), -lightDir2)); 
  float diffuseInt4 = abs(dot(normalize(ex_normal), lightDir2)); 
  float diffuseInt5 = abs(dot(normalize(ex_normal), -lightDir3)); 
  float diffuseInt6 = abs(dot(normalize(ex_normal), lightDir3)); 
  vec3 brightnessV = vec3(brigthness);
  return 
    solidColor * vec4( brightnessV * diffuseInt1, 1.f) + solidColor * vec4( brightnessV * diffuseInt2, 1.f)
  + solidColor * vec4( brightnessV * diffuseInt3, 1.f) + solidColor * vec4( brightnessV * diffuseInt4, 1.f)
  + solidColor * vec4( brightnessV * diffuseInt5, 1.f) + solidColor * vec4( brightnessV * diffuseInt6, 1.f);
}

vec4 lightIDToColor(uint ID){
   const vec4 array[4] = vec4[4](
   vec4(0, 0, 0, 0),
   vec4(1, 0, 0, 0),
   vec4(0, 0.9, 0, 0),
   vec4(0, 0, 0.8, 0)
   );

  vec4 color = vec4(0);
  if(ID == 0) return vec4(0.75);
  color += array[Log2((1 << 0) & ID)];
  color += array[Log2((1 << 1) & ID)];
  color += array[Log2((1 << 2) & ID)];

  return color;
}

void main(void){
  uvec4 lightPatch = imageLoad(lightpatchTex, ex_PolygonID);

  //if(ex_PolygonID >= 24000 && ex_PolygonID < 25000){ color = vec4(1.0,0,0,0);} else {
  vec4 solidColor = lightIDToColor(lightPatch.x);
  color = solidColoring(solidColor);
  //}
}