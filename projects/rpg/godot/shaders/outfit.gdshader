// The appearance of Kenney's Mini Characters. Their single texture is a palette of cells, 8 across
// and 4 down, each with its own light-to-dark gradient. Three changes, each keeping the gradient:
// - the hair cell (on the head) becomes the cell chosen for the hair colour;
// - the skin cell (on the head and the hands) becomes the cell chosen for the skin tone;
// - the coloured cells of the third row (green, yellow, orange, red, blue, light blue, purple) are
//   turned by `shift` steps, so clothes change colour together.
// A "from" cell of (-1, -1) changes nothing.
shader_type spatial;

uniform sampler2D albedo_texture : source_color, filter_nearest;
uniform int shift = 0;
uniform vec2 hair_from = vec2(-1.0);
uniform vec2 hair_to = vec2(-1.0);
uniform vec2 skin_from = vec2(-1.0);
uniform vec2 skin_to = vec2(-1.0);

void fragment() {
	vec2 cell = floor(UV * vec2(8.0, 4.0));
	vec2 inside = UV * vec2(8.0, 4.0) - cell;
	vec2 to = cell;
	if (hair_from.x >= 0.0 && cell == hair_from) {
		to = hair_to;
	} else if (skin_from.x >= 0.0 && cell == skin_from) {
		to = skin_to;
	} else if (shift != 0 && cell.y == 2.0 && cell.x >= 1.0) {
		to.x = 1.0 + mod(cell.x - 1.0 + float(shift), 7.0);
	}
	ALBEDO = texture(albedo_texture, (to + inside) / vec2(8.0, 4.0)).rgb;
	ROUGHNESS = 1.0;
}
