"""Generate the selected A Food + B Woodland exact-scale design preview."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import cv2
import numpy as np


ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "vision"))

from contour_token_geometry import outline_points, woodland_felt_outline_points  # noqa: E402
from io_utils import write_image  # noqa: E402


DEFAULT_CONFIG = ROOT / "vision" / "config" / "contour_tokens_v0.2.json"
DEFAULT_OUTPUT = ROOT / "docs" / "images" / "tangible-prototype" / "token-shape-a-plus-b-v02.png"
DEFAULT_SVG_DIR = ROOT / "hardware" / "templates" / "contour-tokens-v0.2"


def leaf_points(centre: tuple[int, int], pixels_per_mm: float, width_mm: float, height_mm: float) -> np.ndarray:
    """Sample two quadratic Bezier curves forming a 120 x 90 mm leaf."""
    points = woodland_felt_outline_points(width_mm, height_mm, pixels_per_mm)
    points[:, 0] += centre[0]
    points[:, 1] += centre[1]
    return np.rint(points).astype(np.int32)


def write_leaf_svg(path: Path, config: dict[str, object]) -> None:
    felt = config["geometry"]["type_shapes"]["woodland"]["outer_felt"]
    width = float(felt["width_mm"])
    height = float(felt["height_mm"])
    middle_y = height / 2.0
    upper_control_x = width / 2.0 - width / 12.0
    lower_control_x = width / 2.0 + width / 12.0
    pad_size = float(felt["underside_magnetic_pads_mm"])
    pad_rectangles = []
    for x, y in felt["underside_magnetic_pad_centres_mm"]:
        pad_rectangles.append(
            f'    <rect x="{width / 2 + float(x) - pad_size / 2:.3f}" '
            f'y="{middle_y + float(y) - pad_size / 2:.3f}" width="{pad_size:.3f}" height="{pad_size:.3f}"/>'
        )
    content = f'''<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" width="{width}mm" height="{height}mm" viewBox="0 0 {width} {height}">
  <title>Woodland B leaf felt template</title>
  <desc>Green hairline is the felt cut edge. Grey dashed squares are underside magnetic-pad placement guides, not cuts.</desc>
  <g id="CUT_FELT" fill="none" stroke="#008000" stroke-width="0.2">
    <path d="M 0,{middle_y} Q {upper_control_x},-{middle_y} {width},{middle_y} Q {lower_control_x},{height + middle_y} 0,{middle_y} Z"/>
  </g>
  <g id="UNDERSIDE_GUIDES_DO_NOT_CUT" fill="none" stroke="#777777" stroke-width="0.2" stroke-dasharray="1,1">
{chr(10).join(pad_rectangles)}
  </g>
</svg>
'''
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


def draw_token(canvas: np.ndarray, logical_id: int, centre: tuple[int, int], config: dict[str, object], scale: float) -> None:
    code = config["codes"][str(logical_id)]
    if code["type"] == "woodland":
        felt = config["geometry"]["type_shapes"]["woodland"]["outer_felt"]
        leaf = leaf_points(centre, scale, float(felt["width_mm"]), float(felt["height_mm"]))
        cv2.fillPoly(canvas, [leaf], (72, 82, 55), cv2.LINE_AA)
        cv2.polylines(canvas, [leaf], True, (48, 57, 36), 3, cv2.LINE_AA)

    outline = outline_points(logical_id, config, pixels_per_mm=scale)
    outline[:, 0] += centre[0]
    outline[:, 1] += centre[1]
    polygon = np.rint(outline).astype(np.int32)
    cv2.fillPoly(canvas, [polygon], (199, 219, 232), cv2.LINE_AA)
    cv2.polylines(canvas, [polygon], True, (95, 125, 145), 3, cv2.LINE_AA)

    if code["type"] == "food_hotspot":
        radius = int(20 * scale)
        for ring in (0.45, 0.72):
            cv2.ellipse(canvas, centre, (int(radius * ring), int(radius * ring)), 0, 0, 360, (145, 171, 185), 2, cv2.LINE_AA)
        for angle in range(0, 360, 60):
            radians = np.deg2rad(angle)
            end = (round(centre[0] + radius * np.sin(radians)), round(centre[1] - radius * np.cos(radians)))
            cv2.line(canvas, centre, end, (145, 171, 185), 2, cv2.LINE_AA)
    else:
        cv2.line(canvas, (centre[0], centre[1] - int(14 * scale)), (centre[0], centre[1] + int(14 * scale)), (145, 171, 185), 2, cv2.LINE_AA)
        for offset in (-9, -3, 3, 9):
            y = centre[1] + int(offset * scale)
            dx = int((12 - abs(offset) * 0.4) * scale)
            cv2.line(canvas, (centre[0], y), (centre[0] - dx, y - int(7 * scale)), (145, 171, 185), 2, cv2.LINE_AA)
            cv2.line(canvas, (centre[0], y), (centre[0] + dx, y - int(7 * scale)), (145, 171, 185), 2, cv2.LINE_AA)


def create_preview(config: dict[str, object], output: Path) -> None:
    scale = 3.5
    canvas = np.full((1000, 1800, 3), (29, 29, 29), dtype=np.uint8)
    cv2.putText(canvas, "SELECTED TOKEN SHAPES V0.2", (90, 90), cv2.FONT_HERSHEY_SIMPLEX, 1.6, (235, 235, 235), 3, cv2.LINE_AA)
    cv2.putText(canvas, "A FOOD / B WOODLAND", (90, 145), cv2.FONT_HERSHEY_SIMPLEX, 1.0, (180, 190, 195), 2, cv2.LINE_AA)

    food_centres = [(300, 360), (700, 360), (1100, 360)]
    for logical_id, centre in zip((10, 11, 12), food_centres):
        draw_token(canvas, logical_id, centre, config, scale)
        cv2.putText(canvas, f"FOOD ID {logical_id}", (centre[0] - 110, 525), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (230, 230, 230), 2, cv2.LINE_AA)
    cv2.putText(canvas, "60 mm soft hexagon / plaza engraving", (1350, 350), cv2.FONT_HERSHEY_SIMPLEX, 0.62, (200, 200, 200), 2, cv2.LINE_AA)
    cv2.putText(canvas, "one direction notch + A/B code slots", (1350, 390), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (165, 175, 180), 1, cv2.LINE_AA)

    woodland_centres = [(470, 760), (1120, 760)]
    for logical_id, centre in zip((20, 21), woodland_centres):
        draw_token(canvas, logical_id, centre, config, scale)
        cv2.putText(canvas, f"WOODLAND ID {logical_id}", (centre[0] - 145, 940), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (230, 230, 230), 2, cv2.LINE_AA)
    cv2.putText(canvas, "120 x 90 mm leaf felt", (1400, 710), cv2.FONT_HERSHEY_SIMPLEX, 0.62, (200, 200, 200), 2, cv2.LINE_AA)
    cv2.putText(canvas, "50 mm circular recognition core", (1400, 750), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (165, 175, 180), 1, cv2.LINE_AA)
    cv2.putText(canvas, "Four 10 x 10 mm magnetic pads go underneath the felt.", (90, 985), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (160, 170, 175), 1, cv2.LINE_AA)
    write_image(output, canvas)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--svg-dir", type=Path, default=DEFAULT_SVG_DIR)
    args = parser.parse_args()
    config = json.loads(args.config.read_text(encoding="utf-8"))
    create_preview(config, args.output)
    felt_svg = args.svg_dir / "woodland-felt-leaf-120x90.svg"
    write_leaf_svg(felt_svg, config)
    print(json.dumps({"preview": str(args.output), "felt_svg": str(felt_svg)}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
