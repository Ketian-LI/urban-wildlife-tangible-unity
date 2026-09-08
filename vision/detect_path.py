"""Segment a high-saturation physical path and output a denoised binary mask."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import cv2
import numpy as np

from detect_markers import load_camera_frame
from io_utils import read_image, write_image


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CONFIG = ROOT / "vision" / "config" / "path_detection.json"
DEFAULT_CAMERA_CONFIG = ROOT / "vision" / "config" / "calibration.json"


def hsv_mask(frame: np.ndarray, ranges: list[dict[str, list[int]]]) -> np.ndarray:
    """Combine one or more OpenCV HSV ranges into a binary mask."""
    hsv = cv2.cvtColor(frame, cv2.COLOR_BGR2HSV)
    combined = np.zeros(hsv.shape[:2], dtype=np.uint8)
    for colour_range in ranges:
        lower = np.asarray(colour_range["lower"], dtype=np.uint8)
        upper = np.asarray(colour_range["upper"], dtype=np.uint8)
        combined = cv2.bitwise_or(combined, cv2.inRange(hsv, lower, upper))
    return combined


def remove_small_components(mask: np.ndarray, minimum_area: int) -> tuple[np.ndarray, list[int]]:
    """Keep connected foreground regions at or above the configured area."""
    count, labels, stats, _ = cv2.connectedComponentsWithStats(mask, connectivity=8)
    cleaned = np.zeros_like(mask)
    kept_areas: list[int] = []
    for label in range(1, count):
        area = int(stats[label, cv2.CC_STAT_AREA])
        if area >= minimum_area:
            cleaned[labels == label] = 255
            kept_areas.append(area)
    kept_areas.sort(reverse=True)
    return cleaned, kept_areas


def denoise_mask(mask: np.ndarray, morphology: dict[str, int]) -> tuple[np.ndarray, list[int]]:
    open_size = int(morphology["open_kernel_px"])
    close_size = int(morphology["close_kernel_px"])
    iterations = int(morphology.get("iterations", 1))
    if open_size < 1 or close_size < 1:
        raise ValueError("Morphology kernel sizes must be positive")

    opened = cv2.morphologyEx(
        mask,
        cv2.MORPH_OPEN,
        cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (open_size, open_size)),
        iterations=iterations,
    )
    closed = cv2.morphologyEx(
        opened,
        cv2.MORPH_CLOSE,
        cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (close_size, close_size)),
        iterations=iterations,
    )
    return remove_small_components(closed, int(morphology["minimum_component_area_px"]))


def mask_bounds(mask: np.ndarray) -> dict[str, int] | None:
    points = cv2.findNonZero(mask)
    if points is None:
        return None
    x, y, width, height = cv2.boundingRect(points)
    return {"x": x, "y": y, "width": width, "height": height}


def draw_path_overlay(frame: np.ndarray, mask: np.ndarray, colour_bgr: list[int]) -> np.ndarray:
    overlay = frame.copy()
    colour_layer = np.zeros_like(frame)
    colour_layer[:] = tuple(int(value) for value in colour_bgr)
    blended = cv2.addWeighted(frame, 0.35, colour_layer, 0.65, 0.0)
    overlay[mask > 0] = blended[mask > 0]
    contours, _ = cv2.findContours(mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    cv2.drawContours(overlay, contours, -1, (255, 255, 255), 2, cv2.LINE_AA)
    return overlay


def detect_path(
    frame: np.ndarray,
    preset_name: str,
    config: dict[str, object],
) -> tuple[dict[str, object], np.ndarray, np.ndarray]:
    presets = config["presets"]
    if preset_name not in presets:
        available = ", ".join(sorted(presets))
        raise ValueError(f"Unknown colour preset '{preset_name}'. Available presets: {available}")

    preset = presets[preset_name]
    raw_mask = hsv_mask(frame, preset["hsv_ranges"])
    mask, component_areas = denoise_mask(raw_mask, config["morphology"])
    area = int(cv2.countNonZero(mask))
    height, width = mask.shape
    result: dict[str, object] = {
        "schema_version": "0.1",
        "preset": preset_name,
        "preset_label": preset["label"],
        "hsv_ranges": preset["hsv_ranges"],
        "image_size_px": {"width": width, "height": height},
        "path_detected": area > 0,
        "mask_area_px": area,
        "mask_fraction": area / float(width * height),
        "component_count": len(component_areas),
        "component_areas_px": component_areas,
        "bounds_px": mask_bounds(mask),
        "morphology": config["morphology"],
    }
    overlay = draw_path_overlay(frame, mask, preset["overlay_bgr"])
    return result, mask, overlay


def main() -> int:
    parser = argparse.ArgumentParser()
    source = parser.add_mutually_exclusive_group(required=True)
    source.add_argument("--image", type=Path)
    source.add_argument("--camera", type=int)
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    parser.add_argument("--camera-config", type=Path, default=DEFAULT_CAMERA_CONFIG)
    parser.add_argument("--preset")
    parser.add_argument("--output-dir", type=Path, default=ROOT / "data" / "raw" / "path-tests")
    args = parser.parse_args()

    config = json.loads(args.config.read_text(encoding="utf-8"))
    preset_name = args.preset or config["default_preset"]
    if args.image:
        frame = read_image(args.image)
    else:
        camera_config = json.loads(args.camera_config.read_text(encoding="utf-8"))
        video = camera_config["video"]
        frame = load_camera_frame(args.camera, video["width"], video["height"], video["fps"])

    try:
        result, mask, overlay = detect_path(frame, preset_name, config)
    except ValueError as error:
        print(json.dumps({"ok": False, "error": str(error)}, ensure_ascii=False, indent=2))
        return 2

    args.output_dir.mkdir(parents=True, exist_ok=True)
    write_image(args.output_dir / "path_mask.png", mask)
    write_image(args.output_dir / "path_overlay.png", overlay)
    (args.output_dir / "path_detection.json").write_text(
        json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(json.dumps({"ok": True, **result, "output_dir": str(args.output_dir)}, ensure_ascii=False, indent=2))
    return 0 if result["path_detected"] else 2


if __name__ == "__main__":
    raise SystemExit(main())
