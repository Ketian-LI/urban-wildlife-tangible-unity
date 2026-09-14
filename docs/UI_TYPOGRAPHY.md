# City Prototype UI typography

The City Prototype uses [Nunito from the official Google Fonts repository](https://github.com/google/fonts/tree/main/ofl/nunito), a rounded sans-serif that matches the friendly city-builder art direction. The source variable font plus explicit static Regular (400) and Bold (700) instances are bundled with the SIL Open Font License in `Assets/Resources/UrbanWildlife/Fonts`, so builds do not depend on an operating-system font or an ambiguous variable-font default. Unity's `LegacyRuntime.ttf` remains a defensive fallback if a resource is missing.

## Type scale

| Role | Reference size at 1080p | Weight | Usage |
| --- | ---: | --- | --- |
| Display | 30 px | Bold | One screen title only |
| Metric | 16 px | Bold | Primary values and current workflow step |
| Body | 14 px | Regular | Explanations and map-key content |
| Section | 15 px | Bold | Short uppercase section labels |
| Caption | 12 px | Regular | Supporting context and provenance |
| Eyebrow | 11 px | Bold | District or organisation label above the title |
| Button | 14 px | Bold | Short action labels |

The scale responds to screen height between 85% and 125% of these reference sizes. Body text never drops below 10 px. Text hierarchy must not be communicated by colour alone: size or weight must also change.

## Spacing

All UI spacing uses a 4 px baseline: `4 / 8 / 12 / 20 / 28`. Related labels stay within 4–8 px; sections separate by 12–20 px; the screen title receives 28 px of space from unrelated content.

## Colour

| Token | Hex | Use |
| --- | --- | --- |
| Panel | `#F8F6EF` | Warm neutral background |
| Ink | `#20343F` | Titles and high-priority values |
| Secondary ink | `#506066` | Body text |
| Muted ink | `#5D6A6D` | Captions |
| Accent | `#22696D` | Section labels and active emphasis |
| Button | `#DCECEE` | Default button surface |
| Button hover | `#C7E1E4` | Pointer feedback |
| Button pressed | `#AFD3D7` | Pressed feedback |

The dark ink and restrained teal replace the previous bright cyan. This keeps the sidebar in the same soft city-builder visual family as the map while retaining clear contrast.

## Writing rules

- Use sentence case for titles and actions; reserve uppercase for short section labels.
- Prefer nouns plus values (`Active residents  8/12`) over full sentences in metric rows.
- Keep buttons to one line and ideally below 28 characters.
- Do not add typefaces or decorative text effects without a clear information purpose.
- Keep body copy left-aligned. Centre alignment is reserved for compact map labels.
