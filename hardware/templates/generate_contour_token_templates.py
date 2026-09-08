"""Generate five exact-size SVG cut outlines and one A4 print-check PDF."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from reportlab.lib.pagesizes import A4
from reportlab.lib.units import mm
from reportlab.pdfgen.canvas import Canvas


ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "vision"))

from contour_token_geometry import outline_points  # noqa: E402


DEFAULT_CONFIG = ROOT / "vision" / "config" / "contour_tokens.json"
DEFAULT_SVG_DIR = ROOT / "hardware" / "templates" / "contour-tokens"
DEFAULT_PDF = ROOT / "output" / "pdf" / "contour-token-cut-templates-a4.pdf"


def svg_path(points, offset: float) -> str:
    commands = [f"M {points[0, 0] + offset:.3f},{points[0, 1] + offset:.3f}"]
    commands.extend(f"L {x + offset:.3f},{y + offset:.3f}" for x, y in points[1:])
    commands.append("Z")
    return " ".join(commands)


def write_svg(path: Path, logical_id: int, config: dict[str, object]) -> None:
    code = config["codes"][str(logical_id)]
    diameter = float(code["diameter_mm"])
    margin = 5.0
    size = diameter + 2.0 * margin
    points = outline_points(logical_id, config, pixels_per_mm=1.0)
    content = f'''<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" width="{size}mm" height="{size}mm" viewBox="0 0 {size} {size}">
  <title>Contour token ID {logical_id} - {code["type"]}</title>
  <desc>Exact-size cut outline in millimetres. Red hairline is the only cut path.</desc>
  <g id="CUT" fill="none" stroke="#ff0000" stroke-width="0.2">
    <path d="{svg_path(points, size / 2.0)}"/>
  </g>
</svg>
'''
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


def draw_outline(canvas: Canvas, logical_id: int, centre_x: float, centre_y: float, config: dict[str, object]) -> None:
    points = outline_points(logical_id, config, pixels_per_mm=mm)
    path = canvas.beginPath()
    path.moveTo(centre_x + float(points[0, 0]), centre_y - float(points[0, 1]))
    for x, y in points[1:]:
        path.lineTo(centre_x + float(x), centre_y - float(y))
    path.close()
    canvas.setLineWidth(0.35)
    canvas.setStrokeColorRGB(0.0, 0.0, 0.0)
    canvas.setFillColorRGB(1.0, 1.0, 1.0)
    canvas.drawPath(path, stroke=1, fill=0)
    canvas.setFillColorRGB(0.0, 0.0, 0.0)
    canvas.setFont("Helvetica-Bold", 9)
    canvas.drawCentredString(centre_x, centre_y - float(config["codes"][str(logical_id)]["diameter_mm"]) / 2 * mm - 5 * mm, f"ID {logical_id}")


def create_pdf(path: Path, config: dict[str, object]) -> None:
    page_width, page_height = A4
    path.parent.mkdir(parents=True, exist_ok=True)
    canvas = Canvas(str(path), pagesize=A4, pageCompression=1)
    canvas.setTitle("Contour-Coded Token Cut Templates - A4")
    canvas.setAuthor("Urban Wildlife Tangible Simulation")
    canvas.setFont("Helvetica-Bold", 16)
    canvas.drawCentredString(page_width / 2, page_height - 15 * mm, "Contour-Coded Token Templates")
    canvas.setFont("Helvetica", 8.5)
    canvas.drawCentredString(page_width / 2, page_height - 21 * mm, "Print A4 at 100% / Actual size. Disable Fit to page.")
    canvas.drawCentredString(page_width / 2, page_height - 26 * mm, "Solid outline = cut edge. Large top notch = 0 degree direction.")

    placements = {
        10: (52 * mm, 230 * mm),
        11: (145 * mm, 230 * mm),
        12: (52 * mm, 150 * mm),
        20: (145 * mm, 150 * mm),
        21: (52 * mm, 78 * mm),
    }
    for logical_id, (centre_x, centre_y) in placements.items():
        draw_outline(canvas, logical_id, centre_x, centre_y, config)

    canvas.setFillColorRGB(0.0, 0.0, 0.0)
    canvas.setFont("Helvetica-Bold", 8.5)
    canvas.drawString(95 * mm, 100 * mm, "Code slots clockwise from direction notch:")
    canvas.setFont("Helvetica", 8)
    canvas.drawString(95 * mm, 94 * mm, "A = 90 deg / value 1")
    canvas.drawString(95 * mm, 89 * mm, "B = 180 deg / value 2")
    canvas.drawString(95 * mm, 84 * mm, "C = 270 deg / value 4")
    canvas.drawString(95 * mm, 76 * mm, "Food: 10=A, 11=B, 12=A+B")
    canvas.drawString(95 * mm, 71 * mm, "Woodland: 20=C, 21=A+C")
    canvas.drawString(95 * mm, 63 * mm, "Orientation notch: 10 x 7 mm")
    canvas.drawString(95 * mm, 58 * mm, "Code notch: 8 x 5 mm")

    start_x = 55 * mm
    end_x = 155 * mm
    line_y = 27 * mm
    canvas.setLineWidth(0.6)
    canvas.line(start_x, line_y, end_x, line_y)
    canvas.line(start_x, line_y - 2 * mm, start_x, line_y + 2 * mm)
    canvas.line(end_x, line_y - 2 * mm, end_x, line_y + 2 * mm)
    canvas.setFont("Helvetica", 8)
    canvas.drawCentredString(page_width / 2, line_y + 4 * mm, "Print check: this line must measure exactly 100 mm")
    canvas.drawCentredString(page_width / 2, 12 * mm, "Prototype before cutting wood. Dimensions require physical validation.")
    canvas.showPage()
    canvas.save()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    parser.add_argument("--svg-dir", type=Path, default=DEFAULT_SVG_DIR)
    parser.add_argument("--pdf", type=Path, default=DEFAULT_PDF)
    parser.add_argument("--skip-pdf", action="store_true", help="Generate only SVG cut outlines")
    args = parser.parse_args()
    config = json.loads(args.config.read_text(encoding="utf-8"))
    svg_paths = []
    for logical_id in sorted(int(value) for value in config["codes"]):
        path = args.svg_dir / f"contour-token-id-{logical_id}.svg"
        write_svg(path, logical_id, config)
        svg_paths.append(str(path))
    pdf_path = None
    if not args.skip_pdf:
        create_pdf(args.pdf, config)
        pdf_path = str(args.pdf)
    print(json.dumps({"svg": svg_paths, "pdf": pdf_path}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
