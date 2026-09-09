# P0 Human Sprite Set V0.2

Generated on 2026-09-09 with the built-in image generation tool for the Unity P0 prototype.

Assets:

- `walker-topdown-v01.png`
- `dweller-topdown-v01.png`
- `visitor-topdown-v01.png`

All three characters face toward the top of the source image and are rotated at runtime. Clothing, pose, and one small accessory distinguish each role without relying only on colour. Each initial generation accidentally contained a baked checkerboard; a second background-extraction pass produced the committed files with real alpha transparency. OpenCV inspection confirmed four-channel PNG output and transparent pixels.

## Generation mode

Built-in image generation tool. One generation and one background-extraction edit per role.

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
