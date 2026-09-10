# Unity Visual Validation

## `visual-layer-v11-unified-character-palette-preview.png`

Rendered on 2026-09-10 in Unity 6000.3.4f1 using the new deterministic park-palette Sprite shader. Humans use 0.72 source saturation; pigeon, squirrel, and fox use 0.78, 0.68, and 0.66 respectively. All characters receive a small brightness reduction and a restrained yellow-green ambient tint so that they sit inside the painted park palette rather than reading as a separate high-saturation layer.

Feeding, avoidance, dwelling, visiting, and retreat state tints were softened at the same time. The grading is presentation-only and does not alter source PNG files, transparent edges, recognition data, agent scale, routes, state-machine decisions, or research logs.

## `visual-layer-v10-web-motion-side-animals-preview.png`

Rendered on 2026-09-10 in Unity 6000.3.4f1 after comparing the prototype with the earlier web simulation. Humans now stay screen-facing and change direction through horizontal mirroring instead of rotating around every path bend. Their walk cycle is slower, with much smaller sway, scale pulse, and step lift. A completed trip now hides at the exit during its entry delay instead of visibly teleporting across the park.

Pigeon, squirrel, and fox now use dedicated two-frame right-facing side-profile sprites and mirror horizontally when their lateral direction changes. Their presentation lengths increased from 0.18/0.24/0.55 to 0.30/0.44/0.72 Unity units so that all three species remain readable on the complete board while preserving the pigeon < squirrel < fox < human ordering. This static preview validates presentation and scale; runtime smoke checks separately validate both animation frames, screen-facing motion, and horizontal flipping.

## `visual-layer-v09-asphalt-road-preview.png`

Rendered on 2026-09-10 in Unity 6000.3.4f1. The confirmed route is now presented as a charcoal-grey asphalt park road with pale concrete kerbs and subtle deterministic fine-aggregate flecks. It intentionally has no centre line because it represents a pedestrian park access route rather than a city vehicle road.

The physical input remains a high-saturation magenta ribbon for reliable HSV segmentation. This is a presentation-only change: route coordinates, width, continuity checks, human routing, and gameplay constraints are unchanged.

## `visual-layer-v08-fence-gates-preview.png`

Rendered on 2026-09-10 in Unity 6000.3.4f1. Entrance A and Exit B now use a strict top-down wrought-iron fence-gate sprite with short boundary fence sections, stone posts, two open gate leaves, and a gravel threshold. Exit B mirrors the source artwork so both gates open toward the park interior. Small A/B badges preserve the research landmarks.

The replacement is visual only. Entrance/exit coordinates and all path-continuity checks are unchanged.

## `visual-layer-v07-planning-areas-preview.png`

Rendered on 2026-09-10 in Unity 6000.3.4f1 from the active presentation layer. Woodland IDs 20 and 21 now appear as movable forest groves. Food Hotspot IDs 10 and 12 appear as bench rest areas, while ID 11 appears as a small paved plaza. The Food Hotspot meaning is a human-activity/crumb opportunity rather than literal food.

Small edge badges preserve token IDs for research observation. Recognition data, token types, coordinates, angles, constraint checks, human routes, and animal state machines are unchanged.

## `visual-layer-v06-realistic-road-preview.png`

Rendered on 2026-09-09 in Unity 6000.3.4f1 from the active display layer. The confirmed path Polyline is rendered as a warm-grey gravel park path with dark soil shoulders, pale stone edging, and deterministic light/dark gravel details instead of a solid magenta ribbon.

The physical input remains a high-saturation magenta ribbon for reliable HSV segmentation. This preview changes only the Unity presentation; path coordinates, continuity checks, Food distance rules, and human routing still consume the original confirmed Polyline.

## `visual-layer-v05-realistic-scale-preview.png`

Rendered on 2026-09-09 in Unity 6000.3.4f1 from the active realistic-proportion human set and real-size relative Sprite scale. Human body proportions are less stylized, and pigeon, squirrel, and fox are no longer enlarged to near-human length.

The approximate display lengths are Walker 0.89, Dweller 0.84, Visitor 0.85, pigeon 0.18, squirrel 0.24, and fox 0.55 Unity units. These values affect visual size only. Characters are staged at fixed positions for this static review; the image does not prove animation timing or physical-camera input.

## `visual-layer-v04-front-facing-humans-preview.png`

Rendered on 2026-09-09 in Unity 6000.3.4f1 from the active front-facing human Sprite set. Walker, Dweller, and Visitor now share one high-angle three-quarter map view with readable facial features while retaining their distinct clothing and accessories.

The human route, heading convention, state machines, planning geometry, and animal behavior are unchanged. Humans and animals are staged at fixed positions for this static visual review; the image does not prove animation timing or physical-camera input.

## `visual-layer-v03-refinement-preview.png`

Rendered on 2026-09-09 in Unity 6000.3.4f1 from the active V0.3 visual layer. The preview verifies the final heel direction on Walker and Visitor, layered Food Hotspot markers, felt/canopy/wood-ring Woodland markers, two-post park gates, and strongly separated squirrel/fox silhouettes.

The planning geometry and token footprint rules are unchanged. Humans and animals are staged at fixed positions for this static visual review; the image does not prove animation timing or physical-camera input.

## `visual-layer-v021-shoe-fix-preview.png`

Rendered on 2026-09-09 in Unity 6000.3.4f1 with the corrected Walker and Visitor V0.2 sprites. Both characters now have two shoes connected to their legs below the hips; the extra shoe above each head has been removed. The older V0.2 preview remains below for comparison.

This is a static visual-composition check using the project assets and runtime layout renderer. Character positions are staged for inspection, so this image does not verify walking animation or physical-camera input. The capture requires a graphics device; Unity's `-nographics` mode cannot render this scene correctly.

## `visual-layer-v02-preview.png`

Generated on 2026-09-09 by the Unity 6000.3.4f1 editor from the actual V0.2 runtime display components and committed project assets.

The preview verifies the combined visual hierarchy of:

- the 3:2 hand-painted S001 background;
- exact Entrance A, Exit B, plaza, and pond guides;
- the outlined planned path;
- three Food and two Woodland planning markers;
- three human role Sprites;
- pigeon, squirrel, and fox Sprites.

The image is a software visual-composition check. It is not evidence that the physical camera, contour recognition, or final interaction setup has passed.
