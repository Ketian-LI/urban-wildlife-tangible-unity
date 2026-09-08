from __future__ import annotations

import json
import sys
import unittest
from pathlib import Path

import cv2
import numpy as np


ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "vision"))

from contour_token_geometry import render_token  # noqa: E402
from detect_contour_tokens import detect_contour_tokens  # noqa: E402


class ContourTokenDetectionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.config = json.loads(
            (ROOT / "vision" / "config" / "contour_tokens.json").read_text(encoding="utf-8")
        )

    def build_board(self, rotations: dict[int, float]) -> np.ndarray:
        board = np.full((600, 900, 3), 24, dtype=np.uint8)
        cv2.ellipse(board, (450, 440), (90, 55), 0, 0, 360, (85, 75, 65), -1)
        centres = {10: (120, 150), 11: (330, 145), 12: (550, 155), 20: (245, 400), 21: (680, 405)}
        for logical_id, centre in centres.items():
            token, _ = render_token(logical_id, self.config, pixels_per_mm=1.0, rotation_clockwise_deg=rotations[logical_id])
            height, width = token.shape
            x = centre[0] - width // 2
            y = centre[1] - height // 2
            region = board[y : y + height, x : x + width]
            region[token > 0] = (215, 215, 215)
        return board

    def test_all_five_ids_detect(self) -> None:
        rotations = {10: 0, 11: 45, 12: 90, 20: -45, 21: 135}
        detections, _, _ = detect_contour_tokens(self.build_board(rotations), self.config)
        self.assertEqual([item["id"] for item in detections], [10, 11, 12, 20, 21])

    def test_angles_follow_direction_notch(self) -> None:
        rotations = {10: 0, 11: 45, 12: 90, 20: -45, 21: 135}
        detections, _, _ = detect_contour_tokens(self.build_board(rotations), self.config)
        by_id = {item["id"]: item for item in detections}
        for logical_id, expected in rotations.items():
            self.assertAlmostEqual(by_id[logical_id]["angle_deg"], expected, delta=5.0)

    def test_dark_and_oversized_regions_are_rejected(self) -> None:
        board = self.build_board({10: 0, 11: 0, 12: 0, 20: 0, 21: 0})
        cv2.rectangle(board, (20, 500), (100, 570), (220, 220, 220), -1)
        cv2.circle(board, (820, 520), 20, (90, 90, 90), -1)
        detections, _, _ = detect_contour_tokens(board, self.config)
        self.assertEqual([item["id"] for item in detections], [10, 11, 12, 20, 21])

    def test_each_id_at_all_eight_acceptance_rotations(self) -> None:
        tolerance = float(self.config["acceptance"]["maximum_angle_error_deg"])
        for logical_id in (10, 11, 12, 20, 21):
            for angle in self.config["acceptance"]["test_rotations_deg"]:
                with self.subTest(logical_id=logical_id, angle=angle):
                    board = np.full((180, 180, 3), 24, dtype=np.uint8)
                    token, _ = render_token(logical_id, self.config, 1.0, angle)
                    height, width = token.shape
                    x = 90 - width // 2
                    y = 90 - height // 2
                    board[y : y + height, x : x + width][token > 0] = (215, 215, 215)
                    detections, _, _ = detect_contour_tokens(board, self.config)
                    self.assertEqual(len(detections), 1)
                    self.assertEqual(detections[0]["id"], logical_id)
                    error = abs((float(detections[0]["angle_deg"]) - angle + 180.0) % 360.0 - 180.0)
                    self.assertLessEqual(error, tolerance)


if __name__ == "__main__":
    unittest.main()
