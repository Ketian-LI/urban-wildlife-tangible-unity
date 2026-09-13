"""Build a validated city Token scan for Unity from one stable overhead frame."""

from __future__ import annotations

import argparse
from collections import Counter
import json
import math
from datetime import datetime, timezone
from pathlib import Path

import cv2
import numpy as np

from build_layout_packet import atomic_write_json
from calibrate_corners import calibrate_frame, camera_to_normalized, transform_point
from capture_stability import capture_stable_camera_frame
from detect_markers import detect_frame
from io_utils import read_image, write_image


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CONFIG = ROOT / "vision" / "config" / "city_tokens_v0.1.json"


def normalized_angle(detection: dict[str, object], homography: np.ndarray) -> float:
    corners = detection["corners_px"]
    start = transform_point((float(corners[0][0]), float(corners[0][1])), homography)
    end = transform_point((float(corners[1][0]), float(corners[1][1])), homography)
    angle = math.degrees(math.atan2(end[1] - start[1], end[0] - start[0]))
    return round((angle + 180.0) % 360.0 - 180.0, 2)


def build_city_token_scan(
    frame: np.ndarray,
    config: dict[str, object],
    session_id: str,
    development_phase: int,
    calibration_id: str,
    timestamp_ms: int,
    capture_metadata: dict[str, object],
) -> tuple[dict[str, object], dict[str, object], np.ndarray, np.ndarray]:
    """Rectify a frame and convert known city markers into normalized Token states."""
    if not session_id.strip() or development_phase < 1 or not calibration_id.strip():
        raise ValueError("Session, development phase and calibration identity are required")
    if not capture_metadata.get("stable", False):
        raise ValueError("City Token scan requires a stable confirmed frame")
    width = int(config["board_size_mm"]["width"])
    height = int(config["board_size_mm"]["height"])
    calibration, calibration_overlay, warped = calibrate_frame(
        frame,
        str(config["aruco_dictionary"]),
        config["corner_ids"],
        width,
        height,
    )
    homography = np.asarray(calibration["homography_camera_to_board"], dtype=np.float64)
    detections, _ = detect_frame(frame, str(config["aruco_dictionary"]))
    corner_ids = {int(value) for value in config["corner_ids"].values()}
    inventory = {int(marker_id): token_type for marker_id, token_type in config["token_inventory"].items()}
    token_detections = [item for item in detections if int(item["id"]) not in corner_ids]

    unknown_ids = sorted({int(item["id"]) for item in token_detections if int(item["id"]) not in inventory})
    if unknown_ids:
        raise ValueError(f"Unknown city Token marker IDs: {unknown_ids}")
    counts = Counter(int(item["id"]) for item in token_detections)
    duplicates = sorted(marker_id for marker_id, count in counts.items() if count > 1)
    if duplicates:
        raise ValueError(f"Duplicate city Token marker IDs: {duplicates}")

    tokens: list[dict[str, object]] = []
    for detection in token_detections:
        marker_id = int(detection["id"])
        centre = detection["center_px"]
        normalized = camera_to_normalized(
            (float(centre["x"]), float(centre["y"])),
            homography,
            width,
            height,
        )
        x_norm = round(float(normalized["x"]), 6)
        y_norm = round(float(normalized["y"]), 6)
        if not 0.0 <= x_norm <= 1.0 or not 0.0 <= y_norm <= 1.0:
            raise ValueError(f"City Token {marker_id} is outside the calibrated board")
        tokens.append(
            {
                "id": marker_id,
                "type": inventory[marker_id],
                "x_norm": x_norm,
                "y_norm": y_norm,
                "rotation_deg": normalized_angle(detection, homography),
                "confidence": 1.0,
            }
        )
    tokens.sort(key=lambda item: int(item["id"]))

    timestamp_utc = datetime.fromtimestamp(
        timestamp_ms / 1000.0, tz=timezone.utc
    ).isoformat().replace("+00:00", "Z")
    packet: dict[str, object] = {
        "schema_version": "0.1",
        "packet_type": "city_token_scan",
        "timestamp_ms": timestamp_ms,
        "timestamp_utc": timestamp_utc,
        "scan_id": f"city-scan-{timestamp_ms}",
        "session_id": session_id,
        "development_phase": development_phase,
        "calibration_id": calibration_id,
        "coordinate_system": {
            "origin": "top_left",
            "x_axis": "right",
            "y_axis": "down",
            "range": [0.0, 1.0],
            "unity_plane_mapping": "x_norm -> Unity X; y_norm -> Unity Z",
        },
        "capture": capture_metadata,
        "recognition": {
            "marker_backend": "aruco",
            "marker_family": str(config["aruco_dictionary"]),
            "token_config_version": str(config["schema_version"]),
        },
        "token_states": tokens,
    }

    overlay = warped.copy()
    for token in tokens:
        x = round(float(token["x_norm"]) * (width - 1))
        y = round(float(token["y_norm"]) * (height - 1))
        cv2.circle(overlay, (x, y), 10, (0, 180, 255), -1, cv2.LINE_AA)
        cv2.putText(
            overlay,
            f"{token['id']} {token['type']}",
            (x + 14, y - 10),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.55,
            (0, 80, 150),
            2,
            cv2.LINE_AA,
        )
    return packet, calibration, overlay, calibration_overlay


def main() -> int:
    parser = argparse.ArgumentParser()
    source = parser.add_mutually_exclusive_group(required=True)
    source.add_argument("--image", type=Path)
    source.add_argument("--camera", type=int)
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    parser.add_argument("--session-id", default="local-city-test")
    parser.add_argument("--development-phase", type=int, default=1)
    parser.add_argument("--calibration-id", default="inline-city-four-corner")
    parser.add_argument("--timestamp-ms", type=int)
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=ROOT / "data" / "raw" / "city-token-scans",
    )
    args = parser.parse_args()
    if args.development_phase < 1:
        parser.error("--development-phase must be at least 1")

    config = json.loads(args.config.read_text(encoding="utf-8"))
    try:
        if args.image:
            frame = read_image(args.image)
            capture_metadata: dict[str, object] = {
                "mode": "image_input",
                "stable": True,
                "note": "Deterministic image input; no live stability claim",
            }
        else:
            video = config["video"]
            frame, capture_metadata = capture_stable_camera_frame(
                args.camera,
                int(video["width"]),
                int(video["height"]),
                int(video["fps"]),
                config["stability"],
            )
        timestamp_ms = args.timestamp_ms
        if timestamp_ms is None:
            timestamp_ms = int(datetime.now(tz=timezone.utc).timestamp() * 1000)
        packet, calibration, overlay, calibration_overlay = build_city_token_scan(
            frame,
            config,
            args.session_id,
            args.development_phase,
            args.calibration_id,
            timestamp_ms,
            capture_metadata,
        )
    except (RuntimeError, ValueError) as error:
        print(json.dumps({"ok": False, "error": str(error)}, ensure_ascii=False, indent=2))
        return 2

    archive_path = (
        args.output_dir
        / "archive"
        / args.session_id
        / f"phase-{args.development_phase:02d}-{timestamp_ms}.json"
    )
    latest_path = args.output_dir / "latest_city_scan.json"
    atomic_write_json(archive_path, packet)
    atomic_write_json(latest_path, packet)
    atomic_write_json(args.output_dir / "calibration_used.json", calibration)
    atomic_write_json(args.output_dir / "capture_stability.json", packet["capture"])
    write_image(args.output_dir / "scan_overlay.png", overlay)
    write_image(args.output_dir / "calibration_overlay.png", calibration_overlay)
    print(
        json.dumps(
            {
                "ok": True,
                "latest_city_scan": str(latest_path),
                "archive": str(archive_path),
                "token_count": len(packet["token_states"]),
                "capture_mode": packet["capture"]["mode"],
            },
            ensure_ascii=False,
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
