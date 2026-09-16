# Riverside UI layout v01

This pass aligns the playable Unity interface with the supplied Riverside Park references while preserving the current map simulation.

- The game area uses 79% of the screen and the right tool panel uses 21%.
- The title card sits at the upper left; city metrics sit in the upper centre.
- The title card also carries the current development stage, while the top metrics begin with year and season.
- City status remains at the lower left and the Build / Info / Trace / Pause navigation is centred along the bottom.
- The right panel opens on Buildings and keeps Animals and Tools as switchable tabs.
- Choosing Info changes the right panel to City Status, Animals and Events; the status values and event list come from the live simulation.
- Building and environment choices use the in-game sprites rather than text-only controls.
- The animal hotspot panel remains at the lower right, toggled with `H`, with a low-to-high colour legend.
- Trace mode is separate from that card: Human, Animal and Combined use a light full-map overlay with orange/red human paths and cyan/blue animal paths.
- Chinese and English modes share the same hierarchy and spacing.
- UI rectangles block map placement clicks so interface interaction cannot place buildings accidentally.
- During placement, the exact building footprint and a translucent building preview are shown together: green is valid and red is blocked. Hidden planning cells are no longer outlined, so the visible shape matches the continuous gameplay footprint rather than suggesting a larger grid-sized site.
- In desktop play, non-overlapping building footprints may share a hidden ecology cell; the cell is bookkeeping rather than extra collision space.
- Both green and red desktop previews can be repositioned immediately by clicking another map position; construction happens only after explicit confirmation. The camera workflow is unchanged in this pass.
- The cleared warm-white ground after confirmation follows the compact building footprint and narrow access route instead of clearing whole planning cells.
- Automatically generated building access uses the same warm-white kerb, pale-grey surface, centre dashes and width as the baked original streets; the pedestrian strip remains offset at the road edge.
- Building density comes from adding more compact buildings, not enlarging individual icons. Detached houses retain their established size; apartments, markets and community buildings use smaller type-specific display scales matched to the mature-city reference while their gameplay footprints remain unchanged.
- Camera-free desktop play can continue placing repeated building types up to a 72-building city; the physical-camera edition remains constrained to its real Token inventory.

`city-riverside-ui-layout-preview-v01.png` is a layout composite made from the verified Unity map capture and the verified UI layer capture. It documents the intended combined presentation; it is not presented as a single runtime screenshot.
