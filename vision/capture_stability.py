"""Wait for a quiet camera interval before accepting a Confirm frame."""

from __future__ import annotations

import time

import cv2
import numpy as np


def analysis_gray(frame: np.ndarray, width: int) -> np.ndarray:
    gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY) if frame.ndim == 3 else frame
    if width > 0 and gray.shape[1] > width:
        height = max(1, round(gray.shape[0] * width / gray.shape[1]))
        gray = cv2.resize(gray, (width, height), interpolation=cv2.INTER_AREA)
    return cv2.GaussianBlur(gray, (5, 5), 0)


def changed_fraction(previous: np.ndarray, current: np.ndarray, pixel_threshold: int) -> float:
    if previous.shape != current.shape:
        raise ValueError("Stability frames must have matching dimensions")
    difference = cv2.absdiff(previous, current)
    return float(np.count_nonzero(difference >= pixel_threshold) / difference.size)


class FrameStabilityGate:
    def __init__(
        self,
        stable_seconds: float,
        difference_pixel_threshold: int,
        maximum_changed_fraction: float,
        analysis_width: int,
        minimum_frames: int,
    ) -> None:
        if stable_seconds <= 0 or minimum_frames < 2:
            raise ValueError("Stability duration must be positive and minimum_frames must be at least 2")
        self.stable_seconds = float(stable_seconds)
        self.difference_pixel_threshold = int(difference_pixel_threshold)
        self.maximum_changed_fraction = float(maximum_changed_fraction)
        self.analysis_width = int(analysis_width)
        self.minimum_frames = int(minimum_frames)
        self.previous: np.ndarray | None = None
        self.stable_since: float | None = None
        self.stable_frames = 0
        self.frames_observed = 0
        self.last_changed_fraction: float | None = None

    def update(self, frame: np.ndarray, timestamp: float) -> bool:
        current = analysis_gray(frame, self.analysis_width)
        self.frames_observed += 1
        if self.previous is None:
            self.previous = current
            return False

        fraction = changed_fraction(self.previous, current, self.difference_pixel_threshold)
        self.previous = current
        self.last_changed_fraction = fraction
        if fraction <= self.maximum_changed_fraction:
            if self.stable_since is None:
                self.stable_since = timestamp
                self.stable_frames = 2
            else:
                self.stable_frames += 1
        else:
            self.stable_since = None
            self.stable_frames = 0
            return False

        return (
            self.stable_frames >= self.minimum_frames
            and timestamp - self.stable_since >= self.stable_seconds
        )


def capture_stable_camera_frame(
    source: int,
    width: int,
    height: int,
    fps: int,
    config: dict[str, object],
) -> tuple[np.ndarray, dict[str, object]]:
    backend = cv2.CAP_DSHOW if hasattr(cv2, "CAP_DSHOW") else cv2.CAP_ANY
    capture = cv2.VideoCapture(source, backend)
    capture.set(cv2.CAP_PROP_FRAME_WIDTH, width)
    capture.set(cv2.CAP_PROP_FRAME_HEIGHT, height)
    capture.set(cv2.CAP_PROP_FPS, fps)
    if not capture.isOpened():
        capture.release()
        raise RuntimeError(f"Camera source {source} could not be opened")

    gate = FrameStabilityGate(
        stable_seconds=float(config["stable_seconds"]),
        difference_pixel_threshold=int(config["difference_pixel_threshold"]),
        maximum_changed_fraction=float(config["maximum_changed_fraction"]),
        analysis_width=int(config["analysis_width"]),
        minimum_frames=int(config["minimum_frames"]),
    )
    maximum_wait = float(config["maximum_wait_seconds"])
    started = time.monotonic()
    accepted: np.ndarray | None = None
    try:
        while time.monotonic() - started <= maximum_wait:
            ok, frame = capture.read()
            if not ok:
                continue
            now = time.monotonic()
            if gate.update(frame, now):
                accepted = frame.copy()
                break
    finally:
        actual_width = int(capture.get(cv2.CAP_PROP_FRAME_WIDTH))
        actual_height = int(capture.get(cv2.CAP_PROP_FRAME_HEIGHT))
        capture.release()

    elapsed = time.monotonic() - started
    if accepted is None:
        last_fraction = gate.last_changed_fraction
        detail = "unavailable" if last_fraction is None else f"{last_fraction:.5f}"
        raise RuntimeError(
            f"Camera source {source} did not remain stable for {gate.stable_seconds:.2f}s "
            f"within {maximum_wait:.2f}s; last changed fraction={detail}"
        )

    report: dict[str, object] = {
        "mode": "stable_camera",
        "source": source,
        "stable": True,
        "stable_seconds_required": gate.stable_seconds,
        "elapsed_seconds": round(elapsed, 3),
        "frames_observed": gate.frames_observed,
        "last_changed_fraction": round(float(gate.last_changed_fraction or 0.0), 6),
        "maximum_changed_fraction": gate.maximum_changed_fraction,
        "difference_pixel_threshold": gate.difference_pixel_threshold,
        "analysis_width": gate.analysis_width,
        "actual_frame_size": [actual_width, actual_height],
    }
    return accepted, report
