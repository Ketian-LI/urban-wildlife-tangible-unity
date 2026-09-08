"""Detect ArUco IDs, pixel centres, and clockwise image-plane angles."""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path

import cv2
import numpy as np

from io_utils import read_image, write_image


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CONFIG = ROOT / "vision" / "config" / "calibration.json"


def create_detector(dictionary_name: str) -> cv2.aruco.ArucoDetector:
    dictionary_id = getattr(cv2.aruco, dictionary_name)
    dictionary = cv2.aruco.getPredefinedDictionary(dictionary_id)
    parameters = cv2.aruco.DetectorParameters()
    return cv2.aruco.ArucoDetector(dictionary, parameters)


def detect_frame(frame: np.ndarray, dictionary_name: str) -> tuple[list[dict[str, object]], np.ndarray]:
    detector = create_detector(dictionary_name)
    gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY) if frame.ndim == 3 else frame
    corners, ids, _ = detector.detectMarkers(gray)
    annotated = cv2.cvtColor(gray, cv2.COLOR_GRAY2BGR) if frame.ndim == 2 else frame.copy()
    detections: list[dict[str, object]] = []
    if ids is None:
        return detections, annotated

    cv2.aruco.drawDetectedMarkers(annotated, corners, ids)
    for marker_corners, marker_id in zip(corners, ids.flatten(), strict=True):
        points = marker_corners.reshape(4, 2)
        centre = points.mean(axis=0)
        top_edge = points[1] - points[0]
        angle = math.degrees(math.atan2(float(top_edge[1]), float(top_edge[0])))
        detection = {
            "id": int(marker_id),
            "center_px": {"x": round(float(centre[0]), 2), "y": round(float(centre[1]), 2)},
            "angle_deg": round(angle, 2),
            "corners_px": [[round(float(x), 2), round(float(y), 2)] for x, y in points],
        }
        detections.append(detection)
        origin = tuple(np.rint(centre).astype(int))
        cv2.circle(annotated, origin, 5, (0, 0, 255), -1)
        cv2.putText(annotated, f"ID {marker_id} {angle:.1f} deg", (origin[0] + 8, origin[1] - 8), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 0, 255), 2)
    detections.sort(key=lambda item: int(item["id"]))
    return detections, annotated


def load_camera_frame(source: int, width: int, height: int, fps: int) -> np.ndarray:
    backend = cv2.CAP_DSHOW if hasattr(cv2, "CAP_DSHOW") else cv2.CAP_ANY
    capture = cv2.VideoCapture(source, backend)
    capture.set(cv2.CAP_PROP_FRAME_WIDTH, width)
    capture.set(cv2.CAP_PROP_FRAME_HEIGHT, height)
    capture.set(cv2.CAP_PROP_FPS, fps)
    if not capture.isOpened():
        capture.release()
        raise RuntimeError(f"Camera source {source} could not be opened")
    frame = None
    for _ in range(20):
        ok, candidate = capture.read()
        if ok:
            frame = candidate
    capture.release()
    if frame is None:
        raise RuntimeError(f"Camera source {source} returned no frame")
    return frame


def main() -> int:
    parser = argparse.ArgumentParser()
    source = parser.add_mutually_exclusive_group(required=True)
    source.add_argument("--image", type=Path)
    source.add_argument("--camera", type=int)
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    parser.add_argument("--output-dir", type=Path, default=ROOT / "data" / "raw" / "marker-tests")
    args = parser.parse_args()

    config = json.loads(args.config.read_text(encoding="utf-8"))
    if args.image:
        frame = read_image(args.image)
    else:
        video = config["video"]
        frame = load_camera_frame(args.camera, video["width"], video["height"], video["fps"])

    detections, annotated = detect_frame(frame, config["aruco_dictionary"])
    args.output_dir.mkdir(parents=True, exist_ok=True)
    write_image(args.output_dir / "annotated.png", annotated)
    (args.output_dir / "detections.json").write_text(
        json.dumps({"dictionary": config["aruco_dictionary"], "detections": detections}, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(json.dumps({"count": len(detections), "detections": detections}, ensure_ascii=False, indent=2))
    return 0 if detections else 2


if __name__ == "__main__":
    raise SystemExit(main())
