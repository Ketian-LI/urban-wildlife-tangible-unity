"""Find accessible camera indexes or capture a short headless test."""

from __future__ import annotations

import argparse
import json
import time
from pathlib import Path

import cv2

from io_utils import write_image


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_OUTPUT = ROOT / "data" / "raw" / "camera-test"


def open_capture(source: int) -> cv2.VideoCapture:
    backend = cv2.CAP_DSHOW if hasattr(cv2, "CAP_DSHOW") else cv2.CAP_ANY
    return cv2.VideoCapture(source, backend)


def probe_indexes(max_index: int) -> list[dict[str, object]]:
    found: list[dict[str, object]] = []
    for index in range(max_index + 1):
        capture = open_capture(index)
        opened = capture.isOpened()
        ok, frame = capture.read() if opened else (False, None)
        item: dict[str, object] = {"source": index, "opened": opened, "frame_received": bool(ok)}
        if ok and frame is not None:
            item["width"] = int(frame.shape[1])
            item["height"] = int(frame.shape[0])
        found.append(item)
        capture.release()
    return found


def capture_test(source: int, seconds: float, width: int, height: int, fps: int, output_dir: Path) -> dict[str, object]:
    capture = open_capture(source)
    capture.set(cv2.CAP_PROP_FRAME_WIDTH, width)
    capture.set(cv2.CAP_PROP_FRAME_HEIGHT, height)
    capture.set(cv2.CAP_PROP_FPS, fps)
    if not capture.isOpened():
        capture.release()
        raise RuntimeError(f"Camera source {source} could not be opened")

    started = time.monotonic()
    frames = 0
    last_frame = None
    while time.monotonic() - started < seconds:
        ok, frame = capture.read()
        if ok:
            frames += 1
            last_frame = frame
    elapsed = time.monotonic() - started
    actual_width = int(capture.get(cv2.CAP_PROP_FRAME_WIDTH))
    actual_height = int(capture.get(cv2.CAP_PROP_FRAME_HEIGHT))
    capture.release()

    if last_frame is None:
        raise RuntimeError(f"Camera source {source} returned no frames")
    output_dir.mkdir(parents=True, exist_ok=True)
    write_image(output_dir / "snapshot.png", last_frame)
    report = {
        "source": source,
        "requested": {"width": width, "height": height, "fps": fps},
        "actual": {"width": actual_width, "height": actual_height, "measured_fps": round(frames / elapsed, 2)},
        "duration_seconds": round(elapsed, 2),
        "frames_received": frames,
        "snapshot": str(output_dir / "snapshot.png"),
    }
    (output_dir / "report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    return report


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--list", action="store_true")
    parser.add_argument("--max-index", type=int, default=3)
    parser.add_argument("--source", type=int, default=0)
    parser.add_argument("--seconds", type=float, default=3.0)
    parser.add_argument("--width", type=int, default=1920)
    parser.add_argument("--height", type=int, default=1080)
    parser.add_argument("--fps", type=int, default=30)
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT)
    args = parser.parse_args()

    if args.list:
        result: object = {"sources": probe_indexes(args.max_index)}
    else:
        result = capture_test(args.source, args.seconds, args.width, args.height, args.fps, args.output_dir)
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
