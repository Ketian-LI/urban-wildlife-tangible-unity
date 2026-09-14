# Commercial building visual set v01

These assets were generated with the built-in ImageGen tool and are wired into
the Unity city prototype. The production market and shops use the corrected v02
files with true alpha; their v01 review files are retained for comparison.

## Market hall

`market-hall-citybuilder-v02.png` is a compact red-brick community market with
dark green shopfront framing, a cream-and-ochre awning and produce crates. It is
used by the `market-hall` simulation building.

## Corner shops

`corner-shops-citybuilder-v02.png` is a connected pair of London-stock-brick
neighbourhood shops with distinct blue and muted-rust storefronts. It is used by
the `corner-shops` simulation building.

## Community centre

`community-centre-citybuilder-v01.png` is a balanced pale-brick civic hall with
dark teal doors, a slate roof lantern and an icon-only civic emblem. It replaces
the final coloured prototype block used by the `community-centre` building.

## Shared production prompt

Use case: `stylized-concept`. Asset type: transparent Unity city-map building
sprite. Match the selected residential A/B/C set in three-quarter bird's-eye
camera, soft upper-left light, matte low-saturation materials, simplified
city-builder geometry and small-map readability. Avoid text, logos, people,
vehicles, streets, landscaping and watermarks.

The production files were generated on a uniform `#FF00FF` source plate and
converted to real transparency with
`tools/chroma_key_sprite.py --key-color magenta`.
