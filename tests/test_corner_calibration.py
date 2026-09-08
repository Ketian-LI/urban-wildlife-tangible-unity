from __future__ import annotations

import json
import sys
import unittest
from pathlib import Path

import cv2
import numpy as np


ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "vision"))

from calibrate_corners import (  # noqa: E402
    CORNER_ORDER,
    calibrate_frame,
    camera_to_normalized,
    ordered_corner_centres,
    transform_point,
)
from detect_markers import detect_frame  # noqa: E402
from generate_calibration_markers import build_card  # noqa: E402


class CornerCalibrationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.config = json.loads((ROOT / "vision" / "config" / "calibration.json").read_text(encoding="utf-8"))
        cls.frame = cls._build_perspective_fixture()

    @classmethod
    def _build_perspective_fixture(cls) -> np.ndarray:
        canvas = np.full((700, 1000), 230, dtype=np.uint8)
        card_size = 170
        placements = {
            0: (60, 60),
            1: (770, 60),
            2: (770, 470),
            3: (60, 470),
        }
        for marker_id, (x, y) in placements.items():
            card = build_card(marker_id, cls.config["aruco_dictionary"], 70, 50, 120)
            card = cv2.resize(card, (card_size, card_size), interpolation=cv2.INTER_NEAREST)
            canvas[y : y + card_size, x : x + card_size] = card

        source = np.float32([[0, 0], [999, 0], [999, 699], [0, 699]])
        destination = np.float32([[105, 45], [1110, 90], [1045, 795], [45, 725]])
        perspective = cv2.getPerspectiveTransform(source, destination)
        return cv2.warpPerspective(canvas, perspective, (1180, 840), borderValue=245)

    def test_all_four_markers_detect_under_perspective(self) -> None:
        detections, _ = detect_frame(self.frame, self.config["aruco_dictionary"])
        self.assertEqual([item["id"] for item in detections], [0, 1, 2, 3])

    def test_corner_centres_map_to_normalized_corners(self) -> None:
        result, _, _ = calibrate_frame(
            self.frame,
            self.config["aruco_dictionary"],
            self.config["corner_ids"],
            900,
            600,
        )
        homography = np.asarray(result["homography_camera_to_board"], dtype=np.float64)
        expected = [(0.0, 0.0), (1.0, 0.0), (1.0, 1.0), (0.0, 1.0)]
        for name, normalized in zip(CORNER_ORDER, expected, strict=True):
            source = result["source_corners_px"][name]
            actual = camera_to_normalized((source["x"], source["y"]), homography, 900, 600)
            self.assertAlmostEqual(actual["x"], normalized[0], places=5)
            self.assertAlmostEqual(actual["y"], normalized[1], places=5)

    def test_rectified_centre_maps_to_half(self) -> None:
        result, _, _ = calibrate_frame(
            self.frame,
            self.config["aruco_dictionary"],
            self.config["corner_ids"],
            900,
            600,
        )
        homography = np.asarray(result["homography_camera_to_board"], dtype=np.float64)
        inverse = np.linalg.inv(homography)
        camera_centre = transform_point(((900 - 1) / 2, (600 - 1) / 2), inverse)
        normalized = camera_to_normalized(camera_centre, homography, 900, 600)
        self.assertAlmostEqual(normalized["x"], 0.5, places=5)
        self.assertAlmostEqual(normalized["y"], 0.5, places=5)

    def test_missing_corner_has_clear_error(self) -> None:
        detections, _ = detect_frame(self.frame, self.config["aruco_dictionary"])
        without_id_3 = [item for item in detections if item["id"] != 3]
        with self.assertRaisesRegex(ValueError, r"Missing required corner marker IDs: \[3\]"):
            ordered_corner_centres(without_id_3, self.config["corner_ids"])


if __name__ == "__main__":
    unittest.main()
