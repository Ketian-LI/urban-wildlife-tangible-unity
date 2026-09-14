# Residential buildings city-builder kit v01

Built-in ImageGen was used with the user's two architectural references and the
approved soft city-builder visual reference. The generated assets were isolated
on a solid magenta plate, then converted to transparent RGBA sprites with
`tools/chroma_key_sprite.py --key-color magenta`.

## Detached house

- Source reference: British red-brick urban house with bay-window detailing.
- One narrow two-storey building, never a terrace row.
- Warm red brick, cream trim, slate-blue pitched roof, short chimney and dark door.
- Three-quarter bird's-eye view with soft upper-left daylight.
- Rounded, readable city-builder geometry with restrained detail.
- Solid `#FF00FF` isolation plate; no street, people, vegetation, label or watermark.

Final asset:
`unity/Assets/Resources/UrbanWildlife/Buildings/house-detached-citybuilder-v01.png`

## Low-rise apartment

- Source reference: contemporary three-storey British red-brick and cream apartment.
- One compact linked building with flat roofs, recessed balconies and clear entrance.
- Matches the detached house's angle, lighting, edge softness and saturation.
- Solid `#FF00FF` isolation plate; no street, people, vegetation, label or watermark.

Final asset:
`unity/Assets/Resources/UrbanWildlife/Buildings/apartment-lowrise-citybuilder-v01.png`
