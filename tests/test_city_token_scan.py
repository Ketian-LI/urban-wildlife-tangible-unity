from __future__ import annotations

import json
import sys
import unittest
from pathlib import Path

import cv2
import numpy as np


ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "vision"))

from build_city_token_scan import build_city_token_scan  # noqa: E402
from generate_layout_fixture import marker_card, place_card  # noqa: E402


class CityTokenScanTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.config = json.loads(
            (ROOT / "vision" / "config" / "city_tokens_v0.1.json").read_text(
                encoding="utf-8"
            )
        )

    def fixture(self, unknown_id: int | None = None) -> np.ndarray:
        canvas = np.full((800, 1100, 3), (212, 204, 190), dtype=np.uint8)
        cv2.rectangle(canvas, (100, 100), (1000, 700), (52, 58, 52), -1)
        corners = {
            "top_left": (100, 100),
            "top_right": (1000, 100),
            "bottom_right": (1000, 700),
            "bottom_left": (100, 700),
        }
        for name, centre in corners.items():
            place_card(
                canvas,
                marker_card(
                    int(self.config["corner_ids"][name]),
                    self.config["aruco_dictionary"],
                    92,
                    66,
                ),
                centre,
            )
        positions = {
            100: (250, 220),
            111: (820, 240),
            120: (690, 440),
            131: (310, 500),
            140: (560, 600),
        }
        if unknown_id is not None:
            positions[unknown_id] = (520, 340)
        for marker_id, centre in positions.items():
            place_card(
                canvas,
                marker_card(marker_id, self.config["aruco_dictionary"], 70, 52),
                centre,
            )
        source = np.float32([[0, 0], [1099, 0], [1099, 799], [0, 799]])
        destination = np.float32([[85, 30], [1180, 75], [1115, 865], [30, 800]])
        perspective = cv2.getPerspectiveTransform(source, destination)
        return cv2.warpPerspective(
            canvas, perspective, (1240, 900), borderValue=(235, 235, 235)
        )

    def build(self, frame: np.ndarray) -> dict[str, object]:
        packet, _, _, _ = build_city_token_scan(
            frame,
            self.config,
            "city-camera-test",
            2,
            "city-calibration-test",
            1789322400000,
            {"mode": "image_input", "stable": True},
        )
        return packet

    def test_builds_versioned_normalized_city_packet(self) -> None:
        packet = self.build(self.fixture())
        self.assertEqual(packet["schema_version"], "0.1")
        self.assertEqual(packet["packet_type"], "city_token_scan")
        self.assertEqual(packet["timestamp_utc"], "2026-09-13T18:00:00Z")
        self.assertEqual(packet["development_phase"], 2)
        self.assertEqual(
            [item["id"] for item in packet["token_states"]],
            [100, 111, 120, 131, 140],
        )
        self.assertEqual(
            [item["type"] for item in packet["token_states"]],
            [
                "Apartment",
                "DetachedHouse",
                "Commercial",
                "CommunityFacility",
                "GreenIntervention",
            ],
        )
        for token in packet["token_states"]:
            self.assertGreaterEqual(token["x_norm"], 0.0)
            self.assertLessEqual(token["x_norm"], 1.0)
            self.assertGreaterEqual(token["y_norm"], 0.0)
            self.assertLessEqual(token["y_norm"], 1.0)
            self.assertAlmostEqual(token["rotation_deg"], 0.0, delta=1.5)

    def test_matches_schema_required_fields(self) -> None:
        packet = self.build(self.fixture())
        schema = json.loads(
            (ROOT / "data" / "schemas" / "city_token_scan_v0.1.schema.json").read_text(
                encoding="utf-8"
            )
        )
        self.assertEqual(set(schema["required"]) - set(packet), set())

    def test_unknown_marker_is_rejected(self) -> None:
        with self.assertRaisesRegex(ValueError, "Unknown city Token marker IDs"):
            self.build(self.fixture(500))

    def test_unstable_frame_is_rejected(self) -> None:
        with self.assertRaisesRegex(ValueError, "stable confirmed frame"):
            build_city_token_scan(
                self.fixture(),
                self.config,
                "city-camera-test",
                1,
                "city-calibration-test",
                1789322400001,
                {"mode": "camera", "stable": False},
            )


if __name__ == "__main__":
    unittest.main()
