# City board woodland opening V0.2

- Generation mode: built-in image generation tool
- Use case: `precise-object-edit`
- Edit target: `unity/Assets/Resources/UrbanWildlife/Environment/city-board-riverside-opening-v01.png`
- Integrated asset: `unity/Assets/Resources/UrbanWildlife/Environment/city-board-woodland-opening-v02.png`

## Edit 1 — remove river

> Use case: precise-object-edit. Asset type: production Unity map underlay for a top-down urban-wildlife city-building game. Image 1 is the exact edit target. Remove the entire blue river from top edge to bottom edge and remove every riverbank strip and bridge element. Replace the former river corridor seamlessly with the same warm ivory/off-white open ground used elsewhere on the map, with a few restrained pale-sage organic woodland-edge extensions only where needed to make the terrain transition natural. At the former bridge crossing, reconstruct the existing pale grey main road as one smooth, ordinary continuous road segment, matching its current width, curvature, dashed centre line, thin edging, lighting, and low-relief treatment on both sides. Preserve the exact 1536 × 1024 canvas, exact straight-down orthographic framing, every existing road outside the former bridge span, every tree and woodland cluster outside the former river/bank corridor, all warm-white clearings, palette, texture, scale, light direction, and soft low-relief city-builder style. Change only the river, its banks, and the bridge crossing. Keep water completely absent; the new ground must match the existing warm ivory terrain rather than becoming a differently coloured strip. Keep the road flat and understated. No water, river, stream, pond, bridge, canal, shoreline, blue terrain, buildings, people, animals, vehicles, UI, grid, labels, text, icons, logo, border, watermark, new roads, or new paths. Do not crop, zoom, tilt, redesign, or rearrange the map.

## Sampled reference palette

- Warm ivory ground: `#F0F0E8`; highlight `#F8F8F0`
- Pale woodland ground: `#C8E0B8`, `#C8E8B8`; transition `#D0E8B8`
- Medium tree crowns: `#408060`, `#589870`, `#60A078`
- Dark crown accents: `#387058`, `#306850`
- Road grey: `#E0E0E0`; highlight `#F0F0F0`

The palette was sampled from the playable map portion of the supplied 1536 × 1024
reference before the correction pass. The UI sidebar and blue water pixels were
excluded from the target groups.

## Edit 2 — sampled colour correction

> Use case: precise-object-edit. Asset type: production Unity map underlay for a top-down urban-wildlife city-building game. Image 1 is the exact no-river edit target. Image 2 is a color and rendering reference only, not a composition target. Correct only Image 1's color palette so its terrain, forest and road match Image 2's sampled hue, saturation and brightness. Remove the current yellow-green color cast and use the quieter, cooler sage palette from Image 2. Sampled target palette from Image 2 map area: warm ivory open ground dominant #F0F0E8 with highlight #F8F8F0; pale woodland ground #C8E0B8 and #C8E8B8 with transition #D0E8B8; medium tree crowns #408060, #589870 and #60A078; dark crown accents #387058 and #306850; road grey #E0E0E0 with highlight #F0F0F0. Change color grading only. Preserve Image 1's exact 1536 × 1024 canvas, straight-down orthographic framing, no-river/no-water state, continuous road geometry and dashed centre line, every warm-white clearing, every green land contour, every tree position, silhouette, scale and shadow, all paths, negative space, texture detail and lighting direction. Keep the former river corridor indistinguishable from the rest of the warm ivory ground. Do not add, remove, move, resize or redraw any object or terrain boundary. No water, river, stream, pond, bridge, canal, shoreline or blue terrain; no buildings, people, animals, vehicles, UI, grid, labels, text, icons, logo, border or watermark; no new roads or paths; no yellow-lime grass, neon green, high saturation, heavy contrast, dramatic shadows, plastic gloss, crop, zoom, tilt or layout change.
