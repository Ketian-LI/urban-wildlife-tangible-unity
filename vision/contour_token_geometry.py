"""Shared geometry for contour-coded token templates and synthetic tests."""

from __future__ import annotations

import math

import cv2
import numpy as np


def woodland_felt_outline_points(
    width_mm: float,
    height_mm: float,
    pixels_per_mm: float = 1.0,
    samples_per_side: int = 160,
) -> np.ndarray:
    """Return a centred, asymmetric leaf outline with an exact width and height."""
    width = width_mm * pixels_per_mm
    height = height_mm * pixels_per_mm
    left = np.array([-width / 2.0, 0.0], dtype=np.float32)
    right = np.array([width / 2.0, 0.0], dtype=np.float32)

    def quadratic(p0: np.ndarray, p1: np.ndarray, p2: np.ndarray) -> np.ndarray:
        t = np.linspace(0.0, 1.0, samples_per_side, dtype=np.float32)[:, None]
        return (1 - t) ** 2 * p0 + 2 * (1 - t) * t * p1 + t**2 * p2

    upper = quadratic(left, np.array([-width / 12.0, -height], dtype=np.float32), right)
    lower = quadratic(right, np.array([width / 12.0, height], dtype=np.float32), left)
    return np.vstack([upper, lower]).astype(np.float32)


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
    """Return a clockwise polygon with one orientation notch and encoded radial notches.

    V0.1 configs omit ``type_shapes`` and therefore keep the original circular
    outline.  Later configs may use a shallow six-fold radial modulation for a
    soft hexagon.  Keeping the modulation shallower than the code-notch
    threshold lets the same contour decoder distinguish styling from data.
    """
    code = config["codes"][str(logical_id)]
    geometry = config["geometry"]
    radius_mm = float(code["diameter_mm"]) / 2.0
    type_shape = geometry.get("type_shapes", {}).get(code["type"], {})
    recognition_outline = type_shape.get("recognition_outline", "circle")
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
        if recognition_outline == "soft_hexagon":
            phase_deg = float(type_shape.get("phase_deg", 0.0))
            side_inset_mm = float(type_shape.get("side_inset_mm", 2.0))
            phase = math.radians(6.0 * (local_angle - phase_deg))
            radius = radius_mm - side_inset_mm * (1.0 - math.cos(phase)) / 2.0
        else:
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
