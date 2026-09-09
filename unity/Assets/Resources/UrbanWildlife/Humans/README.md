# P0 Human Sprite Set V0.5

Generated on 2026-09-09 with the built-in image generation tool for the Unity P0 prototype.

Assets:

- `walker-topdown-v05.png` (active; realistic adult proportions)
- `dweller-topdown-v03.png` (active; realistic adult proportions)
- `visitor-topdown-v05.png` (active; realistic adult proportions)

The earlier Walker, Dweller, and Visitor files are retained as superseded prototypes. V0.4 established a consistent high-angle front-facing view. V0.5 reduces the oversized heads and upper bodies, lengthens the legs, and restores natural adult shoulder, arm, torso, and hip proportions.

All three characters use the same source orientation: their visible faces and shoe toes point toward the bottom of the image. Runtime applies a fixed 180-degree heading offset and then rotates each complete Sprite, so the visible front remains aligned with travel. Their clothing, pose, and one small accessory distinguish each role without relying only on colour. The active files are four-channel PNGs with transparent pixels.

## Generation mode

Built-in image generation tool in `precise-object-edit` mode. The three active V0.5 role images were generated on uniform green isolation plates and converted to real alpha with `tools/chroma_key_sprite.py`; this keeps the delivered Unity assets as RGBA PNGs without a baked checkerboard.

## V0.5 realistic adult proportions

The proportion targets are visual references, not personal biometric claims. Walker uses a 7.5-head adult baseline; Dweller and Visitor use a 7.25-head adult baseline before high-angle foreshortening. Runtime display lengths are 0.89, 0.84, and 0.85 Unity units respectively.

### Walker final prompt

    Use case: precise-object-edit.
    Asset type: Unity 2D high-angle top-down adult human sprite on a chroma-key isolation plate.
    Primary request: Correct the Walker's BODY PROPORTIONS to realistic adult anatomy while preserving his recognizable face, identity, clothing, colors, backpack, walking direction, and high-angle front-facing view. His current head and upper body read too large and youthful. Redesign the full silhouette as a normally proportioned adult man approximately 7.5 heads tall before perspective foreshortening: reduce the head relative to the body, use natural adult shoulder width, a proportionate torso, arms reaching around mid-thigh, and longer realistically proportioned legs. Keep only mild high-angle foreshortening so the body does not become chibi or big-headed.
    Subject invariants: adult man, short brown hair, bright blue hooded jacket, navy backpack, dark navy trousers, muted red trainers; same friendly facial identity and painterly storybook line-art style. His visible face and both shoe toes point toward the BOTTOM of the canvas. Exactly one head, two connected arms, two connected legs, and two connected shoes; no reversed feet, extra limbs, or childlike proportions.
    Composition: full body centered, complete silhouette, generous padding, no cropping, approximately 82% of canvas height.
    Background: perfectly uniform solid #00FF00 green edge to edge.
    Do not add ground, cast shadow, glow, halo, border, text, checkerboard, or other objects.

### Dweller final prompt

    Use case: precise-object-edit.
    Asset type: Unity 2D high-angle top-down adult human sprite on a chroma-key isolation plate.
    Primary request: Correct the Dweller's BODY PROPORTIONS to realistic adult anatomy while preserving her recognizable face, identity, clothing, colors, cup, walking direction, and high-angle front-facing view. Her current head and upper body read too large and youthful. Redesign the full silhouette as a normally proportioned adult woman approximately 7.25 heads tall before perspective foreshortening: reduce the head relative to the body, use natural adult shoulder and hip proportions, a proportionate torso, arms reaching around mid-thigh, and longer realistically proportioned legs. Keep only mild high-angle foreshortening so the body does not become chibi or big-headed.
    Subject invariants: adult woman with brown hair in a bun, mustard-orange overshirt/coat, cream knitted top, earthy brown trousers, dark lace-up shoes, holding exactly one small reusable cup; same friendly facial identity and painterly storybook line-art style. Her visible face and both shoe toes point toward the BOTTOM of the canvas. Exactly one head, two connected arms, two connected legs, and two connected shoes; no reversed feet, extra limbs, extra cup, or childlike proportions.
    Composition: full body centered, complete silhouette, generous padding, no cropping, approximately 82% of canvas height.
    Background: perfectly uniform solid #00FF00 green edge to edge.
    Do not add ground, cast shadow, glow, halo, border, text, checkerboard, or other objects.

### Visitor final prompt

    Use case: precise-object-edit.
    Asset type: Unity 2D high-angle top-down adult human sprite on a chroma-key isolation plate.
    Primary request: Correct the Visitor's BODY PROPORTIONS to realistic adult anatomy while preserving her recognizable face, identity, clothing, colors, satchel, walking direction, and high-angle front-facing view. Her current head and upper body read too large and youthful. Redesign the full silhouette as a normally proportioned adult woman approximately 7.25 heads tall before perspective foreshortening: reduce the head relative to the body, use natural adult shoulder and hip proportions, a proportionate torso, arms reaching around mid-thigh, and longer realistically proportioned legs. Keep only mild high-angle foreshortening so the body does not become chibi or big-headed.
    Subject invariants: adult woman with brown hair in a bun, teal hooded coat, burgundy trousers, tan crossbody satchel, grey trainers; same friendly facial identity and painterly storybook line-art style. Her visible face and both shoe toes point toward the BOTTOM of the canvas. Exactly one head, two connected arms, two connected legs, and two connected shoes; no reversed feet, extra limbs, or childlike proportions. Satchel remains naturally attached by its strap.
    Composition: full body centered, complete silhouette, generous padding, no cropping, approximately 82% of canvas height.
    Background: perfectly uniform solid #00FF00 green edge to edge.
    Do not add ground, cast shadow, glow, halo, border, text, checkerboard, or other objects.

## V0.4 front-facing view

The phrase “front-facing” means that the face is readable from a high-angle map view; it does not change the project to an eye-level or side-view camera. The generated fronts point toward the bottom of the source image, so P0HumanSimulation applies a fixed 180-degree offset before its normal movement heading and sway.

### Walker final prompt

```text
Use case: precise-object-edit.
Asset type: Unity 2D top-down human character sprite on a chroma-key isolation plate.
Edit the referenced Walker while preserving his recognizable identity and palette: adult man, short brown hair, bright blue hooded jacket, navy backpack, dark navy trousers, muted red trainers. Change the pose and camera to a consistent high-angle three-quarter TOP-DOWN view (camera about 65–75 degrees above the ground) in which the character is walking TOWARD THE TOP OF THE CANVAS but lifts/tilts his face enough toward the camera that both eyes, nose, and mouth are clearly readable. The front of his body, chest, and facial expression must be visible; do not show only the crown or back of his head. Keep a compact readable map-game silhouette and the same hand-painted storybook line-art style.
Anatomy constraints: exactly one head, two connected arms, two connected legs, two shoes; both shoes point in the same direction as his face and body; no backward feet, floating limbs, or duplicated parts. Backpack must sit naturally behind his shoulders.
Composition: centered, full body, generous padding, no cropping. The entire character occupies about 78% of canvas height.
Background: perfectly uniform solid #00FF00 green, edge to edge. No transparency preview checkerboard, no texture, no gradient.
Do not add ground, floor, cast shadow, glow, halo, border, text, labels, icons, or other objects.
```

### Dweller final prompt

```text
Use case: precise-object-edit.
Asset type: Unity 2D top-down human character sprite on a chroma-key isolation plate.
Edit the referenced Dweller while preserving her recognizable identity and palette: adult woman with brown hair in a bun, mustard-orange overshirt/coat, cream knitted top, earthy brown trousers, dark lace-up shoes, holding one small reusable cup. Change the pose and camera to a consistent high-angle three-quarter TOP-DOWN view (camera about 65–75 degrees above the ground) in which the character is walking TOWARD THE TOP OF THE CANVAS but lifts/tilts her face enough toward the camera that both eyes, nose, and mouth are clearly readable. The front of her body, chest, and facial expression must be visible; do not show only the crown of her head and do not make her stare straight down. Keep a compact readable map-game silhouette and the same hand-painted storybook line-art style.
Anatomy constraints: exactly one head, two connected arms, two connected legs, two shoes; both shoes point in the same direction as her face and body; no backward feet, floating limbs, duplicated parts, or extra cups. Cup stays in one hand.
Composition: centered, full body, generous padding, no cropping. The entire character occupies about 78% of canvas height.
Background: perfectly uniform solid #00FF00 green, edge to edge. No transparency preview checkerboard, no texture, no gradient.
Do not add ground, floor, cast shadow, glow, halo, border, text, labels, icons, or other objects.
```

### Visitor final prompt

```text
Use case: precise-object-edit.
Asset type: Unity 2D top-down human character sprite on a chroma-key isolation plate.
Edit the referenced Visitor while preserving her recognizable identity and palette: adult woman, brown hair in a bun, teal hooded coat, burgundy trousers, tan crossbody satchel, grey trainers. Change the pose and camera to a consistent high-angle three-quarter TOP-DOWN view (camera about 65–75 degrees above the ground) in which the character is walking TOWARD THE TOP OF THE CANVAS but lifts/tilts her face enough toward the camera that both eyes, nose, and mouth are clearly readable. The front of her body, chest, satchel strap, and facial expression must be visible; do not show only the crown or back of her head. Keep a compact readable map-game silhouette and the same hand-painted storybook line-art style.
Anatomy constraints: exactly one head, two connected arms, two connected legs, two shoes; both shoes point in the same direction as her face and body; no backward feet, floating limbs, or duplicated parts. Satchel remains naturally connected by its strap.
Composition: centered, full body, generous padding, no cropping. The entire character occupies about 78% of canvas height.
Background: perfectly uniform solid #00FF00 green, edge to edge. No transparency preview checkerboard, no texture, no gradient.
Do not add ground, floor, cast shadow, glow, halo, border, text, labels, icons, or other objects.
```

## V0.3 shoe-direction correction

### Walker final prompt

```text
Use case: precise-object-edit
Asset type: Unity top-down human character sprite
Input image: the supplied blue-clothed male Walker is the edit target
Primary request: Correct ONLY the orientation and visible construction of both red sneakers. The character's face and walking direction point toward the TOP of the canvas, so both shoes must also point toward the TOP.
Shoe anatomy: keep both shoes below the hips and connected to the legs. The toe boxes point upward toward the knees/head and are partly foreshortened. The lowest visible end of each shoe at the BOTTOM of the canvas must be the BACK HEEL: show a compact heel counter, rear collar seam, and rear outsole edge. Do not show a rounded toe cap or front-facing laces at the bottom. Both shoes face the same forward direction as the head.
Invariants: preserve the exact person, hair, head, arms, jacket, backpack, trousers, body pose, leg positions, proportions, painterly outlined style, colours, scale, and placement. Exactly two legs and two shoes.
Scene/backdrop: perfectly flat uniform chroma-key green RGB (0,255,0), hex #00FF00, outside the character.
Constraints: edit only the shoes; no shoe above the head; no reversed foot; no detached, duplicated, crossed, or malformed limb; no checkerboard, texture, gradient, shadow, glow, halo, text, or border.
```

### Visitor final prompt

```text
Use case: precise-object-edit
Asset type: Unity top-down human character sprite
Input image: the supplied teal-clothed female Visitor is the edit target
Primary request: Correct ONLY the orientation and visible construction of both grey sneakers. The character's face and walking direction point toward the TOP of the canvas, so both shoes must also point toward the TOP.
Shoe anatomy: keep both shoes below the hips and connected to the legs. The toe boxes point upward toward the knees/head and are partly foreshortened. The lowest visible end of each shoe at the BOTTOM of the canvas must be the BACK HEEL: show a compact heel counter, rear collar seam, and rear outsole edge. Do not show a rounded toe cap or front-facing laces at the bottom. Both shoes face the same forward direction as the head.
Invariants: preserve the exact person, hair bun, arms, teal coat, tan cross-body satchel, burgundy trousers, body pose, leg positions, proportions, painterly outlined style, colours, scale, and placement. Exactly two legs and two shoes.
Scene/backdrop: perfectly flat uniform chroma-key green RGB (0,255,0), hex #00FF00, outside the character.
Constraints: edit only the shoes; no shoe above the head; no reversed foot; no detached, duplicated, crossed, or malformed limb; no checkerboard, texture, gradient, shadow, glow, halo, text, or border.
```

## V0.2.1 shoe-pose correction

### Walker correction prompt

```text
Use case: precise object edit for a top-down Unity game sprite.

Edit ONLY the lower-body walking pose and shoes of the supplied blue-clothed male Walker. Preserve his exact identity, brown hair, blue hooded jacket, navy backpack, arms, body proportions, painterly outlined illustration style, lighting, colors, and strict overhead camera angle.

Correct the anatomy and silhouette:
- The top of the canvas is the character's head direction; the bottom is behind/below the body.
- Remove the red shoe and leg that currently appear above the man's head.
- Show exactly two legs and exactly two red sneakers, both anatomically connected from the hips through the legs to the ankles.
- Both shoes must be entirely below the hips and below the backpack, in the lower half of the character silhouette.
- Use a restrained natural walking stance: one foot only slightly ahead of the other, modest separation, no extreme foreshortening, no crossed, floating, detached, duplicated, or overlapping limbs.
- At small game-token size the silhouette must read clearly as a normal person walking, never as a shoe above the head.

Keep the character centered and keep the same overall scale. Output a clean isolated sprite with genuinely transparent alpha everywhere outside the character; no background, no floor, no checkerboard pattern, no glow, no halo, no cast shadow, no text, and no border.
```

### Visitor correction prompt

```text
Use case: precise object edit for a top-down Unity game sprite.

Edit ONLY the lower-body walking pose and shoes of the supplied teal-blue-clothed female Visitor. Preserve her exact identity, brown hair bun, teal hooded coat, burgundy trousers, tan cross-body satchel, arms, body proportions, painterly outlined illustration style, lighting, colors, and strict overhead camera angle.

Correct the anatomy and silhouette:
- The top of the canvas is the character's head direction; the bottom is behind/below the body.
- Remove the grey shoe and leg that currently appear above the woman's head.
- Show exactly two legs and exactly two grey sneakers, both anatomically connected from the hips through the legs to the ankles.
- Both shoes must be entirely below the hips and below the coat hem, in the lower half of the character silhouette.
- Use a restrained natural walking stance: one foot only slightly ahead of the other, modest separation, no extreme foreshortening, no crossed, floating, detached, duplicated, or overlapping limbs.
- At small game-token size the silhouette must read clearly as a normal person walking, never as a shoe above the head.

Keep the character centered and keep the same overall scale. Output a clean isolated sprite with genuinely transparent alpha everywhere outside the character; no background, no floor, no checkerboard pattern, no glow, no halo, no cast shadow, no text, and no border.
```

Both correction outputs were passed through a background-extraction edit. Because that edit returned a baked checkerboard rather than alpha, the same characters were placed on a uniform `#00FF00` isolation plate with the built-in image tool and then converted deterministically to RGBA. No pose or character content was changed during that final format conversion.

### Follow-up background prompt (both characters)

```text
Use case: exact background removal for a Unity sprite.

Keep the supplied character completely unchanged pixel-for-pixel in design, anatomy, pose, clothing, facial/hair details, bags, outlines, colors, scale, and placement. Do not redraw or restyle the person.

Remove the entire grey-and-white checkerboard background and any paper-like background texture. Convert every pixel outside the character silhouette to genuine transparent alpha (RGBA alpha 0). Preserve clean antialiased edges around the character. Do not leave a checkerboard, grey field, white field, black field, glow, halo, shadow, outline expansion, text, or border. The final file must be a clean isolated character sprite on true transparency.
```

### Final isolation-plate prompt (both characters)

```text
Use case: background-extraction intermediate plate.
Asset type: Unity top-down human sprite.
Primary request: Keep the character design, pose, anatomy, clothing, bags, outlines, colors, scale, and placement completely unchanged. Replace ONLY every background pixel outside the character silhouette with one perfectly flat, uniform chroma-key green color RGB (0, 255, 0), hex #00FF00.
Scene/backdrop: featureless solid #00FF00.
Constraints: the green must stop precisely at the antialiased character edge; no checkerboard, texture, wrinkles, gradients, shadows, glow, halo, text, border, or green inside the character. Do not redraw or restyle the person.
```

The final PNGs preserve the generated pose and interior artwork. The local conversion removes green background pixels and adjusts boundary colour to reduce a green fringe. Prompt preservation requests above are instructions to the generator, not a guarantee that iterative image generation preserved every original pixel.

## Exact generation prompts

### Walker

```text
Use case: stylized-concept
Asset type: Unity top-down human character sprite
Input images: the supplied park map and pigeon are style references only
Primary request: one adult park Walker seen directly from above, full body, head pointing straight upward for rotation in a top-down game, captured in a natural mid-stride walking pose
Subject details: practical blue-grey lightweight jacket, dark navy trousers, muted red trainers, compact navy day backpack; clean readable silhouette
Style/medium: gentle hand-painted storybook game illustration, naturalistic but simplified, refined painterly texture, visually cohesive with the reference park and animal
Composition/framing: one person centered, complete head-to-feet silhouette, generous genuinely transparent padding, no cropping
Lighting/mood: soft neutral daylight, calm everyday park visitor
Constraints: actual transparent background and preserved alpha; only one person; no ground, no cast shadow, no aura, no glow, no vignette, no border, no text, no icon frame, no watermark; transparent pixels outside a tight clean character edge
```

### Dweller

```text
Use case: stylized-concept
Asset type: Unity top-down human character sprite
Input images: the supplied park map and pigeon are style references only
Primary request: one adult park Dweller seen directly from above, full body, head pointing straight upward for rotation in a top-down game, relaxed upright pose with feet slightly apart
Subject details: warm mustard-orange overshirt, cream top, earthy brown trousers, dark shoes, holding one small reusable cup close to the body; relaxed readable silhouette that suggests lingering in a plaza
Style/medium: gentle hand-painted storybook game illustration, naturalistic but simplified, refined painterly texture, visually cohesive with the reference park and animal
Composition/framing: one person centered, complete head-to-feet silhouette, generous genuinely transparent padding, no cropping
Lighting/mood: soft neutral daylight, calm and unhurried
Constraints: actual transparent background and preserved alpha; only one person; no furniture, no ground, no cast shadow, no aura, no glow, no vignette, no border, no text, no icon frame, no watermark; transparent pixels outside a tight clean character edge
```

### Visitor

```text
Use case: stylized-concept
Asset type: Unity top-down human character sprite
Input images: the supplied park map and pigeon are style references only
Primary request: one adult park Visitor seen directly from above, full body, head pointing straight upward for rotation in a top-down game, captured in a gentle walking pose
Subject details: sage-teal rain jacket, burgundy trousers, neutral shoes, small tan crossbody satchel; curious but natural posture and a clean readable silhouette
Style/medium: gentle hand-painted storybook game illustration, naturalistic but simplified, refined painterly texture, visually cohesive with the reference park and animal
Composition/framing: one person centered, complete head-to-feet silhouette, generous genuinely transparent padding, no cropping
Lighting/mood: soft neutral daylight, friendly and observant
Constraints: actual transparent background and preserved alpha; only one person; no ground, no cast shadow, no aura, no glow, no vignette, no border, no text, no icon frame, no watermark; transparent pixels outside a tight clean character edge
```

## Exact background-extraction prompt

The following prompt was run once for each role, replacing `<ROLE>` with `Walker`, `Dweller`, and `Visitor` respectively.

```text
Use case: background-extraction
Asset type: Unity top-down human character sprite
Input images: Image 1 is the <ROLE> sprite edit target
Primary request: remove the entire grey-and-white checkerboard and every background pixel, and output the same person as a clean cutout on a genuinely transparent background with preserved alpha
Invariants: preserve the person's exact pose, anatomy, face, hairstyle, clothing, colors, accessories, proportions, painterly texture, orientation, and full-body framing; do not redesign or recolor anything
Constraints: actual alpha transparency outside the body; tight clean character edge; preserve fine hair and clothing edges; no checkerboard, no white background, no grey background, no ground, no cast shadow, no halo, no aura, no glow, no vignette, no border, no text, no watermark
```

These generated images are prototype assets. Before final submission, record the selected provenance statement and confirm the course policy for AI-assisted visual material.
