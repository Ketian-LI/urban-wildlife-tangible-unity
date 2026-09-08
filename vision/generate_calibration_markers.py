"""Generate four exact-size ArUco corner cards and an A4 print sheet."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import cv2
import numpy as np
from reportlab.lib.pagesizes import A4
from reportlab.lib.units import mm
from reportlab.pdfgen.canvas import Canvas

from io_utils import write_image


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CONFIG = ROOT / "vision" / "config" / "calibration.json"
DEFAULT_OUTPUT_DIR = ROOT / "hardware" / "printables" / "calibration-markers"
DEFAULT_PDF = ROOT / "hardware" / "printables" / "aruco-four-corner-calibration-a4.pdf"


def millimetres_to_pixels(value_mm: float, dpi: int) -> int:
    return round(value_mm / 25.4 * dpi)


def build_card(marker_id: int, dictionary_name: str, card_mm: int, marker_mm: int, dpi: int) -> np.ndarray:
    dictionary_id = getattr(cv2.aruco, dictionary_name)
    dictionary = cv2.aruco.getPredefinedDictionary(dictionary_id)
    marker_px = millimetres_to_pixels(marker_mm, dpi)
    card_px = millimetres_to_pixels(card_mm, dpi)
    marker = cv2.aruco.generateImageMarker(dictionary, marker_id, marker_px)
    card = np.full((card_px, card_px), 255, dtype=np.uint8)
    offset = (card_px - marker_px) // 2
    card[offset : offset + marker_px, offset : offset + marker_px] = marker
    return card


def draw_crop_marks(canvas: Canvas, x: float, y: float, size: float) -> None:
    length = 4 * mm
    gap = 1.5 * mm
    canvas.setLineWidth(0.35)
    canvas.setStrokeColorRGB(0.35, 0.35, 0.35)
    for sx in (x, x + size):
        direction = -1 if sx == x else 1
        canvas.line(sx + direction * gap, y, sx + direction * (gap + length), y)
        canvas.line(sx + direction * gap, y + size, sx + direction * (gap + length), y + size)
    for sy in (y, y + size):
        direction = -1 if sy == y else 1
        canvas.line(x, sy + direction * gap, x, sy + direction * (gap + length))
        canvas.line(x + size, sy + direction * gap, x + size, sy + direction * (gap + length))


def create_pdf(pdf_path: Path, cards: list[tuple[str, Path]], config: dict[str, object]) -> None:
    page_width, page_height = A4
    card_size = float(config["calibration_card_size_mm"]) * mm
    marker_size = float(config["calibration_marker_size_mm"]) * mm
    left = 25 * mm
    horizontal_gap = 20 * mm
    top_y = page_height - 58 * mm - card_size
    bottom_y = top_y - card_size - 30 * mm
    positions = [
        (left, top_y),
        (left + card_size + horizontal_gap, top_y),
        (left, bottom_y),
        (left + card_size + horizontal_gap, bottom_y),
    ]

    canvas = Canvas(str(pdf_path), pagesize=A4, pageCompression=1)
    canvas.setTitle("ArUco Four-Corner Calibration Markers - A4")
    canvas.setAuthor("Urban Wildlife Tangible Simulation")
    canvas.setFont("Helvetica-Bold", 17)
    canvas.drawCentredString(page_width / 2, page_height - 22 * mm, "Four-Corner ArUco Calibration Markers")
    canvas.setFont("Helvetica", 9)
    canvas.drawCentredString(page_width / 2, page_height - 29 * mm, "Print on A4 at 100% / Actual size. Disable Fit to page and duplex printing.")

    for (label, image_path), (x, y) in zip(cards, positions, strict=True):
        canvas.drawImage(str(image_path), x, y, width=card_size, height=card_size, preserveAspectRatio=True, mask="auto")
        draw_crop_marks(canvas, x, y, card_size)
        canvas.setFont("Helvetica-Bold", 9)
        canvas.drawCentredString(x + card_size / 2, y - 6 * mm, label)

    line_x = (page_width - marker_size) / 2
    line_y = 20 * mm
    canvas.setLineWidth(0.6)
    canvas.line(line_x, line_y, line_x + marker_size, line_y)
    canvas.line(line_x, line_y - 2 * mm, line_x, line_y + 2 * mm)
    canvas.line(line_x + marker_size, line_y - 2 * mm, line_x + marker_size, line_y + 2 * mm)
    canvas.setFont("Helvetica", 8)
    canvas.drawCentredString(page_width / 2, line_y + 4 * mm, "Print check: this line must measure exactly 50 mm")
    canvas.drawCentredString(page_width / 2, 10 * mm, "DICT_4X4_50 | card 70 x 70 mm | encoded square 50 x 50 mm")
    canvas.showPage()
    canvas.save()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT_DIR)
    parser.add_argument("--pdf", type=Path, default=DEFAULT_PDF)
    args = parser.parse_args()

    config = json.loads(args.config.read_text(encoding="utf-8"))
    args.output_dir.mkdir(parents=True, exist_ok=True)
    args.pdf.parent.mkdir(parents=True, exist_ok=True)
    card_size = int(config["calibration_card_size_mm"])
    marker_size = int(config["calibration_marker_size_mm"])
    dpi = int(config["print_dpi"])

    order = [
        ("TOP LEFT - ID 0", config["corner_ids"]["top_left"]),
        ("TOP RIGHT - ID 1", config["corner_ids"]["top_right"]),
        ("BOTTOM LEFT - ID 3", config["corner_ids"]["bottom_left"]),
        ("BOTTOM RIGHT - ID 2", config["corner_ids"]["bottom_right"]),
    ]
    cards: list[tuple[str, Path]] = []
    for label, marker_id in order:
        card = build_card(marker_id, config["aruco_dictionary"], card_size, marker_size, dpi)
        image_path = args.output_dir / f"aruco_4x4_50_id_{marker_id:02d}_card_70mm.png"
        write_image(image_path, card)
        cards.append((label, image_path))

    create_pdf(args.pdf, cards, config)
    print(json.dumps({"pdf": str(args.pdf), "cards": [str(path) for _, path in cards]}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
