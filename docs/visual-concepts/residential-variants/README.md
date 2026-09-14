# Residential building review set v02

These four candidates were generated with built-in ImageGen and exported with
transparent RGBA backgrounds. Candidates A, B and C are the selected production
set and are now wired into the Unity prototype as three distinct residences.

## Reference decisions

- Keep a recognisable London street character instead of generic suburban forms.
- Use brick, bay windows, door surrounds and slate roofs for individual houses.
- Use compact low-rise massing, visible shared entrances, corner windows and
  recessed balconies/loggias for apartments.
- Keep one consistent three-quarter bird's-eye view, upper-left diffuse light,
  restrained saturation and readable silhouettes at map scale.

External design references:

- [London Housing Design](https://www.london.gov.uk/publications/housing-design)
- [Housing Design Standards London Plan Guidance](https://www.london.gov.uk/programmes-strategies/planning/implementing-london-plan/london-plan-guidance/housing-design-standards-lpg)

## Candidates

### A — red-brick bay house

Prompt focus: one compact two-storey London red-brick house with a faceted bay
window, cream stone trim, slate hipped roof, short chimney, dark door and a small
recessed porch.

### B — London-stock-brick house

Prompt focus: one calmer individual house with pale honey stock brick, shallow
curved bay, green door, charcoal pitched roof, chimney and cream window reveals.

### C — three-storey corner apartment

Prompt focus: one compact three-storey apartment with a warm red-brick wing,
pale rendered wing, flat roofs, clear entrance, tall windows, a dual-aspect
corner and two restrained recessed balconies.

### D — four-storey loggia apartment

Prompt focus: one connected shallow L-shaped apartment with buff and muted red
brick, flat parapets, a shared entrance, paired vertical windows and deep
loggia-style balconies.

## Selected Unity application

- Detached house: A, red-brick bay house.
- Lower-rise residence: B, London-stock-brick house.
- Apartment: C, three-storey corner apartment.
- The three silhouettes use independent map scales so they remain readable
  without covering neighbouring cells.

## Shared production constraints

Polished soft 2.5D city-builder illustration; one centered building; consistent
three-quarter bird's-eye angle; roof, front and one side visible; low-saturation
matte materials; rounded simplified geometry; subtle ambient occlusion; small
grounding shadow; no street, landscaping, people, cars, labels or watermark.
The source was generated on a uniform `#FF00FF` plate and converted to real alpha
with `tools/chroma_key_sprite.py --key-color magenta`.
