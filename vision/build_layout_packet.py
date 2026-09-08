"""Build one versioned Unity layout packet from a confirmed camera frame."""

from __future__ import annotations

import argparse
import json
import math
import os
import tempfile
from datetime import datetime, timezone
from pathlib import Path

import cv2
import numpy as np

from calibrate_corners import calibrate_frame, camera_to_normalized, transform_point
from detect_markers import detect_frame, load_camera_frame
from detect_path import detect_path
from io_utils import read_image, write_image


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CALIBRATION_CONFIG = ROOT / "vision" / "config" / "calibration.json"
DEFAULT_PATH_CONFIG = ROOT / "vision" / "config" / "path_detection.json"


def token_type(marker_id: int, token_ranges: dict[str, list[int]]) -> str:
    for name, bounds in token_ranges.items():
        if int(bounds[0]) <= marker_id <= int(bounds[1]):
            return name
    return "unknown"


def normalized_angle(detection: dict[str, object], homography: np.ndarray) -> float:
    """Transform the marker's top edge and return its clockwise board-plane angle."""
    corners = detection["corners_px"]
    start = transform_point((float(corners[0][0]), float(corners[0][1])), homography)
    end = transform_point((float(corners[1][0]), float(corners[1][1])), homography)
    angle = math.degrees(math.atan2(end[1] - start[1], end[0] - start[0]))
    return round((angle + 180.0) % 360.0 - 180.0, 2)


def largest_component(mask: np.ndarray) -> np.ndarray:
    count, labels, stats, _ = cv2.connectedComponentsWithStats(mask, connectivity=8)
    if count <= 1:
        return np.zeros_like(mask)
    largest_label = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
    component = np.zeros_like(mask)
    component[labels == largest_label] = 255
    return component


def mask_to_polyline(mask: np.ndarray, sample_spacing_px: int = 24, epsilon_px: float = 3.0) -> list[list[float]]:
    """Sample the centre of a single left-to-right P0 path and normalize it."""
    component = largest_component(mask)
    y_values, x_values = np.nonzero(component)
    if x_values.size == 0:
        return []

    minimum_x = int(x_values.min())
    maximum_x = int(x_values.max())
    samples: list[list[float]] = []
    starts = list(range(minimum_x, maximum_x + 1, sample_spacing_px))
    if starts[-1] != maximum_x:
        starts.append(maximum_x)
    for index, start in enumerate(starts):
        end = starts[index + 1] if index + 1 < len(starts) else maximum_x + 1
        selected = (x_values >= start) & (x_values < end)
        if not np.any(selected):
            continue
        samples.append([float(np.median(x_values[selected])), float(np.median(y_values[selected]))])

    if len(samples) < 2:
        return []
    points = np.asarray(samples, dtype=np.float32).reshape((-1, 1, 2))
    simplified = cv2.approxPolyDP(points, epsilon_px, False).reshape((-1, 2))
    height, width = mask.shape
    return [
        [round(float(x) / (width - 1), 6), round(float(y) / (height - 1), 6)]
        for x, y in simplified
    ]


def build_token(
    detection: dict[str, object],
    homography: np.ndarray,
    width: int,
    height: int,
    token_ranges: dict[str, list[int]],
) -> dict[str, object]:
    centre = detection["center_px"]
    normalized = camera_to_normalized((float(centre["x"]), float(centre["y"])), homography, width, height)
    x_norm = round(normalized["x"], 6)
    y_norm = round(normalized["y"], 6)
    return {
        "id": int(detection["id"]),
        "type": token_type(int(detection["id"]), token_ranges),
        "x_norm": x_norm,
        "y_norm": y_norm,
        "angle_deg": normalized_angle(detection, homography),
        "in_bounds": 0.0 <= x_norm <= 1.0 and 0.0 <= y_norm <= 1.0,
        "confidence": 1.0,
    }


def draw_packet_overlay(warped: np.ndarray, path_overlay: np.ndarray, tokens: list[dict[str, object]]) -> np.ndarray:
    overlay = path_overlay.copy()
    height, width = warped.shape[:2]
    for token in tokens:
        x = round(float(token["x_norm"]) * (width - 1))
        y = round(float(token["y_norm"]) * (height - 1))
        colour = (0, 255, 0) if token["in_bounds"] else (0, 0, 255)
        cv2.circle(overlay, (x, y), 9, colour, -1, cv2.LINE_AA)
        cv2.putText(
            overlay,
            f"ID {token['id']} {token['type']}",
            (x + 12, y - 10),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.55,
            colour,
            2,
            cv2.LINE_AA,
        )
    return overlay


def build_layout_packet(
    frame: np.ndarray,
    calibration_config: dict[str, object],
    path_config: dict[str, object],
    path_preset: str,
    session_id: str,
    cycle_index: int,
    scenario_id: str,
    calibration_id: str,
    timestamp_ms: int,
) -> tuple[dict[str, object], dict[str, object], np.ndarray, np.ndarray, np.ndarray]:
    width = int(calibration_config["board_size_mm"]["width"])
    height = int(calibration_config["board_size_mm"]["height"])
    calibration, calibration_overlay, warped = calibrate_frame(
        frame,
        calibration_config["aruco_dictionary"],
        calibration_config["corner_ids"],
        width,
        height,
    )
    homography = np.asarray(calibration["homography_camera_to_board"], dtype=np.float64)
    detections, _ = detect_frame(frame, calibration_config["aruco_dictionary"])
    corner_ids = set(int(value) for value in calibration_config["corner_ids"].values())
    tokens = [
        build_token(item, homography, width, height, calibration_config["token_id_ranges"])
        for item in detections
        if int(item["id"]) not in corner_ids
    ]
    tokens.sort(key=lambda item: int(item["id"]))

    path_result, path_mask, path_overlay = detect_path(warped, path_preset, path_config)
    points_norm = mask_to_polyline(path_mask)
    timestamp_utc = datetime.fromtimestamp(timestamp_ms / 1000.0, tz=timezone.utc).isoformat().replace("+00:00", "Z")
    packet: dict[str, object] = {
        "schema_version": "0.1",
        "packet_type": "confirmed_layout",
        "timestamp_ms": timestamp_ms,
        "timestamp_utc": timestamp_utc,
        "session_id": session_id,
        "cycle_index": cycle_index,
        "scenario_id": scenario_id,
        "calibration_id": calibration_id,
        "coordinate_system": {
            "origin": "top_left",
            "x_axis": "right",
            "y_axis": "down",
            "range": [0.0, 1.0],
            "unity_plane_mapping": "x_norm -> Unity X; y_norm -> Unity Z",
        },
        "tokens": tokens,
        "path": {
            "format": "polyline",
            "preset": path_preset,
            "points_norm": points_norm,
            "point_count": len(points_norm),
            "continuous": int(path_result["component_count"]) == 1,
            "component_count": path_result["component_count"],
            "mask_area_fraction": round(float(path_result["mask_fraction"]), 8),
        },
        "validation": {
            "all_tokens_in_bounds": all(bool(token["in_bounds"]) for token in tokens),
            "path_detected": bool(path_result["path_detected"]),
            "path_continuous": int(path_result["component_count"]) == 1,
        },
    }
    packet_overlay = draw_packet_overlay(warped, path_overlay, tokens)
    return packet, calibration, path_mask, packet_overlay, calibration_overlay


def atomic_write_json(path: Path, payload: dict[str, object]) -> None:
    """Write complete JSON then atomically replace the destination on Windows."""
    path.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary_name = tempfile.mkstemp(prefix=f".{path.name}.", suffix=".tmp", dir=path.parent)
    temporary_path = Path(temporary_name)
    try:
        with os.fdopen(descriptor, "w", encoding="utf-8", newline="\n") as handle:
            json.dump(payload, handle, ensure_ascii=False, indent=2)
            handle.write("\n")
        os.replace(temporary_path, path)
    finally:
        if temporary_path.exists():
            temporary_path.unlink()


def main() -> int:
    parser = argparse.ArgumentParser()
    source = parser.add_mutually_exclusive_group(required=True)
    source.add_argument("--image", type=Path)
    source.add_argument("--camera", type=int)
    parser.add_argument("--calibration-config", type=Path, default=DEFAULT_CALIBRATION_CONFIG)
    parser.add_argument("--path-config", type=Path, default=DEFAULT_PATH_CONFIG)
    parser.add_argument("--path-preset")
    parser.add_argument("--session-id", default="local-test")
    parser.add_argument("--cycle-index", type=int, default=0)
    parser.add_argument("--scenario-id", default="S001")
    parser.add_argument("--calibration-id", default="inline-four-corner")
    parser.add_argument("--timestamp-ms", type=int)
    parser.add_argument("--output-dir", type=Path, default=ROOT / "data" / "raw" / "layout-packets")
    args = parser.parse_args()

    calibration_config = json.loads(args.calibration_config.read_text(encoding="utf-8"))
    path_config = json.loads(args.path_config.read_text(encoding="utf-8"))
    path_preset = args.path_preset or path_config["default_preset"]
    if args.image:
        frame = read_image(args.image)
    else:
        video = calibration_config["video"]
        frame = load_camera_frame(args.camera, video["width"], video["height"], video["fps"])
    timestamp_ms = args.timestamp_ms if args.timestamp_ms is not None else int(datetime.now(tz=timezone.utc).timestamp() * 1000)

    try:
        packet, calibration, path_mask, packet_overlay, calibration_overlay = build_layout_packet(
            frame,
            calibration_config,
            path_config,
            path_preset,
            args.session_id,
            args.cycle_index,
            args.scenario_id,
            args.calibration_id,
            timestamp_ms,
        )
    except ValueError as error:
        print(json.dumps({"ok": False, "error": str(error)}, ensure_ascii=False, indent=2))
        return 2

    if not packet["validation"]["path_detected"]:
        print(json.dumps({"ok": False, "error": "No valid path was detected"}, ensure_ascii=False, indent=2))
        return 2

    archive_path = args.output_dir / "archive" / args.session_id / f"cycle-{args.cycle_index:03d}-{timestamp_ms}.json"
    latest_path = args.output_dir / "latest_layout.json"
    atomic_write_json(archive_path, packet)
    atomic_write_json(latest_path, packet)
    atomic_write_json(args.output_dir / "calibration_used.json", calibration)
    write_image(args.output_dir / "path_mask.png", path_mask)
    write_image(args.output_dir / "packet_overlay.png", packet_overlay)
    write_image(args.output_dir / "calibration_overlay.png", calibration_overlay)
    print(
        json.dumps(
            {
                "ok": True,
                "latest_layout": str(latest_path),
                "archive": str(archive_path),
                "token_count": len(packet["tokens"]),
                "path_point_count": packet["path"]["point_count"],
            },
            ensure_ascii=False,
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
