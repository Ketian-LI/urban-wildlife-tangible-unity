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

## Movable planning-area sprites V0.1

Generated on 2026-09-10 with the built-in image generation tool. These presentation assets replace the abstract Food and Woodland icons without changing the recognised token types or IDs:

- `woodland-forest-grove-v01.png`: Woodland IDs 20 and 21.
- `human-activity-bench-v01.png`: even Food Hotspot IDs 10 and 12.
- `human-activity-plaza-v01.png`: odd Food Hotspot ID 11.

The simulation still records `woodland` and `food_hotspot`. “Food Hotspot” now means a human-activity setting where discarded crumbs or feeding opportunities may occur, not a literal movable food object.

Generation mode: built-in `stylized-concept`, followed by a built-in `precise-object-edit` isolation pass. The uniform green plates were converted to RGBA with `tools/chroma_key_sprite.py`.

### Forest grove generation prompt

```text
Use case: stylized-concept. Asset type: transparent Unity top-down environment sprite. The supplied park board is a STYLE REFERENCE ONLY. Create one compact movable urban-park woodland/forest grove viewed in strict orthographic top-down view: a clearly readable cluster of five to seven mature deciduous tree canopies with varied olive, moss, and deep forest greens, visible small brown trunks through a few canopy gaps, low shrubs, ferns, leaf litter, and two or three small natural stones. Form an irregular oval forest patch, wider than tall, with a strong natural silhouette and enough internal gaps to read as a grove rather than one giant bush. Match the gentle hand-painted semi-realistic British park game illustration, soft daylight, refined painterly texture and scale language of the reference. Isolate the complete grove on a genuinely transparent RGBA background with generous padding and clean antialiased edges. No people, animals, paths, benches, buildings, food, tokens, labels, numbers, text, border, shadow plate, glow, watermark, or checkerboard.
```

### Plaza generation prompt

```text
Use case: stylized-concept. Asset type: transparent Unity top-down environment sprite. The supplied park board is a STYLE REFERENCE ONLY. Create one small human-activity plaza viewed in strict orthographic top-down view: a compact circular-to-oval paved area made from warm pale sandstone setts, with a subtle concentric paving pattern, slightly irregular stone edges, and a very thin fringe of grass and a few tiny low flowers around part of the perimeter. It must clearly read as a public park plaza and a potential human gathering/crumb hotspot, but show no literal food. Match the gentle hand-painted semi-realistic British park game illustration, soft daylight, refined painterly texture and scale language of the reference. Isolate the complete plaza on a genuinely transparent RGBA background with generous padding and clean antialiased edges. No people, animals, furniture, paths extending off canvas, food icons, tokens, labels, numbers, text, border, cast shadow, glow, watermark, or checkerboard.
```

### Bench generation prompt

```text
Use case: stylized-concept. Asset type: transparent Unity top-down environment sprite. The supplied park board is a STYLE REFERENCE ONLY. Create one compact park bench rest area viewed in strict orthographic top-down view: a single realistic wooden slat bench with dark iron supports, placed on a small rounded rectangle of warm compacted gravel or pale paving, with a narrow soft grass fringe and two or three tiny low wildflowers at the edge. The bench must be instantly readable from above and suggest a human resting/crumb hotspot without showing literal food. Match the gentle hand-painted semi-realistic British park game illustration, soft daylight, refined painterly texture and scale language of the reference. Isolate the complete bench area on a genuinely transparent RGBA background with generous padding and clean antialiased edges. No people, animals, tables, bins, signs, food icons, tokens, labels, numbers, text, border, cast shadow beyond the object, glow, watermark, or checkerboard.
```

### Forest grove isolation prompt

```text
Use case: precise-object-edit. Asset type: Unity environment sprite isolation plate. Preserve the forest grove itself exactly: same top-down composition, tree count, canopy shapes, trunks, shrubs, ferns, stones, leaf litter, colors, painterly style, scale, centering, and complete silhouette. Change ONLY the entire background and glow outside the grove to one perfectly flat uniform chroma-key green RGB (0,255,0), hex #00FF00. The green must stop at the grove's clean antialiased silhouette. No dark vignette, gradient, cast shadow plate, aura, glow, checkerboard, texture, ground outside the grove, text, label, border, or watermark.
```

### Plaza isolation prompt

```text
Use case: precise-object-edit. Asset type: Unity environment sprite isolation plate. Preserve the paved plaza itself exactly: same strict top-down oval sandstone paving, concentric pattern, stone edges, grass fringe, flowers, colors, painterly style, scale, centering, and complete silhouette. Change ONLY the entire background and glow outside the plaza to one perfectly flat uniform chroma-key green RGB (0,255,0), hex #00FF00. The green must stop at the plaza's clean antialiased silhouette. No dark vignette, gradient, cast shadow plate, aura, glow, checkerboard, texture outside the plaza, people, animals, furniture, food, text, label, border, or watermark.
```

### Bench isolation prompt

```text
Use case: precise-object-edit. Asset type: Unity environment sprite isolation plate. Preserve the bench rest area itself exactly: same strict top-down wooden slat bench, dark iron supports, rounded gravel pad, grass fringe, flowers, colors, painterly style, scale, centering, and complete silhouette. Change ONLY the entire background and glow outside the rest area to one perfectly flat uniform chroma-key green RGB (0,255,0), hex #00FF00. The green must stop at the rest area's clean antialiased silhouette. No dark vignette, gradient, cast shadow plate beyond the rest area, aura, glow, checkerboard, texture outside the area, people, animals, bins, signs, food, text, label, border, or watermark.
```
