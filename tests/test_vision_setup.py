from __future__ import annotations

import json
import sys
import tempfile
import unittest
from pathlib import Path

import cv2


ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "vision"))

from detect_markers import detect_frame  # noqa: E402
from generate_calibration_markers import build_card  # noqa: E402
from io_utils import read_image, write_image  # noqa: E402


class VisionSetupTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.config = json.loads((ROOT / "vision" / "config" / "calibration.json").read_text(encoding="utf-8"))

    def test_corner_ids_are_unique_and_ordered(self) -> None:
        corner_ids = self.config["corner_ids"]
        self.assertEqual(corner_ids, {"top_left": 0, "top_right": 1, "bottom_right": 2, "bottom_left": 3})
        self.assertEqual(len(set(corner_ids.values())), 4)

    def test_marker_card_detects_at_print_resolution(self) -> None:
        card = build_card(0, self.config["aruco_dictionary"], 70, 50, 300)
        detections, _ = detect_frame(card, self.config["aruco_dictionary"])
        self.assertEqual([item["id"] for item in detections], [0])
        self.assertAlmostEqual(detections[0]["angle_deg"], 0.0, delta=0.2)

    def test_generated_png_can_be_written_and_read(self) -> None:
        card = build_card(3, self.config["aruco_dictionary"], 70, 50, 300)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "中文路径" / "marker.png"
            write_image(path, card)
            self.assertIsNotNone(read_image(path, cv2.IMREAD_GRAYSCALE))


if __name__ == "__main__":
    unittest.main()
