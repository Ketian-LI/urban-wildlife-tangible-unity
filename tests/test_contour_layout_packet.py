from __future__ import annotations

import json
import sys
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "vision"))

from build_layout_packet import build_layout_packet  # noqa: E402
from generate_layout_fixture import build_fixture  # noqa: E402


class ContourLayoutPacketTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.calibration_config = json.loads(
            (ROOT / "vision" / "config" / "calibration.json").read_text(encoding="utf-8")
        )
        cls.path_config = json.loads(
            (ROOT / "vision" / "config" / "path_detection.json").read_text(encoding="utf-8")
        )
        cls.contour_config = json.loads(
            (ROOT / "vision" / "config" / "contour_tokens_v0.2.json").read_text(encoding="utf-8")
        )
        cls.frame, cls.truth = build_fixture(
            cls.calibration_config,
            token_backend="contour",
            contour_config=cls.contour_config,
        )
        cls.packet, _, _, _, _ = build_layout_packet(
            cls.frame,
            cls.calibration_config,
            cls.path_config,
            "magenta",
            "contour-test",
            1,
            "S001",
            "contour-fixture-calibration",
            1788883200000,
            token_backend="contour",
            contour_config=cls.contour_config,
        )

    def test_full_packet_uses_selected_contour_backend(self) -> None:
        self.assertEqual(self.packet["recognition"], {
            "token_backend": "contour",
            "token_config_version": "0.2",
        })
        self.assertEqual([token["id"] for token in self.packet["tokens"]], [10, 11, 12, 20, 21])
        self.assertTrue(self.packet["validation"]["all_tokens_in_bounds"])
        self.assertTrue(self.packet["validation"]["all_required_tokens_detected"])

    def test_contour_coordinates_and_angles_survive_full_pipeline(self) -> None:
        truth_by_id = {item["id"]: item for item in self.truth["tokens"]}
        for token in self.packet["tokens"]:
            expected = truth_by_id[token["id"]]
            self.assertAlmostEqual(token["x_norm"], expected["center_norm"][0], delta=0.006)
            self.assertAlmostEqual(token["y_norm"], expected["center_norm"][1], delta=0.006)
            error = abs((float(token["angle_deg"]) - float(expected["angle_deg"]) + 180.0) % 360.0 - 180.0)
            self.assertLessEqual(error, self.contour_config["acceptance"]["maximum_angle_error_deg"])

    def test_path_remains_detected_with_contour_tokens_and_felt(self) -> None:
        self.assertTrue(self.packet["validation"]["path_detected"])
        self.assertTrue(self.packet["validation"]["path_continuous"])
        self.assertGreater(self.packet["path"]["point_count"], 5)


if __name__ == "__main__":
    unittest.main()
