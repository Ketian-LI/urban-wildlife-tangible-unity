# S001 Illustrated Environment V0.2

Generated on 2026-09-09 with the built-in image generation tool and integrated as a Unity Sprite.

Asset:

- `park-board-s001-v02.png`

The image is a visual background only. Unity still draws exact coordinate guides for Entrance A, Exit B, the 20 × 14 cm plaza, and the 18 × 12 cm pond. Simulation and constraint logic continue to use `data/scenarios/s001_weekend_park_baseline.json`, not pixels sampled from this artwork.

The final version intentionally contains no people, animals, movable tokens, route, text, benches, lamps, or other P1 planning props.

## Generation mode

Built-in image generation tool. One initial generation followed by two targeted edits; the third output is the committed asset.

## Exact prompt chain

### Initial generation

```text
Use case: stylized-concept
Asset type: Unity top-down game environment background
Input images: the three supplied animal sprites are style references only
Primary request: a cohesive hand-painted overhead background for an urban wildlife planning game set in a calm British city park
Scene/backdrop: a rectangular 3:2 park board with open green lawns and subtle painted grass texture; clusters of leafy trees, bushes, and small flowerbeds concentrated around the outer edges and corners; keep the central play area visually open
Fixed spatial landmarks: a warm pale-stone oval plaza centered at 50% width and 30% height; one irregular horizontal blue-green pond centered at 56% width and 70% height; visually clear open entry areas at the middle of the left and right edges
Style/medium: gentle hand-painted storybook game illustration, refined 2D painterly texture, natural but simplified, matching the calm visual language of the reference animal sprites
Composition/framing: strict orthographic directly overhead view, entire rectangular map visible, exact 3:2 landscape composition, no perspective tilt, no border, no cropped edges, clear negative space for moving characters and planning pieces
Lighting/mood: soft neutral daylight, welcoming and contemplative, restrained contrast so characters remain readable
Color palette: layered sage, moss, and meadow greens; warm cream stone; muted blue-green water; small restrained seasonal flower accents
Constraints: background only; no people, no animals, no tokens, no movable woodland pieces, no food icons, no text, no letters, no labels, no UI, no grid, no colored route, no magenta, no watermark; do not add extra ponds or plazas
```

### Edit 1 — remove premature P1 props and correct pond scale

```text
Use case: precise-object-edit
Asset type: Unity top-down game environment background
Input images: Image 1 is the map edit target
Primary request: refine the same park map for accurate game use
Required edits: reduce the single pond so its outer bounding shape is approximately 20% of the full canvas width and 20% of the full canvas height, while keeping its center at approximately 56% canvas width and 70% canvas height; naturally repaint the freed area as matching open lawn. Remove every bench, lamppost, freestanding stone post, statue, gate fixture, and other human-made prop around the plaza and map edges.
Invariants: keep the exact 3:2 canvas, strict orthographic overhead viewpoint, tree and flower border, open central lawn, painterly texture, warm oval plaza at approximately 50% width and 30% height, left and right mid-edge openings, palette, lighting, and overall hand-painted storybook style unchanged.
Constraints: no people, no animals, no tokens, no food symbols, no movable woodland pieces, no text, no labels, no UI, no route, no magenta, no extra pond, no extra plaza, no watermark.
```

### Edit 2 — align the plaza

```text
Use case: precise-object-edit
Asset type: Unity top-down game environment background
Input images: Image 1 is the map edit target
Primary request: move only the warm oval stone plaza downward so its center is at exactly approximately 50% of the canvas width and 30% of the canvas height. Keep the plaza's current size, oval shape, paving pattern, flowers, and painterly finish. Naturally repaint its former location as matching lawn and border foliage where needed.
Invariants: keep the pond exactly unchanged at approximately 56% width and 70% height; keep the 3:2 canvas, strict orthographic overhead view, all trees, flowers, edge openings, open lawn, palette, lighting, and style unchanged.
Constraints: no benches, no lampposts, no stone posts, no people, no animals, no tokens, no text, no labels, no route, no magenta, no extra pond, no extra plaza, no watermark.
```

This generated image is a prototype asset. Before final submission, record the selected provenance statement and confirm the course policy for AI-assisted visual material.
