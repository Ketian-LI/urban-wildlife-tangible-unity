# P0 Animal Sprite Set V0.2

Generated on 2026-09-09 with the built-in image generation tool for the Unity P0 prototype.

Assets:

- `pigeon-topdown-v01.png`
- `squirrel-topdown-v02.png` (active; wide C-shaped tail silhouette)
- `fox-topdown-v02.png` (active; narrow straight-tail silhouette)

The V0.1 squirrel and fox are retained as superseded prototypes. Both used a long orange body with a tail extending directly behind, so their silhouettes were too similar at runtime scale. V0.2 separates them primarily through outline shape, then reinforces the distinction with markings.

Shared art direction: single animal seen directly from above, head pointing toward the top of the image, gentle hand-painted storybook game illustration, natural but simplified colouring, transparent background, no ground shadow, text or border. Each file is rotated at runtime to follow movement.

Generation mode: built-in image generation tool. The active V0.2 squirrel and fox were produced as precise-object edits on uniform green isolation plates, then converted to RGBA with `tools/chroma_key_sprite.py --remove-all-green-spill`.

## Runtime display scale

The active real-size reference uses approximately 1 Unity unit per 2 metres of representative body length: pigeon 0.18 units (about 0.36 m), squirrel 0.24 units including tail (about 0.48 m), and fox 0.55 units including tail (about 1.10 m). These values control only the rendered Sprite length, not movement speed, disturbance radius, collider size, or ecological behavior.

## V0.2 silhouette-separation prompts

### Squirrel V0.2

```text
Use case: precise-object-edit
Asset type: Unity top-down animal sprite
Input image: the supplied Eurasian red squirrel is the edit target
Primary request: Redesign the squirrel's silhouette so it is unmistakably different from a fox at very small top-down game-token size.
Subject: one compact Eurasian red squirrel viewed strictly from directly above, head pointing to the TOP of the canvas. Make the head small and rounded with visible short tufted ears; make the torso short, rounded, and compact. Curve the enormous fluffy tail into a broad C-shape along the squirrel's LEFT side, with the tail tip curling slightly upward beside the torso instead of extending straight behind. The sideways tail should make the overall silhouette wide and rounded.
Colour and markings: warm chestnut/russet body, darker paws, small cream cheek/ear accents, bushy tail with a lighter cream outer fringe; no white fox-style tail tip.
Style: preserve the refined hand-painted storybook illustration language and clean dark outline used by the source sprite.
Composition: centered, complete animal silhouette, generous padding, no cropping.
Scene/backdrop: perfectly flat uniform chroma-key green RGB (0,255,0), hex #00FF00 outside the animal.
Constraints: exactly one squirrel; four small paws anatomically attached; no long canine snout, no fox proportions, no straight tail, no extra limb or tail; no ground, shadow, checkerboard, texture, gradient, glow, halo, text, or border.
```

### Fox V0.2

```text
Use case: precise-object-edit
Asset type: Unity top-down animal sprite
Input image: the supplied urban red fox is the edit target
Primary request: Strengthen the fox's silhouette and markings so it is unmistakably different from a squirrel at very small top-down game-token size.
Subject: one lean adult urban red fox viewed strictly from directly above, head pointing to the TOP of the canvas. Keep a long narrow angular torso, an elongated pointed muzzle, and two large sharp triangular ears. Make all four lower legs and paws clearly charcoal-black. Keep the tail long, comparatively narrow, and almost straight behind the body, ending in one large high-contrast cream-white tail tip that occupies roughly the final quarter of the tail.
Colour and markings: saturated burnt orange back, cream-white muzzle and thin neck chevron, charcoal ears and legs, bright cream-white tail tip.
Style: preserve the refined hand-painted storybook illustration language and clean dark outline used by the source sprite.
Composition: centered, complete animal silhouette, generous padding, no cropping.
Scene/backdrop: perfectly flat uniform chroma-key green RGB (0,255,0), hex #00FF00 outside the animal.
Constraints: exactly one fox; four legs anatomically attached; no rounded squirrel body, no curled tail, no oversized fluffy squirrel tail, no extra limb or tail; no ground, shadow, checkerboard, texture, gradient, glow, halo, text, or border.
```

## Final prompt set

### Pigeon

```text
Use case: stylized-concept
Asset type: Unity game character sprite
Primary request: a single urban grey pigeon viewed directly from above, full body, wings folded, head pointing straight upward for rotation in a top-down game
Style/medium: gentle hand-painted storybook game illustration, naturalistic but simplified, warm soft texture, consistent with a calm illustrated British city park
Composition/framing: one animal centered, complete silhouette, generous transparent padding, no cropping
Lighting/mood: soft neutral daylight, calm and friendly
Color palette: grey-blue plumage with subtle iridescent green-purple neck accents and small coral feet
Constraints: genuinely transparent background and preserved alpha; no ground, no cast shadow, no border, no text, no icon frame, no extra animals, no watermark
```

### Squirrel

```text
Use case: stylized-concept
Asset type: Unity game character sprite
Primary request: a single Eurasian red squirrel viewed directly from above, full body, head pointing straight upward for rotation in a top-down game, large bushy tail trailing naturally behind
Style/medium: gentle hand-painted storybook game illustration, naturalistic but simplified, warm soft texture, consistent with a calm illustrated British city park
Composition/framing: one animal centered, complete readable silhouette, generous transparent padding, no cropping
Lighting/mood: soft neutral daylight, calm and friendly
Color palette: warm russet brown, cream underside details, subtle dark paws and eyes
Constraints: genuinely transparent background and preserved alpha; no ground, no cast shadow, no border, no text, no icon frame, no extra animals, no watermark
```

### Fox

```text
Use case: stylized-concept
Asset type: Unity game character sprite
Primary request: a single urban red fox viewed directly from above, full body, head pointing straight upward for rotation in a top-down game, slim body and long tail with a pale tip
Style/medium: gentle hand-painted storybook game illustration, naturalistic but simplified, warm soft texture, consistent with a calm illustrated British city park
Composition/framing: one animal centered, complete readable silhouette, generous transparent padding, no cropping
Lighting/mood: soft neutral daylight, alert but not threatening
Color palette: rich burnt orange, cream muzzle and tail tip, dark legs and ears
Constraints: genuinely transparent background and preserved alpha; no ground, no cast shadow, no border, no text, no icon frame, no extra animals, no watermark
```

These generated images are prototype assets. Before final submission, record the selected licence/provenance statement required by the course and replace any asset that does not meet the final authorship or assessment policy.
