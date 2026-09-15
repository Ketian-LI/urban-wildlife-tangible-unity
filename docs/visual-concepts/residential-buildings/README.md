# Approved residential visual direction

## Current Unity reference-scaled set (2026-09-15)

The runtime now uses transparent building-only sprites calibrated against the
approved 1536 x 1024 Riverside Park map reference:

- `detached-house-reference-blue-v06.png`
- `detached-house-reference-coral-v06.png`
- `apartment-reference-soft-v03.png`

The sprites share a near-top-down three-quarter camera, high-value restrained
palette, simplified windows and doors, matte shading and a short grounding
shadow. They intentionally contain no lawn tile, trees, bushes, roads or text;
Unity paints the ground and woodland clearing from the authoritative building
footprint so the surrounding colour always matches the map.

The older landscaped A/B/C set documented below remains available as a visual
history, but is no longer selected by `CityPrototypeDemo`.

`residential-abc-approved-v11.png` is the approved A/B/C residential concept sheet.

The Unity-ready sprites in `approved-assets/` are complete landscaped lots, not
bare buildings. Each sprite intentionally retains its rounded lawn base, cream
entrance path, trees, and shrubs. Only the presentation background and A/B/C
sheet labels are removed.

- A: simplified detached orange-brick house with a blue roof and red chimney.
- B: simplified paired residential block with a blue roof and two red chimneys.
- C: simplified low-rise apartment block in the approved muted sand-yellow tone.

All three sprites use a transparent RGBA background and the same clean pastel,
slightly top-down city-builder visual language as the approved reference.
