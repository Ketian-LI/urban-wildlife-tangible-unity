"""Generate a privacy-safe rectified board for contour-token validation."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import cv2
import numpy as np

from contour_token_geometry import render_token
from io_utils import write_image


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CONFIG = ROOT / "vision" / "config" / "contour_tokens.json"


def create_fixture(config: dict[str, object]) -> tuple[np.ndarray, dict[str, object]]:
    frame = np.full((600, 900, 3), 24, dtype=np.uint8)
    cv2.ellipse(frame, (455, 435), (92, 58), 0, 0, 360, (85, 75, 65), -1)
    cv2.ellipse(frame, (230, 410), (82, 68), 0, 0, 360, (64, 88, 64), -1)
    cv2.ellipse(frame, (710, 405), (82, 68), 0, 0, 360, (64, 88, 64), -1)
    cv2.rectangle(frame, (770, 500), (865, 565), (210, 210, 210), -1)

    placements = {
        10: {"centre": (125, 145), "angle_deg": 0},
        11: {"centre": (335, 140), "angle_deg": 45},
        12: {"centre": (555, 150), "angle_deg": 90},
        20: {"centre": (260, 405), "angle_deg": -45},
        21: {"centre": (675, 400), "angle_deg": 135},
    }
    for logical_id, placement in placements.items():
        token, _ = render_token(
            logical_id,
            config,
            pixels_per_mm=1.0,
            rotation_clockwise_deg=placement["angle_deg"],
        )
        token_height, token_width = token.shape
        x = placement["centre"][0] - token_width // 2
        y = placement["centre"][1] - token_height // 2
        region = frame[y : y + token_height, x : x + token_width]
        region[token > 0] = (215, 215, 215)

    truth: dict[str, object] = {
        "image_definition": "rectified 900 x 600 board; 1 px/mm",
        "tokens": [
            {
                "id": logical_id,
                "type": config["codes"][str(logical_id)]["type"],
                "center_px": list(placement["centre"]),
                "angle_deg": placement["angle_deg"],
            }
            for logical_id, placement in placements.items()
        ],
        "distractor": "95 x 65 px bright rectangle; must be rejected",
    }
    return frame, truth


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    parser.add_argument("--output-dir", type=Path, required=True)
    args = parser.parse_args()
    config = json.loads(args.config.read_text(encoding="utf-8"))
    frame, truth = create_fixture(config)
    args.output_dir.mkdir(parents=True, exist_ok=True)
    write_image(args.output_dir / "input.png", frame)
    (args.output_dir / "ground_truth.json").write_text(
        json.dumps(truth, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(json.dumps({"image": str(args.output_dir / "input.png"), "ground_truth": truth}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
