# Unity Visual Validation

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
