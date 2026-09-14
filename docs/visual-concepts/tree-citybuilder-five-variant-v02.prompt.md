# Five-tree dynamic woodland kit V02

## Purpose

This set turns the approved `city-board-natural-broadleaf-midforest-v06` direction into removable Unity sprites. Woodland is assembled at runtime, so replacing a woodland cell with a building also removes its trees.

## Shared generation prompt

> Create exactly one medium deciduous urban-park tree for a reusable five-tree Unity kit. Use a consistent high-angle orthographic city-builder view. Show the full tree from canopy to the base of a slim warm-brown trunk. Use one coherent, gently asymmetrical canopy mass with only shallow contour changes, never separate balloon spheres. Match the approved muted forest-teal, sage-green and restrained olive palette; matte low-relief 2.5D rendering; soft upper-left light; no hard outline or photorealism. Keep the same visual height, scale, centering and generous padding across the set. Output a square PNG with genuine transparent RGBA. Exactly one tree; no ground plate, grass, second tree, fruit, flowers, detached leaves, text, frame, checkerboard or watermark.

## Five controlled silhouettes

1. `pear`: broad pear-shaped crown, widest slightly below centre.
2. `round`: soft rounded crown with one subtle asymmetric shoulder.
3. `three-lobe`: one large central mass and two shallow integrated side shoulders.
4. `two-lobe`: one dominant mass and one smaller integrated side lobe.
5. `tapered`: gently tapered oval crown, still broad enough to avoid a column or cypress shape.

## Transparency cleanup

Some generated variants returned a visual checkerboard instead of an alpha channel. Those foregrounds were isolated on a uniform magenta plate and converted to RGBA with `tools/chroma_key_sprite.py --key-color magenta`. This avoids removing the green and yellow-green foliage highlights.

## Unity placement contract

- Seven trees per woodland cell.
- All five silhouettes appear in every woodland cell.
- Stable deterministic variation between cells and runs.
- Small controlled scale variation only.
- Visible gaps between crowns; no dense wall of foliage.
- Individual tree objects are rebuilt from the current land-cover state.
