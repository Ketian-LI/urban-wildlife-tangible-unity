# Production action frames V0.2 — 2026-09-10

Generated with the built-in image generation tool after the first pigeon/Visitor pilot was approved. The committed contact sheets are the uncut generation outputs:

- `squirrel-feed-sheet-v01.png`: six squirrel feeding poses, 3 × 2.
- `fox-feed-sheet-v01.png`: six fox ground-feeding poses, 3 × 2.
- `walker-walk-sheet-v01.png`: six Walker locomotion poses, 3 × 2.
- `dweller-sit-sheet-v01.png`: four Dweller sitting poses, 2 × 2; Unity reverses the same sequence for rising.

The sheets were divided left-to-right, top-to-bottom. The Dweller source was 1263 × 1246, so eight pixels were removed from the left and nine from the right before division; the figures were not stretched. Uniform green plates were converted to RGBA with `tools/chroma_key_sprite.py --remove-all-green-spill`. Active frames live in `unity/Assets/Resources/UrbanWildlife/Animals/` and `Humans/`.

Idle and turning remain shared procedural keyframes. This prevents redundant artwork while keeping the six P0 actions and every role's signature activity complete.

## Squirrel feeding prompt

```text
Use case: precise-object-edit
Asset type: six-frame Unity sprite sheet for a red squirrel feeding animation.
Input images: Image 1 is the squirrel identity and anatomy reference. Image 2 is the approved six-frame animation-sheet format and print treatment reference. Image 3 is the approved urban-park palette/style reference.
Primary request: Create exactly six equal square frames in a clean 3-column by 2-row sheet, read left-to-right across the top row then the bottom row. Show one consistent adult Eurasian red squirrel in a restrained feeding loop: 1 alert right-facing side profile on all four feet; 2 lowering and sniffing; 3 crouching and bringing a small seed or nut between both forepaws; 4 holding and nibbling it with a subtle head change; 5 lowering forepaws and beginning to rise; 6 returned alert on all four feet.
Identity/anatomy: warm russet coat, cream chest and underside, small tufted ears, dark natural eye, four connected legs/paws, and a very large fluffy tail curved upward and backward into a broad C-shape. Natural adult squirrel proportions, more realistic and less cute than Image 1; no giant cartoon eye. Keep exact species cues, consistent scale, orientation and tail silhouette in all six cells.
Style/medium: realistic-stylized natural-history lithograph matching the urban park: variable dark ink contours, fine fur hatching, stippling, subtle imperfect colour registration and weathered print grain. Detailed enough to recognize at large size but with a bold readable game silhouette. Harmonized moss/rust/parchment palette under soft overcast daylight.
Composition: one complete squirrel centered in each square with equal generous padding and a stable ground/foot baseline. The animal faces right in every frame. Adjoining poses must read as a continuous 8 FPS loop.
Background: perfectly flat uniform chroma green #00FF00 edge to edge in every cell.
Constraints: no ground, shadow, plants, path, furniture, text, labels, numbers, cell dividers, border, checkerboard, extra animal, extra tail, detached paws, cropped tail or watermark. Do not change species between frames.
```

## Fox feeding prompt

```text
Use case: precise-object-edit
Asset type: six-frame Unity sprite sheet for an urban red fox feeding animation.
Input images: Image 1 is the fox identity and anatomy reference. Image 2 is the approved six-frame animation-sheet format and print treatment reference. Image 3 is the approved urban-park palette/style reference.
Primary request: Create exactly six equal square frames in a clean 3-column by 2-row sheet, read left-to-right across the top row then the bottom row. Show one consistent lean adult urban red fox in a restrained ground-feeding loop: 1 alert right-facing side profile on all four feet; 2 head and shoulders beginning to lower while sniffing; 3 muzzle close to the ground locating a small food scrap; 4 taking one small bite and chewing with head still low; 5 lifting muzzle and shoulders, swallowing; 6 returned to alert standing profile.
Identity/anatomy: natural slim British red fox, elongated pointed muzzle, large triangular ears, burnt-russet coat, white throat/muzzle/underside, charcoal-black lower legs, and a long bushy tail ending in a high-contrast cream-white tip. Exactly four connected legs and one tail. Preserve the long low canine silhouette and stable body/tail proportions in all frames; it must never resemble a squirrel, dog or wolf.
Style/medium: realistic-stylized natural-history lithograph matching the urban park: variable dark ink contours, engraved fur hatching, fine stippling, subtle imperfect colour registration and weathered print grain. Mature editorial game art, believable anatomy, bold readable silhouette, harmonized rust/charcoal/parchment palette under soft overcast daylight.
Composition: one complete fox centered in each square with equal generous padding and a stable paw baseline. The animal faces right in every frame. Adjoining poses read as a continuous 8 FPS loop.
Background: perfectly flat uniform chroma green #00FF00 edge to edge in every cell.
Constraints: no ground, shadow, plants, path, furniture, text, labels, numbers, cell dividers, border, checkerboard, extra animal, extra limb, extra tail, cropped tail or watermark. No food held in paws and no dramatic attack pose.
```

## Walker walking prompt

```text
Use case: precise-object-edit
Asset type: six-frame Unity sprite sheet for an adult Walker locomotion cycle.
Input images: Image 1 is the Walker identity, clothing, proportions and camera reference. Image 2 is the approved human animation-sheet format reference. Image 3 is the approved urban-park palette and realistic-stylized print reference.
Primary request: Create exactly six equal square frames in a clean 3-column by 2-row sprite sheet, read left-to-right across the top row then the bottom row. Show one consistent adult male Walker completing one natural walk cycle toward the BOTTOM of the canvas: 1 left heel/contact forward with right arm forward; 2 weight down on left leg; 3 right leg passing under torso; 4 right heel/contact forward with left arm forward; 5 weight down on right leg; 6 left leg passing under torso, ready to return to frame 1.
Identity: same adult man in every cell, short brown hair, friendly natural face, bright blue hooded jacket, navy backpack with both straps attached, dark navy trousers and muted red trainers. Realistic adult anatomy about 7.5 heads tall before high-angle foreshortening. Exactly two connected arms, two connected legs and two shoes. Face, chest and both shoe toes point toward the BOTTOM of every cell; shoes must never face backward.
Camera/composition: consistent high-angle front-facing orthographic map-game view, full body centered with equal generous padding, stable head-to-foot scale and stable body centre. No frame may be cropped. Natural restrained arm swing and leg spacing; no dramatic running or jumping.
Style/medium: realistic-stylized natural-history lithograph coherent with the park and Visitor set: variable dark ink contours, clothing hatching, fine stippling, subtle imperfect colour registration and weathered print grain. Mature editorial game art, unified saturation and soft overcast daylight; not glossy anime and not cute/chibi.
Background: perfectly flat uniform chroma green #00FF00 edge to edge in every cell.
Constraints: no ground, shadow, path, plants, other person, props, text, labels, numbers, cell dividers, border, checkerboard, extra limb, detached shoe, reversed foot, duplicated backpack or watermark. Keep identity, wardrobe, camera and scale consistent across all frames.
```

## Dweller sitting prompt

```text
Use case: precise-object-edit
Asset type: four-frame Unity sprite sheet for an adult Dweller sitting-down transition, reversible for standing up.
Input images: Image 1 is the Dweller identity, clothing, cup, adult proportions and camera reference. Image 2 is the approved human animation-sheet format and print-treatment reference. Image 3 is the approved urban-park palette/style reference.
Primary request: Create exactly four equal square frames in a clean 2-column by 2-row sprite sheet, read left-to-right across the top row then the bottom row. Show one consistent adult female Dweller moving from standing to a stable cross-legged seated rest on the grass without furniture: 1 relaxed upright stance; 2 knees bending and torso lowering while balancing; 3 low crouch with one hand briefly supporting the descent; 4 comfortably seated cross-legged, torso upright. The same frames will be played in reverse for rising, so every transition must connect naturally.
Identity: same adult woman in every cell, brown hair in a bun, warm mustard-orange overshirt/jacket, cream knitted top, earthy brown trousers and dark lace-up shoes. Preserve exactly one small reusable cup held securely in the same hand throughout without spilling. Realistic adult anatomy about 7.25 heads tall before high-angle foreshortening. Exactly two connected arms, two connected legs and two shoes.
Camera/composition: consistent high-angle front-facing orthographic map-game view, face and chest readable and oriented toward the BOTTOM of each cell. Full figure centered with equal generous padding, stable body centre and consistent apparent scale. Seated frame should become shorter naturally rather than being artificially squashed. No bench or chair.
Style/medium: realistic-stylized natural-history lithograph coherent with the park and Visitor set: variable dark ink contours, clothing hatching, fine stippling, subtle imperfect colour registration and weathered print grain. Mature editorial game art, harmonized mustard/earth/parchment palette under soft overcast daylight; not glossy anime and not cute/chibi.
Background: perfectly flat uniform chroma green #00FF00 edge to edge in every cell.
Constraints: no ground, shadow, grass, furniture, other person, text, labels, numbers, cell dividers, border, checkerboard, extra limb, detached shoe, reversed foot, duplicated or missing cup, cropped body or watermark. Keep identity, wardrobe, camera and lighting consistent across all four frames.
```

These are prototype assets. Before final submission, record the selected provenance statement and confirm the course policy for AI-assisted visual material.
