from __future__ import annotations

import json
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]


class ContourTokenConfigTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.config = json.loads(
            (ROOT / "vision" / "config" / "contour_tokens.json").read_text(encoding="utf-8")
        )

    def test_all_five_logical_ids_have_unique_codes(self) -> None:
        weights = {
            name: slot["weight"]
            for name, slot in self.config["geometry"]["code_slots"].items()
        }
        values = {
            int(logical_id): sum(weights[slot] for slot in code["slots"])
            for logical_id, code in self.config["codes"].items()
        }
        self.assertEqual(values, {10: 1, 11: 2, 12: 3, 20: 4, 21: 5})
        self.assertEqual(len(set(values.values())), 5)

    def test_orientation_notch_is_larger_than_code_notches(self) -> None:
        geometry = self.config["geometry"]
        self.assertGreater(geometry["orientation_notch"]["width_mm"], geometry["code_notch"]["width_mm"])
        self.assertGreater(geometry["orientation_notch"]["depth_mm"], geometry["code_notch"]["depth_mm"])

    def test_type_diameters_match_existing_physical_decisions(self) -> None:
        codes = self.config["codes"]
        self.assertTrue(all(codes[str(marker_id)]["diameter_mm"] == 60 for marker_id in (10, 11, 12)))
        self.assertTrue(all(codes[str(marker_id)]["diameter_mm"] == 50 for marker_id in (20, 21)))


if __name__ == "__main__":
    unittest.main()
