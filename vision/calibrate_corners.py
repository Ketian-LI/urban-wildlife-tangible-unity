"""Calibrate the board from four corner ArUco markers and normalize coordinates."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import cv2
import numpy as np

from detect_markers import detect_frame, load_camera_frame
from io_utils import read_image, write_image


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CONFIG = ROOT / "vision" / "config" / "calibration.json"
CORNER_ORDER = ("top_left", "top_right", "bottom_right", "bottom_left")


def ordered_corner_centres(
    detections: list[dict[str, object]], corner_ids: dict[str, int]
) -> np.ndarray:
    """Return TL, TR, BR, BL marker centres, failing if any required ID is absent."""
    by_id = {int(item["id"]): item for item in detections}
    missing = [corner_ids[name] for name in CORNER_ORDER if corner_ids[name] not in by_id]
    if missing:
        raise ValueError(f"Missing required corner marker IDs: {missing}")

    points = []
    for name in CORNER_ORDER:
        centre = by_id[corner_ids[name]]["center_px"]
        points.append([float(centre["x"]), float(centre["y"])])
    source = np.asarray(points, dtype=np.float32)
    if not cv2.isContourConvex(source.astype(np.int32)) or abs(cv2.contourArea(source)) < 1.0:
        raise ValueError("Corner markers do not form a valid TL, TR, BR, BL quadrilateral")
    return source


def destination_corners(width: int, height: int) -> np.ndarray:
    if width < 2 or height < 2:
        raise ValueError("Calibration output width and height must both be at least 2 pixels")
    return np.asarray(
        [[0.0, 0.0], [width - 1.0, 0.0], [width - 1.0, height - 1.0], [0.0, height - 1.0]],
        dtype=np.float32,
    )


def compute_homography(source_corners: np.ndarray, width: int, height: int) -> np.ndarray:
    """Map camera-space corner-marker centres onto a rectangular board plane."""
    source = np.asarray(source_corners, dtype=np.float32).reshape(4, 2)
    matrix = cv2.getPerspectiveTransform(source, destination_corners(width, height))
    if not np.isfinite(matrix).all() or abs(np.linalg.det(matrix)) < 1e-12:
        raise ValueError("Could not compute a stable calibration homography")
    return matrix


def transform_point(point: tuple[float, float], homography: np.ndarray) -> tuple[float, float]:
    source = np.asarray([[point]], dtype=np.float32)
    transformed = cv2.perspectiveTransform(source, homography)[0, 0]
    return float(transformed[0]), float(transformed[1])


def camera_to_normalized(
    point: tuple[float, float], homography: np.ndarray, width: int, height: int
) -> dict[str, float]:
    """Convert a camera pixel to normalized board coordinates (origin top-left)."""
    board_x, board_y = transform_point(point, homography)
    return {
        "x": board_x / (width - 1),
        "y": board_y / (height - 1),
    }


def draw_calibration_overlay(
    frame: np.ndarray,
    source_corners: np.ndarray,
    corner_ids: dict[str, int],
) -> np.ndarray:
    overlay = frame.copy()
    polygon = np.rint(source_corners).astype(np.int32).reshape((-1, 1, 2))
    cv2.polylines(overlay, [polygon], True, (0, 255, 0), 4, cv2.LINE_AA)
    for name, point in zip(CORNER_ORDER, source_corners, strict=True):
        x, y = np.rint(point).astype(int)
        cv2.circle(overlay, (x, y), 9, (0, 0, 255), -1, cv2.LINE_AA)
        label = f"{name.upper()} ID {corner_ids[name]}"
        cv2.putText(
            overlay,
            label,
            (x + 12, y - 12),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.65,
            (0, 0, 255),
            2,
            cv2.LINE_AA,
        )
    return overlay


def calibrate_frame(
    frame: np.ndarray,
    dictionary_name: str,
    corner_ids: dict[str, int],
    width: int,
    height: int,
) -> tuple[dict[str, object], np.ndarray, np.ndarray]:
    detections, _ = detect_frame(frame, dictionary_name)
    source_corners = ordered_corner_centres(detections, corner_ids)
    homography = compute_homography(source_corners, width, height)
    overlay = draw_calibration_overlay(frame, source_corners, corner_ids)
    warped = cv2.warpPerspective(frame, homography, (width, height))

    source_by_name = {
        name: {"id": corner_ids[name], "x": float(point[0]), "y": float(point[1])}
        for name, point in zip(CORNER_ORDER, source_corners, strict=True)
    }
    result: dict[str, object] = {
        "schema_version": "0.1",
        "dictionary": dictionary_name,
        "coordinate_definition": {
            "origin": "top_left_corner_marker_centre",
            "x_axis": "right",
            "y_axis": "down",
            "normalized_range": [0.0, 1.0],
            "unity_plane_mapping": "normalized x -> Unity X; normalized y -> Unity Z",
            "note": "The active board rectangle is bounded by the four marker centres.",
        },
        "output_size_px": {"width": width, "height": height},
        "source_corners_px": source_by_name,
        "homography_camera_to_board": homography.tolist(),
        "detections": detections,
    }
    return result, overlay, warped


def main() -> int:
    parser = argparse.ArgumentParser()
    source = parser.add_mutually_exclusive_group(required=True)
    source.add_argument("--image", type=Path)
    source.add_argument("--camera", type=int)
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    parser.add_argument("--output-dir", type=Path, default=ROOT / "data" / "raw" / "calibration-tests")
    parser.add_argument("--width", type=int, default=900)
    parser.add_argument("--height", type=int, default=600)
    args = parser.parse_args()

    config = json.loads(args.config.read_text(encoding="utf-8"))
    if args.image:
        frame = read_image(args.image)
    else:
        video = config["video"]
        frame = load_camera_frame(args.camera, video["width"], video["height"], video["fps"])

    try:
        result, overlay, warped = calibrate_frame(
            frame,
            config["aruco_dictionary"],
            config["corner_ids"],
            args.width,
            args.height,
        )
    except ValueError as error:
        print(json.dumps({"ok": False, "error": str(error)}, ensure_ascii=False, indent=2))
        return 2

    args.output_dir.mkdir(parents=True, exist_ok=True)
    write_image(args.output_dir / "calibration_overlay.png", overlay)
    write_image(args.output_dir / "warped.png", warped)
    (args.output_dir / "calibration.json").write_text(
        json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(
        json.dumps(
            {
                "ok": True,
                "corner_ids": [config["corner_ids"][name] for name in CORNER_ORDER],
                "output_dir": str(args.output_dir),
            },
            ensure_ascii=False,
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
