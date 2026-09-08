from __future__ import annotations

import json
import sys
import unittest
from pathlib import Path

import cv2
import numpy as np


ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "vision"))

from contour_token_geometry import outline_points, render_token, woodland_felt_outline_points  # noqa: E402
from detect_contour_tokens import detect_contour_tokens  # noqa: E402


class ContourTokenDesignV02Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.config = json.loads(
            (ROOT / "vision" / "config" / "contour_tokens_v0.2.json").read_text(encoding="utf-8")
        )

    def test_selected_design_is_a_plus_b(self) -> None:
        selection = self.config["design_selection"]
        self.assertEqual(selection["food_family"], "A")
        self.assertEqual(selection["woodland_family"], "B")
        shapes = self.config["geometry"]["type_shapes"]
        self.assertEqual(shapes["food_hotspot"]["recognition_outline"], "soft_hexagon")
        self.assertEqual(shapes["woodland"]["outer_felt"]["shape"], "leaf")

    def test_food_is_soft_hexagon_and_woodland_core_remains_circle(self) -> None:
        food = outline_points(10, self.config)
        woodland = outline_points(20, self.config)
        food_radii = np.linalg.norm(food, axis=1)
        woodland_radii = np.linalg.norm(woodland, axis=1)
        self.assertGreater(float(np.percentile(food_radii, 90) - np.percentile(food_radii, 55)), 0.8)
        self.assertLess(float(np.percentile(woodland_radii, 90) - np.percentile(woodland_radii, 55)), 0.2)

    def test_leaf_is_exact_size_and_magnetic_pads_fit_inside(self) -> None:
        felt = self.config["geometry"]["type_shapes"]["woodland"]["outer_felt"]
        outline = woodland_felt_outline_points(float(felt["width_mm"]), float(felt["height_mm"]))
        self.assertAlmostEqual(float(np.ptp(outline[:, 0])), 120.0, delta=0.01)
        self.assertAlmostEqual(float(np.ptp(outline[:, 1])), 90.0, delta=0.01)
        pad_half = float(felt["underside_magnetic_pads_mm"]) / 2.0
        for centre_x, centre_y in felt["underside_magnetic_pad_centres_mm"]:
            for x in (float(centre_x) - pad_half, float(centre_x) + pad_half):
                for y in (float(centre_y) - pad_half, float(centre_y) + pad_half):
                    self.assertGreater(cv2.pointPolygonTest(outline, (x, y), False), 0)

    def test_all_ids_detect_at_all_acceptance_rotations(self) -> None:
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
