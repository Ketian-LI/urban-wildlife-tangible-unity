"""Detect sticker-free, contour-coded P0 tokens on a rectified black board."""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path

import cv2
import numpy as np

from io_utils import read_image, write_image


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CONFIG = ROOT / "vision" / "config" / "contour_tokens.json"


def segment_token_candidates(frame: np.ndarray, config: dict[str, object]) -> np.ndarray:
    gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY) if frame.ndim == 3 else frame
    threshold = int(config["segmentation"]["initial_grayscale_threshold"])
    _, mask = cv2.threshold(gray, threshold, 255, cv2.THRESH_BINARY)
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3))
    return cv2.morphologyEx(mask, cv2.MORPH_OPEN, kernel)


def radial_profile(
    mask: np.ndarray,
    centre: tuple[float, float],
    radius: float,
    angular_samples: int,
) -> np.ndarray:
    """Measure the foreground radius for angles where 0 degrees points upward."""
    radial_steps = max(20, int(math.ceil(radius * 1.2)))
    radii = np.linspace(0.25 * radius, 1.12 * radius, radial_steps)
    profile = np.zeros(angular_samples, dtype=np.float32)
    height, width = mask.shape
    for index in range(angular_samples):
        angle = math.radians(index * 360.0 / angular_samples)
        xs = np.rint(centre[0] + radii * math.sin(angle)).astype(np.int32)
        ys = np.rint(centre[1] - radii * math.cos(angle)).astype(np.int32)
        valid = (xs >= 0) & (xs < width) & (ys >= 0) & (ys < height)
        foreground = valid.copy()
        foreground[valid] = mask[ys[valid], xs[valid]] > 0
        if np.any(foreground):
            profile[index] = float(radii[np.flatnonzero(foreground)[-1]])
    return profile


def circular_smooth(values: np.ndarray, window: int) -> np.ndarray:
    if window <= 1:
        return values.copy()
    if window % 2 == 0:
        window += 1
    half = window // 2
    padded = np.concatenate([values[-half:], values, values[:half]])
    return np.convolve(padded, np.ones(window) / window, mode="valid").astype(np.float32)


def sample_circular_max(values: np.ndarray, centre_index: int, half_window: int) -> float:
    indices = (np.arange(centre_index - half_window, centre_index + half_window + 1) % len(values)).astype(int)
    return float(np.max(values[indices]))


def decode_profile(
    profile: np.ndarray,
    pixels_per_mm: float,
    config: dict[str, object],
) -> tuple[int, float, dict[str, float]] | None:
    detection = config["detection"]
    samples = len(profile)
    smooth_window = max(3, round(float(detection["smoothing_window_deg"]) / 360.0 * samples))
    smoothed = circular_smooth(profile, smooth_window)
    baseline = float(np.percentile(smoothed, 80))
    drops_mm = (baseline - smoothed) / pixels_per_mm
    orientation_index = int(np.argmax(drops_mm))
    orientation_depth = float(drops_mm[orientation_index])
    if orientation_depth < float(detection["orientation_min_depth_mm"]):
        return None

    geometry = config["geometry"]
    slot_half_window = max(2, round(float(detection["slot_window_deg"]) / 360.0 * samples / 2.0))
    slot_depths: dict[str, float] = {}
    value = 0
    for slot_name, slot in geometry["code_slots"].items():
        offset = round(float(slot["angle_clockwise_deg"]) / 360.0 * samples)
        depth = sample_circular_max(drops_mm, orientation_index + offset, slot_half_window)
        slot_depths[slot_name] = round(depth, 3)
        if depth >= float(detection["code_min_depth_mm"]):
            value += int(slot["weight"])

    angle = orientation_index * 360.0 / samples
    angle = (angle + 180.0) % 360.0 - 180.0
    return value, round(angle, 2), {"orientation": round(orientation_depth, 3), **slot_depths}


def code_lookup(config: dict[str, object]) -> dict[int, tuple[int, dict[str, object]]]:
    slots = config["geometry"]["code_slots"]
    lookup: dict[int, tuple[int, dict[str, object]]] = {}
    for logical_id_text, code in config["codes"].items():
        value = sum(int(slots[name]["weight"]) for name in code["slots"])
        lookup[value] = (int(logical_id_text), code)
    return lookup


def detect_contour_tokens(
    frame: np.ndarray,
    config: dict[str, object],
) -> tuple[list[dict[str, object]], np.ndarray, np.ndarray]:
    mask = segment_token_candidates(frame, config)
    contours, _ = cv2.findContours(mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_NONE)
    overlay = cv2.cvtColor(frame, cv2.COLOR_GRAY2BGR) if frame.ndim == 2 else frame.copy()
    pixels_per_mm = float(config["rectified_scale"]["pixels_per_mm"])
    detection_config = config["detection"]
    lookup = code_lookup(config)
    height, width = mask.shape
    detections: list[dict[str, object]] = []

    for contour in contours:
        area = float(cv2.contourArea(contour))
        perimeter = float(cv2.arcLength(contour, True))
        if area < float(detection_config["minimum_area_mm2"]) * pixels_per_mm**2 or perimeter <= 0:
            continue
        circularity = 4.0 * math.pi * area / (perimeter * perimeter)
        if circularity < float(detection_config["minimum_circularity"]):
            continue
        (centre_x, centre_y), radius = cv2.minEnclosingCircle(contour)
        diameter_mm = 2.0 * radius / pixels_per_mm
        possible_diameters = [float(code["diameter_mm"]) for code in config["codes"].values()]
        tolerance = float(config["geometry"]["diameter_tolerance_mm"])
        if min(abs(diameter_mm - expected) for expected in possible_diameters) > tolerance:
            continue

        profile = radial_profile(
            mask,
            (centre_x, centre_y),
            radius,
            int(detection_config["angular_samples"]),
        )
        decoded = decode_profile(profile, pixels_per_mm, config)
        if decoded is None:
            continue
        value, angle_deg, depths = decoded
        if value not in lookup:
            continue
        logical_id, code = lookup[value]
        if abs(diameter_mm - float(code["diameter_mm"])) > tolerance:
            continue

        x_norm = centre_x / (width - 1)
        y_norm = centre_y / (height - 1)
        confidence = min(1.0, max(0.0, depths["orientation"] / float(config["geometry"]["orientation_notch"]["depth_mm"])))
        item: dict[str, object] = {
            "id": logical_id,
            "type": code["type"],
            "center_px": {"x": round(centre_x, 2), "y": round(centre_y, 2)},
            "x_norm": round(x_norm, 6),
            "y_norm": round(y_norm, 6),
            "angle_deg": angle_deg,
            "diameter_mm": round(diameter_mm, 2),
            "code_value": value,
            "notch_depths_mm": depths,
            "confidence": round(confidence, 3),
        }
        detections.append(item)
        centre = (round(centre_x), round(centre_y))
        cv2.drawContours(overlay, [contour], -1, (0, 255, 0), 2, cv2.LINE_AA)
        cv2.circle(overlay, centre, 4, (0, 0, 255), -1, cv2.LINE_AA)
        angle_radians = math.radians(angle_deg)
        direction = (
            round(centre_x + radius * math.sin(angle_radians)),
            round(centre_y - radius * math.cos(angle_radians)),
        )
        cv2.line(overlay, centre, direction, (0, 0, 255), 2, cv2.LINE_AA)
        cv2.putText(
            overlay,
            f"ID {logical_id} {angle_deg:.1f} deg",
            (centre[0] + 10, centre[1] - 10),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.55,
            (0, 255, 0),
            2,
            cv2.LINE_AA,
        )

    detections.sort(key=lambda item: int(item["id"]))
    return detections, mask, overlay


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--image", type=Path, required=True, help="Rectified 900 x 600 board image")
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    parser.add_argument("--output-dir", type=Path, default=ROOT / "data" / "raw" / "contour-token-tests")
    args = parser.parse_args()
    config = json.loads(args.config.read_text(encoding="utf-8"))
    frame = read_image(args.image)
    detections, mask, overlay = detect_contour_tokens(frame, config)
    args.output_dir.mkdir(parents=True, exist_ok=True)
    write_image(args.output_dir / "candidate_mask.png", mask)
    write_image(args.output_dir / "contour_overlay.png", overlay)
    result = {"schema_version": "0.1", "count": len(detections), "detections": detections}
    (args.output_dir / "contour_detections.json").write_text(
        json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0 if detections else 2


if __name__ == "__main__":
    raise SystemExit(main())
