from __future__ import annotations

import json
import sys
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "vision"))

from build_layout_packet import atomic_write_json, build_layout_packet, token_set_validation, token_type  # noqa: E402
from generate_layout_fixture import build_fixture  # noqa: E402


class LayoutPacketTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.calibration_config = json.loads(
            (ROOT / "vision" / "config" / "calibration.json").read_text(encoding="utf-8")
        )
        cls.path_config = json.loads(
            (ROOT / "vision" / "config" / "path_detection.json").read_text(encoding="utf-8")
        )
        cls.frame, cls.truth = build_fixture(cls.calibration_config)
        cls.packet, _, _, _, _ = build_layout_packet(
            cls.frame,
            cls.calibration_config,
            cls.path_config,
            "magenta",
            "automated-test",
            2,
            "S001",
            "fixture-calibration",
            1788883200000,
        )

    def test_packet_metadata_is_versioned(self) -> None:
        self.assertEqual(self.packet["schema_version"], "0.1")
        self.assertEqual(self.packet["packet_type"], "confirmed_layout")
        self.assertEqual(self.packet["session_id"], "automated-test")
        self.assertEqual(self.packet["cycle_index"], 2)
        self.assertEqual(self.packet["timestamp_utc"], "2026-09-08T16:00:00Z")
        self.assertEqual(self.packet["recognition"]["token_backend"], "aruco")

    def test_packet_contains_every_schema_required_field(self) -> None:
        schema = json.loads(
            (ROOT / "data" / "schemas" / "layout_packet_v0.1.schema.json").read_text(encoding="utf-8")
        )
        self.assertEqual(set(schema["required"]) - set(self.packet), set())
        self.assertEqual(
            set(schema["properties"]["path"]["required"]) - set(self.packet["path"]),
            set(),
        )

    def test_corner_markers_are_excluded_and_tokens_are_typed(self) -> None:
        self.assertEqual([token["id"] for token in self.packet["tokens"]], [10, 11, 12, 20, 21])
        self.assertEqual([token["type"] for token in self.packet["tokens"]], [
            "food_hotspot",
            "food_hotspot",
            "food_hotspot",
            "woodland",
            "woodland",
        ])
        self.assertTrue(self.packet["validation"]["all_tokens_in_bounds"])
        self.assertTrue(self.packet["validation"]["all_required_tokens_detected"])
        self.assertEqual(self.packet["validation"]["missing_token_ids"], [])
        self.assertEqual(self.packet["validation"]["duplicate_token_ids"], [])

    def test_token_coordinates_match_fixture(self) -> None:
        truth_by_id = {item["id"]: item["center_norm"] for item in self.truth["tokens"]}
        for token in self.packet["tokens"]:
            expected = truth_by_id[token["id"]]
            self.assertAlmostEqual(token["x_norm"], expected[0], delta=0.005)
            self.assertAlmostEqual(token["y_norm"], expected[1], delta=0.005)
            self.assertAlmostEqual(token["angle_deg"], 0.0, delta=1.5)

    def test_path_is_continuous_normalized_polyline(self) -> None:
        path = self.packet["path"]
        self.assertTrue(path["continuous"])
        self.assertGreater(path["point_count"], 5)
        for x, y in path["points_norm"]:
            self.assertGreaterEqual(x, 0.0)
            self.assertLessEqual(x, 1.0)
            self.assertGreaterEqual(y, 0.0)
            self.assertLessEqual(y, 1.0)

    def test_token_range_mapping(self) -> None:
        ranges = self.calibration_config["token_id_ranges"]
        self.assertEqual(token_type(10, ranges), "food_hotspot")
        self.assertEqual(token_type(21, ranges), "woodland")
        self.assertEqual(token_type(35, ranges), "p1_reserved")
        self.assertEqual(token_type(49, ranges), "unknown")

    def test_incomplete_or_duplicate_token_set_is_rejected(self) -> None:
        tokens = [{"id": 10}, {"id": 10}, {"id": 12}, {"id": 20}]
        result = token_set_validation(tokens, [10, 11, 12, 20, 21])
        self.assertFalse(result["all_required_tokens_detected"])
        self.assertEqual(result["missing_token_ids"], [11, 21])
        self.assertEqual(result["duplicate_token_ids"], [10])

    def test_atomic_json_replaces_complete_document(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "latest_layout.json"
            atomic_write_json(path, {"value": "first"})
            atomic_write_json(path, {"value": "second", "complete": True})
            self.assertEqual(json.loads(path.read_text(encoding="utf-8")), {"value": "second", "complete": True})
            self.assertEqual(list(path.parent.glob("*.tmp")), [])


if __name__ == "__main__":
    unittest.main()
