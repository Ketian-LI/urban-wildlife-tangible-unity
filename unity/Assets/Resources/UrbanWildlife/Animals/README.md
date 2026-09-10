# P0 Animal Sprite Set

This directory contains the generated Unity animal presentation assets used by the P0 prototype.

## Active V0.4 pigeon feeding sequence

The pigeon now has six independently drawn feeding frames at the shared 8 FPS stepped-animation rate:

- `pigeon-side-feed-01-v01.png`: upright start
- `pigeon-side-feed-02-v01.png`: head lowering
- `pigeon-side-feed-03-v01.png`: first peck
- `pigeon-side-feed-04-v01.png`: second peck
- `pigeon-side-feed-05-v01.png`: rising
- `pigeon-side-feed-06-v01.png`: upright return

The frames were generated as one 3 x 2 contact sheet, split into equal 512 x 512 cells with `tools/split_sprite_sheet.py`, and converted from a uniform green isolation plate to RGBA with `tools/chroma_key_sprite.py --remove-all-green-spill`. Runtime uses the true frames only for the pigeon Feeding state; squirrel and fox retain the procedural pose fallback until their own sets are approved.

Generation mode: built-in image generation tool. Final prompt:

```text
Create a production-ready six-frame Unity sprite sheet for one urban rock pigeon performing a feeding cycle. Arrange exactly six equal square cells in a clean 3-column by 2-row grid, read left-to-right across the top row and then the bottom row: 1 upright alert side profile, 2 lowering the head, 3 beak touching the ground for a first peck, 4 a slightly different second peck, 5 lifting the head and chest, 6 returned upright. Keep the same single pigeon, right-facing side profile, anatomy, size, placement, grey plumage, charcoal wing bars, iridescent green-purple neck, orange eye and coral-red feet in every cell. Use the approved expressive natural-history print direction: confident variable dark ink contours, coloured-pencil hatching, screen-print grain, slightly exaggerated readable silhouette and selective coral/magenta accents; sophisticated rather than childish, with consistent saturation and neutral daylight. Put every cell on perfectly flat uniform chroma green #00FF00. No ground, shadow, labels, numbers, dividers, border, text, extra animal, detached limb, cropping, checkerboard or background texture. Keep generous equal padding and make adjoining poses read as a restrained 8 FPS loop.
```

Contact sheet: `docs/images/unity/animation-frames-v01/pigeon-feed-sheet-v01.png`.

## Active V0.3 side-profile walk set

Updated on 2026-09-10 after comparison with the earlier web simulation. Each species now uses two right-facing side-profile frames. Runtime movement keeps the artwork screen-facing, alternates the frames only while moving, and uses horizontal flipping instead of continuously rotating the entire animal toward every waypoint.

Active assets:

- `pigeon-side-walk-a-v01.png`
- `pigeon-side-walk-b-v01.png`
- `squirrel-side-walk-a-v01.png`
- `squirrel-side-walk-b-v01.png`
- `fox-side-walk-a-v01.png`
- `fox-side-walk-b-v01.png`

The two poses for each species were generated together on a uniform `#00FF00` isolation plate, split into equal cells, and converted to real RGBA transparency with `tools/chroma_key_sprite.py --remove-all-green-spill`. The pigeon keeps its low walking silhouette, the squirrel is distinguished by its tall curled tail, and the fox by its long low body, dark legs, and white tail tip.

### V0.3 generation prompt set

Each species prompt used the current animal design and park board as style references. The common production specification was: a two-frame Unity walk sheet with exactly two equal right-facing side-profile poses, identical proportions and palette, complete anatomy, generous padding, refined hand-painted semi-realistic casual-simulation style, and no ground, shadow, text, border, extra animal, cropping, top-down view, or front view. Species-specific requirements were:

- Pigeon: grey urban rock pigeon, charcoal wing bars, iridescent green-purple neck, orange eye and red feet; contact pose followed by the opposite passing pose with a small head bob.
- Squirrel: British red squirrel, russet coat, cream belly, ear tufts and a large curved fluffy tail; opposite diagonal leg poses with subtle body rise and tail follow-through.
- Fox: lean adult urban red fox, white throat and muzzle, dark lower legs and ears, long bushy white-tipped tail; opposite diagonal leg poses with subtle shoulder, hip and tail follow-through.

## Superseded V0.2 top-down set

Retained assets:

- `pigeon-topdown-v01.png`
- `squirrel-topdown-v02.png` (wide C-shaped tail silhouette)
- `fox-topdown-v02.png` (narrow straight-tail silhouette)

The V0.1 squirrel and fox are retained as superseded prototypes. Both used a long orange body with a tail extending directly behind, so their silhouettes were too similar at runtime scale. V0.2 separated them primarily through outline shape, then reinforced the distinction with markings.

The V0.2 shared art direction was a single animal seen directly from above, head pointing toward the top of the image, gentle hand-painted storybook game illustration, natural but simplified colouring, transparent background, no ground shadow, text or border. These files were rotated at runtime to follow movement.

Generation mode: built-in image generation tool. The V0.2 squirrel and fox were produced as precise-object edits on uniform green isolation plates, then converted to RGBA with `tools/chroma_key_sprite.py --remove-all-green-spill`.

## Active runtime display scale

For readability on the full 60 x 90 cm board, active presentation lengths are pigeon 0.30 units, squirrel 0.44 units including tail, and fox 0.72 units including tail. The ordering remains pigeon < squirrel < fox < human. These values control only the rendered Sprite length, not movement speed, disturbance radius, collider size, or ecological behavior.

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

## Two-frame walking cycle V0.1

Generation mode: `precise-object-edit`. The original active sprites remain frame A. The following generated assets are frame B and are shown only while the agent is moving. Uniform green plates were converted to RGBA with `tools/chroma_key_sprite.py --remove-all-green-spill`.

### Pigeon frame B

```text
Precise object edit for frame B of a two-frame game walk cycle. Preserve this exact pigeon and the existing semi-realistic painted sprite style: strict orthographic top-down view, head pointing to the top edge, compact grey body, folded wings, iridescent neck, coral feet, exact identity, colors, canvas size, scale and centering. Create ONLY an alternate walking step: move the opposite coral foot forward and the other foot backward, with a very subtle forward/down head-bob pose while keeping the body axis and travel direction unchanged. Exactly two legs and two feet; wings remain fully folded; no flying pose. Keep the entire bird visible with the same margin. Replace the background with perfectly flat solid chroma green #00FF00, no texture, no shadow, no ground, no text, no border.
```

### Squirrel frame B

```text
Precise object edit for frame B of a two-frame game walk cycle. Preserve this exact red squirrel and the existing semi-realistic painted sprite style: strict orthographic top-down view, head pointing to the top edge, russet body, large fluffy C-shaped tail curving on the left side, exact identity, colors, canvas size, scale and centering. Create ONLY an alternate bounding/walking step: swap the forward/back positions of the forepaws and hind paws and slightly compress the torso for the second phase of a small squirrel hop; keep the head direction and distinctive left-side C tail unchanged. Exactly four legs/paws, no extra limbs. Keep the whole animal visible with the same margin. Replace the background with perfectly flat solid chroma green #00FF00, no texture, no shadow, no ground, no text, no border.
```

### Fox frame B

```text
Precise object edit for frame B of a two-frame game trot cycle. Preserve this exact red fox and the existing semi-realistic painted sprite style: strict orthographic top-down view, head pointing to the top edge, long narrow russet body and tail, black lower legs, white tail tip, exact identity, colors, canvas size, scale and centering. Create ONLY an alternate natural trot step: swap the forward/back diagonal leg pair so all four legs visibly change phase, while keeping the slim body, pointed fox face, long tail, black legs, white tail tip and travel direction unchanged. Exactly four legs/paws, no extra limbs. Keep the whole animal visible with the same margin. Replace the background with perfectly flat solid chroma green #00FF00, no texture, no shadow, no ground, no text, no border.
```
