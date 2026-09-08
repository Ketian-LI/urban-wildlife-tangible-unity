from __future__ import annotations

import sys
import unittest
from pathlib import Path

import cv2
import numpy as np


ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "vision"))

from capture_stability import FrameStabilityGate, analysis_gray, changed_fraction  # noqa: E402


class CaptureStabilityTests(unittest.TestCase):
    def create_gate(self) -> FrameStabilityGate:
        return FrameStabilityGate(
            stable_seconds=1.0,
            difference_pixel_threshold=18,
            maximum_changed_fraction=0.015,
            analysis_width=160,
            minimum_frames=3,
        )

    def test_static_sequence_is_accepted_after_one_second(self) -> None:
        gate = self.create_gate()
        frame = np.full((180, 320, 3), 40, dtype=np.uint8)
        accepted = [gate.update(frame, index * 0.1) for index in range(13)]
        self.assertFalse(any(accepted[:11]))
        self.assertTrue(any(accepted[11:]))

    def test_large_motion_resets_stable_interval(self) -> None:
        gate = self.create_gate()
        still = np.full((180, 320, 3), 40, dtype=np.uint8)
        moving = still.copy()
        cv2.rectangle(moving, (30, 30), (180, 140), (240, 240, 240), -1)
        for index in range(8):
            self.assertFalse(gate.update(still, index * 0.1))
        self.assertFalse(gate.update(moving, 0.8))
        self.assertIsNone(gate.stable_since)
        accepted = [gate.update(moving, 0.9 + index * 0.1) for index in range(12)]
        self.assertTrue(accepted[-1])

    def test_small_sensor_noise_stays_below_motion_threshold(self) -> None:
        base = np.full((180, 320, 3), 80, dtype=np.uint8)
        noise = np.zeros_like(base)
        noise[::30, ::30] = 8
        current = cv2.add(base, noise)
        fraction = changed_fraction(analysis_gray(base, 160), analysis_gray(current, 160), 18)
        self.assertLessEqual(fraction, 0.015)


if __name__ == "__main__":
    unittest.main()
