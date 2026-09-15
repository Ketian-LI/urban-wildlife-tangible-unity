# Commercial and community building visual set v03

## Current Unity reference-scaled set (2026-09-15)

The active prototype now uses one tightly matched building-only sprite for each
public destination category:

- `commercial-shop-reference-soft-v04.png`: compact neighbourhood shop with a
  restrained ochre awning and no embedded site tile.
- `community-centre-reference-soft-v03.png`: low, wide civic building with a
  muted sage roof and no embedded site tile.

Both use the same camera, palette, low-detail geometry and transparent ground as
the active detached and apartment sprites. The six v02/v03 variants documented
below remain in the repository for comparison, but are no longer mixed into the
runtime because their stronger volume conflicted with the approved map style.

These assets were generated with the built-in ImageGen tool and are wired into
the Unity city prototype. Six new production files use true alpha and the
brighter, softer Riverside Park palette. Earlier detailed v01/v02 files remain
available for visual comparison but are no longer selected by the prototype.

## Commercial variants

- `commercial-cafe-citybuilder-soft-v03.png`: compact one-storey corner café.
- `commercial-shop-row-citybuilder-soft-v03.png`: two connected neighbourhood
  shopfronts.
- `commercial-market-hall-citybuilder-soft-v03.png`: small covered market hall.

## Community variants

- `community-library-citybuilder-soft-v02.png`: neighbourhood library and
  reading room.
- `community-clinic-citybuilder-soft-v02.png`: compact local health clinic.
- `community-hall-citybuilder-soft-v02.png`: small community activity hall.

## Shared production prompt

Use case: `stylized-concept`. Asset type: transparent Unity city-map building
sprite. Match the Riverside Park reference palette and the selected residential
A/B/C camera: high oblique near-top-down view from the upper-right, high
brightness, restrained saturation, shallow volume, simple geometry and clear
small-map silhouettes. Isolate one building on genuine transparency with no
ground tile, landscaping, people, vehicles, text, logos or watermarks.
