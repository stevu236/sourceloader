#version 450 core
out vec4 color;

in vec4 _position;
in vec4 _normal;
in vec2 _texCoord;

uniform sampler2D tex;

void main() {
	vec4 lightDir = vec4(0.1,-1,0,1);

	//float light = dot(_normal, lightDir);
	float light = dot(lightDir, _normal);
	vec4 texColor = texture(tex, _texCoord);
	//color = vec4(texColor.x * light, texColor.y * light, texColor.z * light, 1.0);
	color = texture(tex, _texCoord);

	//color = vec4(0.5, 1.0, 0.0, 1.0);
	//color = _position;
	//color = vec4(0.5, 1.0, _position.z / 10, 1.0);
	//gl_fragColor = vec4(0.5, 1.0, 0.0, 1.0);
}
