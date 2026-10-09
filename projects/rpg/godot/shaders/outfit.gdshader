// The outfit colour of Kenney's Mini Characters: their single texture is a palette of cells, 8 across
// and 4 down. The coloured cells of the third row (green, yellow, orange, red, blue, light blue,
// purple) are turned by `shift` steps, so clothes change colour together while skin, hair, greys and
// eyes stay as they are.
shader_type spatial;

uniform sampler2D albedo_texture : source_color, filter_nearest;
uniform int shift = 0;

void fragment() {
	vec2 uv = UV;
	if (shift != 0 && uv.y >= 0.5 && uv.y < 0.75 && uv.x >= 0.125) {
		float column = floor(uv.x * 8.0);
		float inside = uv.x * 8.0 - column;
		column = 1.0 + mod(column - 1.0 + float(shift), 7.0);
		uv.x = (column + inside) / 8.0;
	}
	ALBEDO = texture(albedo_texture, uv).rgb;
	ROUGHNESS = 1.0;
}
