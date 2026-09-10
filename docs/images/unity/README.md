# Unity Visual Validation

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
