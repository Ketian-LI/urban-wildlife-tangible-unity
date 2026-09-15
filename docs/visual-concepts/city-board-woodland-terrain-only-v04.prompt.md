# City board woodland terrain-only V0.4

## Mode

Two-pass precise object edit. The existing no-river opening underlay was the first edit target; the user-confirmed dense Riverside Park image was used only as the visual-direction and target-density reference. A second precision cleanup removed residual tile seams without changing composition.

## Final prompt set

Pass 1: Edit the existing 1536 × 1024 no-river opening map into a reusable terrain-only orthographic city-building underlay. Remove the complete pale-grey vehicle road, its kerbs, shadows and dashed centre line, and remove every narrow white or cream woodland footpath. Heal road areas on open land with continuous warm ivory ground around `#F0F0E8`/`#F8F8F0`; heal woodland paths with the surrounding pale sage fills around `#C8E0B8`, `#C8E8B8` and restrained `#D0E8B8` transitions. Preserve the exact camera, crop, tree positions, tree count, five-shape tree family, relative scale, spacing, woodland silhouettes, open-ground silhouettes, muted palette and low-relief soft city-builder rendering. Do not add water, river, bridge, buildings, vehicles, people, animals, labels, UI, grid lines, markers or plot outlines. The result must contain only warm-white buildable land, woodland masses and trees so Unity can draw every road, pedestrian path and building dynamically above it.

Pass 2: Remove every faint straight vertical and horizontal grid seam or tile boundary from the terrain-only result. Heal each seam using the immediately adjacent local color and soft texture. Preserve every tree, woodland outline, open-land outline, color and shadow exactly; add no objects or markings.

## Output

`unity/Assets/Resources/UrbanWildlife/Environment/city-board-woodland-terrain-only-v04.png`
