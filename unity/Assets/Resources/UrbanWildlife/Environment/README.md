# S001 Illustrated Environment

## Soft city-builder tree V0.1

`tree-citybuilder-default-v01.png` is the first approved modular tree asset for the
GDD V3 city map. It follows the selected soft isometric planning-game direction:
rounded three-lobed foliage, a compact readable silhouette, gentle yellow-green to
teal shading, and a simple warm-brown trunk. `CityPrototypeDemo` renders the same
sprite at several scales and mirrors selected instances to reduce repetition.

The image is presentation-only. Tree placement and movement obstacles continue to
come from simulation data rather than the sprite pixels. The approved style board is
archived at `docs/visual-concepts/tree-style-board-approved-v01.png`.

Generation prompt summary: extract one clean Default tree from the approved style
board as a transparent Unity sprite; exclude UI, labels, variants, people, animals,
buildings, and backgrounds.

## V0.3 urban-neighbourhood park

Generated on 2026-09-10 with the built-in image generation tool and integrated as `park-board-s001-v03.png`. This version establishes an unmistakable London neighbourhood setting through red-brick housing, perimeter pavement and kerbs, black municipal railings and lamps, maintained planting, a civic stone plaza and a managed wildlife pond. It intentionally excludes people, animals, benches and the planned route because Unity supplies those as live layers.

```text
Use case: precise-object-edit
Asset type: final environment-only background for a 2D Unity top-down urban-wildlife planning game.
Input images: Image 1 is the approved urban-neighbourhood park visual target. Image 2 is the existing EMPTY gameplay background and demonstrates which dynamic objects must be absent.
Primary request: Convert Image 1 into a clean environment-only park background suitable beneath Unity's live path, token, person and animal layers.
Keep from Image 1: strict orthographic bird's-eye view, wide 3:2 composition, red-brick British urban neighbourhood along the upper exterior, grey perimeter pavements and kerbs, continuous black Victorian railings, classic black municipal lamp posts, regularly spaced mature London plane trees, clipped and lightly worn lawns, maintained flower/shrub beds, the central circular pale-stone plaza, and the managed lower-right wildlife/rain-garden pond with shaped brick/stone edge, reeds and lilies. Preserve the realistic-stylized natural-history lithograph language: believable materials and botanical structure, variable ink contours, stippling, engraved hatching, slightly imperfect colour registration and weathered paper grain.
Remove completely: every person, pigeon, squirrel, fox, all benches, all movable furniture, and the entire black zigzag asphalt route including its pale edging and shadows. Heal those removed areas as uninterrupted mown grass or matching plaza paving. Leave clear, open grass corridors between the left and right entrances, plaza and pond so Unity can draw any confirmed route on top.
Entrance requirement: retain the perimeter railings but leave one clean open gateway gap centred on the left edge and one centred on the right edge, each connecting to the exterior pavement. Do not draw gate leaves inside the gaps because Unity will overlay its own gate sprites.
Detail hierarchy: rich urban/botanical detail at the outer boundary and pond; medium detail in planting beds; calm low-contrast open lawn through the middle and lower-left so transparent game sprites remain readable.
Palette/lighting: charcoal and slate, warm London red brick, parchment stone, moss/olive/sage lawn, restrained rust/plum/coral accents, soft overcast daylight. Keep all colours harmonized and slightly weathered.
Constraints: exactly zero people and zero animals; no path or road across the lawn; no benches; no bins, signs, labels, numbers, UI, token circles, glowing rings, selection halos, cars, playground, sports field, text or watermark. Do not crop the complete rectangular park board. No photorealism, 3D render, cute sticker style, tropical jungle or giant wilderness rocks.
```

The image remains a presentation layer only. Landmark coordinates, path rules and simulation logic still come from the scenario JSON and the confirmed layout packet.

## V0.2 natural-park background

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
- `park-fence-gate-open-v01.png`: Entrance A and its mirrored Exit B.

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

## Open fence gate V0.1

The same transparent gate sprite is used at both park openings. Entrance A uses the source orientation; Exit B mirrors it horizontally so both pairs of gate leaves open toward the park interior. The small A/B badges are drawn separately by Unity.

### Gate generation prompt

```text
Use case: stylized-concept. Asset type: transparent Unity top-down environment sprite. The supplied park board is a STYLE REFERENCE ONLY. Create one clearly readable public-park wrought-iron fence gate viewed in strict orthographic directly overhead view. Orientation is exact: the boundary fence runs vertically from the TOP edge toward the BOTTOM edge of the sprite, with a central pedestrian opening that allows travel horizontally from LEFT to RIGHT. At the upper and lower sides of the opening are two sturdy square warm-stone gateposts. Short dark charcoal wrought-iron fence panels extend vertically upward from the upper post and vertically downward from the lower post. Two matching iron gate leaves are hinged to those posts and stand visibly open about 55 degrees toward the RIGHT side, forming a welcoming open passage; include a small warm gravel threshold inside the opening. The gate should look like a real British urban park entrance, with readable parallel bars and small restrained finials, not an abstract icon or arch. Match the gentle hand-painted semi-realistic park-game illustration, refined texture, soft daylight and palette of the reference. Center the complete gate-and-short-fence assembly with generous padding on a genuinely transparent RGBA background and clean antialiased edges. No people, animals, signs, lettering, labels, arrows, numbers, food, benches, trees, large ground patch, border, vignette, glow, watermark, checkerboard, perspective tilt, front view, or closed gate.
```

### Gate isolation prompt

```text
Use case: precise-object-edit. Asset type: Unity environment sprite isolation plate. Preserve the complete park gate assembly exactly: identical strict top-down composition, vertical fence line, two short fence runs, two stone posts, two open wrought-iron gate leaves pointing right, gravel threshold, bars, finials, colors, painterly rendering, scale, centering, and every object edge. Change ONLY every checkerboard/background pixel outside the gate, fence, posts, and gravel threshold to one perfectly flat uniform chroma-key green RGB (0,255,0), hex #00FF00. The green must stop at clean antialiased object edges. No checkerboard, white/grey squares, vignette, gradient, cast shadow plate, aura, glow, extra fence, extra gate, text, label, arrow, number, border, or watermark. Do not redraw or restyle the gate.
```
