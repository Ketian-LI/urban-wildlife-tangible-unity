# Animation Frames V0.3

Generated on 2026-09-10 with the built-in OpenAI image generation tool. These contact sheets are retained as process evidence and source material; Unity loads the split, transparent, normalized frames from `unity/Assets/Resources/UrbanWildlife/`.

## Processing

1. Generate one 3 × 2 contact sheet with a flat `#00FF00` isolation background.
2. Split it into six square frames, left-to-right across the first row and then the second.
3. Remove the green isolation plate and edge spill with `tools/chroma_key_sprite.py`.
4. Normalize animals to a species reference width and humans to a role reference height with `tools/normalize_sprite_frames.py`.
5. Align every result to a stable horizontal centre and bottom/foot baseline on the same canvas.
6. Import as single transparent Unity Sprites. Runtime disables procedural squash, stretch and lift while these true frames are active.

## Pigeon walk

Source: `pigeon-walk-sheet-v01.png`

```text
Use case: precise-object-edit
Asset type: six-frame Unity sprite sheet for an adult feral pigeon walking cycle.
Input images: Image 1 is the exact pigeon identity, anatomy, palette and right-facing side-profile reference. Image 2 shows the alternate leg phase. Image 3 is the approved six-frame natural-history print treatment and identity reference.
Primary request: Create exactly six equal square frames in a clean 3-column by 2-row sheet, read left-to-right across the top row then the bottom row. Show the same adult feral pigeon completing one natural walking cycle to the RIGHT: frame 1 left foot forward/right foot back at contact; frame 2 weight transfer with the rear foot lifting; frame 3 feet passing under the body; frame 4 right foot forward/left foot back at contact; frame 5 opposite weight transfer; frame 6 opposite passing pose, ready to loop to frame 1. Add only the subtle natural counter-phase head bob that accompanies pigeon walking.
Critical motion rule: the two pink legs and feet must visibly alternate front/back through the sequence. Every leg must remain attached beneath the body and anatomically readable. Keep the torso, head, wings and tail the same apparent size in every cell; do not fake motion by scaling the whole bird, stretching the torso or changing camera distance.
Identity/anatomy: slate-grey urban pigeon, dark wing bars, modest turquoise and muted violet neck iridescence, orange-red eye, pale cere, dark beak, two pink legs and two dark-clawed feet. Natural adult proportions, not cute or chibi.
Style/medium: realistic-stylized natural-history lithograph matching Image 3 and the urban park: variable dark ink contours, feather hatching, stippling, subtle imperfect colour registration and weathered print grain. Harmonized slightly muted palette under soft overcast daylight.
Composition: one complete right-facing bird centered in each square with equal generous padding. Use one identical ground/foot baseline, stable body centre and stable beak-to-tail length across all six frames. Adjacent poses must read as continuous motion at 8 FPS.
Background: perfectly flat uniform chroma green #00FF00 edge to edge in every cell.
Constraints: no ground, shadow, plants, path, text, labels, numbers, cell dividers, border, checkerboard, extra bird, extra leg, missing foot, detached limb, cropped tail or watermark.
```

## Squirrel walk

Source: `squirrel-walk-sheet-v01.png`

```text
Use case: precise-object-edit
Asset type: six-frame Unity sprite sheet for an adult Eurasian red squirrel walking cycle.
Input images: Image 1 is the exact squirrel identity, anatomy, palette and right-facing side-profile reference. Image 2 shows an alternate limb phase. Image 3 is the approved six-frame natural-history print treatment and identity reference.
Primary request: Create exactly six equal square frames in a clean 3-column by 2-row sheet, read left-to-right across the top row then the bottom row. Show the same adult red squirrel completing one natural quadruped walking cycle to the RIGHT: frame 1 left foreleg and right hind leg forward at contact; frame 2 weight transfer with the opposite pair beginning to lift; frame 3 limbs passing beneath the torso; frame 4 right foreleg and left hind leg forward at contact; frame 5 opposite weight transfer; frame 6 opposite passing pose, ready to loop to frame 1.
Critical motion rule: all four legs and paws must visibly alternate front/back in a believable diagonal walking pattern. Every limb remains attached at shoulder or hip and passes beneath the body; no duplicated, merged or floating paws. Keep torso, head and the large C-shaped tail at the same apparent size and height in every cell. The tail may make only a very small secondary sway; do not fake motion by scaling the whole squirrel, stretching its body or changing camera distance.
Identity/anatomy: warm russet coat, cream chest and underside, small tufted ears, natural dark eye, exactly four legs and one very large fluffy tail curved upward and backward into a broad C-shape. Natural adult proportions, not cute or chibi.
Style/medium: realistic-stylized natural-history lithograph matching Image 3 and the urban park: variable dark ink contours, fine fur hatching, stippling, subtle imperfect colour registration and weathered print grain. Harmonized slightly muted rust/parchment palette under soft overcast daylight.
Composition: one complete right-facing squirrel centered in each square with equal generous padding. Use one identical paw baseline, stable body centre and stable nose-to-tail length across all six frames. Adjacent poses must read as continuous motion at 8 FPS.
Background: perfectly flat uniform chroma green #00FF00 edge to edge in every cell.
Constraints: no ground, shadow, plants, path, text, labels, numbers, cell dividers, border, checkerboard, extra animal, extra tail, extra limb, detached paw, cropped tail or watermark.
```

## Fox walk

Source: `fox-walk-sheet-v01.png`

```text
Use case: precise-object-edit
Asset type: six-frame Unity sprite sheet for a lean adult urban red fox walking cycle.
Input images: Image 1 is the exact fox identity, anatomy, palette and right-facing side-profile reference. Image 2 shows an alternate limb phase. Image 3 is the approved six-frame natural-history print treatment and identity reference.
Primary request: Create exactly six equal square frames in a clean 3-column by 2-row sheet, read left-to-right across the top row then the bottom row. Show the same adult red fox completing one calm natural walking cycle to the RIGHT: frame 1 left foreleg and right hind leg extended forward at contact while the other diagonal pair trails; frame 2 weight transfer and rear-paw lift; frame 3 all limbs passing beneath the torso; frame 4 right foreleg and left hind leg extended forward at contact while the opposite pair trails; frame 5 opposite weight transfer and rear-paw lift; frame 6 opposite passing pose, ready to loop to frame 1.
Critical motion rule: exactly four black lower legs and four paws must be visible and must clearly alternate front/back as two diagonal pairs across the sequence. Each leg remains anatomically connected at shoulder or hip. Keep the torso, head, ears and long white-tipped tail the same apparent size and height in every cell; keep the spine nearly level. The tail may make only a small counter-sway. Do not fake motion by scaling the whole fox, stretching the body, bouncing dramatically or changing camera distance.
Identity/anatomy: slim British urban red fox, elongated pointed muzzle, large triangular ears, burnt-russet coat, white throat/muzzle/underside, charcoal-black lower legs, and one long bushy tail with a high-contrast cream-white tip. Natural adult proportions; never resemble a dog, wolf or squirrel.
Style/medium: realistic-stylized natural-history lithograph matching Image 3 and the urban park: variable dark ink contours, engraved fur hatching, fine stippling, subtle imperfect colour registration and weathered print grain. Harmonized slightly muted rust/charcoal/parchment palette under soft overcast daylight.
Composition: one complete right-facing fox centered in each square with equal generous padding. Use one identical paw baseline, stable body centre and stable nose-to-tail length across all six frames. Adjacent poses must read as continuous motion at 8 FPS.
Background: perfectly flat uniform chroma green #00FF00 edge to edge in every cell.
Constraints: no ground, shadow, plants, path, text, labels, numbers, cell dividers, border, checkerboard, extra animal, extra leg, merged paws, detached limb, cropped tail or watermark.
```

## Dweller walk

Source: `dweller-walk-sheet-v01.png`

```text
Use case: precise-object-edit
Asset type: six-frame Unity sprite sheet for the adult female Dweller walking cycle.
Input images: Image 1 is the exact Dweller identity, clothing, cup, proportions and high-angle front-facing camera reference. Image 2 shows the alternate walking phase. Image 3 is the approved six-frame layout, realistic-stylized print treatment and motion restraint reference.
Primary request: Create exactly six equal square frames in a clean 3-column by 2-row sprite sheet, read left-to-right across the top row then the bottom row. Show the same adult female Dweller completing one natural walking cycle toward the BOTTOM of the canvas: frame 1 left heel/contact forward and right arm forward; frame 2 weight down on the left leg; frame 3 right leg passing beneath the torso; frame 4 right heel/contact forward and left arm forward; frame 5 weight down on the right leg; frame 6 left leg passing beneath the torso, ready to return to frame 1.
Critical stability rule: the top of the hair bun and the lowest shoe sole must occupy the exact same visual height range in all six cells. Keep identical head size, shoulder width, torso length and camera distance. Do not make alternating frames larger or smaller. Keep both shoe soles on one consistent ground baseline except the lifted rear/passing foot. Natural restrained gait only; no vertical bounce, stretching or dramatic stride.
Identity: same adult woman in every cell, brown hair in a bun, warm mustard-orange overshirt/jacket, cream knitted top, earthy brown trousers and dark lace-up shoes. Preserve exactly one small reusable cup securely in the same hand through every frame; the free arm swings naturally. Realistic adult anatomy about 7.25 heads tall before high-angle foreshortening. Exactly two connected arms, two connected legs and two shoes.
Camera/composition: consistent high-angle front-facing orthographic map-game view. Face, chest and both shoe toes point toward the BOTTOM in every cell. Full figure centered with equal generous padding and a stable body centre.
Style/medium: realistic-stylized natural-history lithograph coherent with the urban park and Image 3: variable dark ink contours, clothing hatching, fine stippling, subtle imperfect colour registration and weathered print grain. Mature editorial game art, harmonized mustard/earth/parchment palette under soft overcast daylight.
Background: perfectly flat uniform chroma green #00FF00 edge to edge in every cell.
Constraints: no ground, shadow, grass, furniture, other person, text, labels, numbers, cell dividers, border, checkerboard, extra limb, detached or reversed shoe, duplicated or missing cup, cropped body or watermark. Keep identity, wardrobe, camera and scale consistent across all six frames.
```

## Visitor walk

Source: `visitor-walk-sheet-v01.png`

```text
Use case: precise-object-edit
Asset type: six-frame Unity sprite sheet for the adult female Visitor walking cycle.
Input images: Image 1 is the exact Visitor identity, clothing, satchel, proportions and high-angle front-facing camera reference. Image 2 shows the alternate walking phase. Image 3 is the approved six-frame layout, realistic-stylized print treatment and motion restraint reference.
Primary request: Create exactly six equal square frames in a clean 3-column by 2-row sprite sheet, read left-to-right across the top row then the bottom row. Show the same adult female Visitor completing one natural walking cycle toward the BOTTOM of the canvas: frame 1 left heel/contact forward and right arm forward; frame 2 weight down on the left leg; frame 3 right leg passing beneath the torso; frame 4 right heel/contact forward and left arm forward; frame 5 weight down on the right leg; frame 6 left leg passing beneath the torso, ready to return to frame 1.
Critical stability rule: the top of the hair bun and the lowest shoe sole must occupy the exact same visual height range in all six cells. Keep identical head size, shoulder width, torso length and camera distance. Do not make alternating frames larger or smaller. Keep both shoe soles on one consistent ground baseline except the lifted rear/passing foot. Natural restrained gait only; no vertical bounce, stretching or dramatic stride.
Identity: same adult woman in every cell, brown hair in a bun, teal hooded coat with cream lining, burgundy trousers, grey trainers, and one tan crossbody satchel with its strap consistently attached. Realistic adult anatomy about 7.25 heads tall before high-angle foreshortening. Exactly two connected arms, two connected legs and two shoes.
Camera/composition: consistent high-angle front-facing orthographic map-game view. Face, chest and both shoe toes point toward the BOTTOM in every cell. Full figure centered with equal generous padding and a stable body centre. The satchel may show a restrained secondary swing without changing size or side.
Style/medium: realistic-stylized natural-history lithograph coherent with the urban park and Image 3: variable dark ink contours, clothing hatching, fine stippling, subtle imperfect colour registration and weathered print grain. Mature editorial game art, harmonized teal/burgundy/tan palette under soft overcast daylight.
Background: perfectly flat uniform chroma green #00FF00 edge to edge in every cell.
Constraints: no ground, shadow, grass, furniture, other person, text, labels, numbers, cell dividers, border, checkerboard, extra limb, detached or reversed shoe, duplicated or missing satchel, cropped body or watermark. Keep identity, wardrobe, camera and scale consistent across all six frames.
```
