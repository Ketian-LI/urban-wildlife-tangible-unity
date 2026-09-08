"""Shared geometry for contour-coded token templates and synthetic tests."""

from __future__ import annotations

import math

import cv2
import numpy as np


def code_value(code: dict[str, object], config: dict[str, object]) -> int:
    slots = config["geometry"]["code_slots"]
    return sum(int(slots[name]["weight"]) for name in code["slots"])


def outline_points(
    logical_id: int,
    config: dict[str, object],
    pixels_per_mm: float = 1.0,
    rotation_clockwise_deg: float = 0.0,
    samples: int = 720,
) -> np.ndarray:
    """Return a clockwise polygon with one orientation notch and encoded radial notches."""
    code = config["codes"][str(logical_id)]
    geometry = config["geometry"]
    radius_mm = float(code["diameter_mm"]) / 2.0
    notches = [
        (
            float(geometry["orientation_notch"]["reference_angle_clockwise_deg"]),
            float(geometry["orientation_notch"]["width_mm"]),
            float(geometry["orientation_notch"]["depth_mm"]),
        )
    ]
    for slot_name in code["slots"]:
        slot = geometry["code_slots"][slot_name]
        notches.append(
            (
                float(slot["angle_clockwise_deg"]),
                float(geometry["code_notch"]["width_mm"]),
                float(geometry["code_notch"]["depth_mm"]),
            )
        )

    points: list[list[float]] = []
    for index in range(samples):
        local_angle = index * 360.0 / samples
        radius = radius_mm
        for centre_angle, width_mm, depth_mm in notches:
            half_angle = math.degrees(math.asin(min(0.99, width_mm / (2.0 * radius_mm))))
            difference = (local_angle - centre_angle + 180.0) % 360.0 - 180.0
            if abs(difference) <= half_angle:
                radius = min(radius, radius_mm - depth_mm)
        angle = math.radians(local_angle + rotation_clockwise_deg)
        x = radius * math.sin(angle) * pixels_per_mm
        y = -radius * math.cos(angle) * pixels_per_mm
        points.append([x, y])
    return np.asarray(points, dtype=np.float32)


def render_token(
    logical_id: int,
    config: dict[str, object],
    pixels_per_mm: float = 2.0,
    rotation_clockwise_deg: float = 0.0,
    margin_mm: float = 6.0,
) -> tuple[np.ndarray, tuple[float, float]]:
    diameter_mm = float(config["codes"][str(logical_id)]["diameter_mm"])
    size = int(math.ceil((diameter_mm + 2.0 * margin_mm) * pixels_per_mm))
    centre = (size / 2.0, size / 2.0)
    points = outline_points(logical_id, config, pixels_per_mm, rotation_clockwise_deg)
    points[:, 0] += centre[0]
    points[:, 1] += centre[1]
    image = np.zeros((size, size), dtype=np.uint8)
    cv2.fillPoly(image, [np.rint(points).astype(np.int32)], 255, cv2.LINE_8)
    return image, centre
