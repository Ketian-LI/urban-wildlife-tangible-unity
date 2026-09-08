from __future__ import annotations

import json
import sys
import unittest
from pathlib import Path

import cv2
import numpy as np


ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "vision"))

from detect_path import detect_path, hsv_mask, remove_small_components  # noqa: E402


def hsv_colour(hue: int, saturation: int = 230, value: int = 230) -> tuple[int, int, int]:
    pixel = np.uint8([[[hue, saturation, value]]])
    bgr = cv2.cvtColor(pixel, cv2.COLOR_HSV2BGR)[0, 0]
    return tuple(int(channel) for channel in bgr)


class PathDetectionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.config = json.loads((ROOT / "vision" / "config" / "path_detection.json").read_text(encoding="utf-8"))

    def build_frame(self) -> np.ndarray:
        frame = np.full((420, 700, 3), 22, dtype=np.uint8)
        magenta = hsv_colour(160)
        points = np.asarray([[30, 310], [160, 230], [310, 260], [470, 135], [670, 95]], dtype=np.int32)
        cv2.polylines(frame, [points], False, magenta, 16, cv2.LINE_AA)
        cv2.circle(frame, (30, 30), 2, magenta, -1)
        cv2.circle(frame, (650, 380), 3, magenta, -1)
        cv2.line(frame, (80, 370), (600, 370), (100, 100, 100), 20)
        return frame

    def test_magenta_path_survives_and_noise_is_removed(self) -> None:
        result, mask, _ = detect_path(self.build_frame(), "magenta", self.config)
        self.assertTrue(result["path_detected"])
        self.assertEqual(result["component_count"], 1)
        self.assertGreater(result["mask_area_px"], 8000)
        self.assertEqual(int(mask[260, 310]), 255)
        self.assertEqual(int(mask[30, 30]), 0)
        self.assertEqual(int(mask[370, 300]), 0)

    def test_colour_presets_are_separate(self) -> None:
        frame = self.build_frame()
        magenta_result, _, _ = detect_path(frame, "magenta", self.config)
        cyan_result, _, _ = detect_path(frame, "cyan", self.config)
        orange_result, _, _ = detect_path(frame, "orange", self.config)
        self.assertTrue(magenta_result["path_detected"])
        self.assertFalse(cyan_result["path_detected"])
        self.assertFalse(orange_result["path_detected"])

    def test_multiple_hsv_ranges_are_combined(self) -> None:
        frame = np.zeros((30, 60, 3), dtype=np.uint8)
        frame[:, :30] = hsv_colour(10)
        frame[:, 30:] = hsv_colour(160)
        ranges = [
            {"lower": [5, 100, 80], "upper": [25, 255, 255]},
            {"lower": [140, 100, 80], "upper": [179, 255, 255]},
        ]
        mask = hsv_mask(frame, ranges)
        self.assertEqual(int(cv2.countNonZero(mask)), 30 * 60)

    def test_small_components_are_filtered(self) -> None:
        mask = np.zeros((100, 100), dtype=np.uint8)
        cv2.rectangle(mask, (10, 10), (49, 49), 255, -1)
        cv2.rectangle(mask, (80, 80), (82, 82), 255, -1)
        cleaned, areas = remove_small_components(mask, minimum_area=100)
        self.assertEqual(areas, [1600])
        self.assertEqual(int(cleaned[20, 20]), 255)
        self.assertEqual(int(cleaned[81, 81]), 0)

    def test_unknown_preset_has_clear_error(self) -> None:
        with self.assertRaisesRegex(ValueError, "Unknown colour preset 'blue'"):
            detect_path(self.build_frame(), "blue", self.config)


if __name__ == "__main__":
    unittest.main()
