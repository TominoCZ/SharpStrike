#version 330

in float _alpha;

void main(void)
{
	gl_Color = vec4(0, 0, 0, _alpha);
}
