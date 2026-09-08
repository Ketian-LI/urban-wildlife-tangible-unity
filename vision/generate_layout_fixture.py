"""Generate a deterministic, privacy-safe camera frame for the full input pipeline."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import cv2
import numpy as np

from io_utils import write_image


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CONFIG = ROOT / "vision" / "config" / "calibration.json"


def marker_card(marker_id: int, dictionary_name: str, card_size: int, marker_size: int) -> np.ndarray:
    dictionary_id = getattr(cv2.aruco, dictionary_name)
    dictionary = cv2.aruco.getPredefinedDictionary(dictionary_id)
    marker = cv2.aruco.generateImageMarker(dictionary, marker_id, marker_size)
    card = np.full((card_size, card_size), 255, dtype=np.uint8)
    offset = (card_size - marker_size) // 2
    card[offset : offset + marker_size, offset : offset + marker_size] = marker
    return cv2.cvtColor(card, cv2.COLOR_GRAY2BGR)


def place_card(frame: np.ndarray, card: np.ndarray, centre: tuple[int, int]) -> None:
    height, width = card.shape[:2]
    x = centre[0] - width // 2
    y = centre[1] - height // 2
    frame[y : y + height, x : x + width] = card


def build_fixture(config: dict[str, object]) -> tuple[np.ndarray, dict[str, object]]:
    canvas = np.full((800, 1100, 3), (205, 195, 180), dtype=np.uint8)
    board_top_left = (100, 100)
    board_bottom_right = (1000, 700)
    cv2.rectangle(canvas, board_top_left, board_bottom_right, (25, 25, 25), -1)
    cv2.ellipse(canvas, (600, 500), (90, 55), 0, 0, 360, (90, 75, 65), -1)
    cv2.ellipse(canvas, (350, 520), (75, 60), 0, 0, 360, (65, 90, 65), -1)
    cv2.ellipse(canvas, (800, 510), (75, 60), 0, 0, 360, (65, 90, 65), -1)

    magenta_hsv = np.uint8([[[160, 230, 230]]])
    magenta = tuple(int(value) for value in cv2.cvtColor(magenta_hsv, cv2.COLOR_HSV2BGR)[0, 0])
    path_points = np.asarray(
        [[120, 380], [270, 310], [430, 345], [570, 280], [740, 325], [880, 300], [980, 390]],
        dtype=np.int32,
    )
    cv2.polylines(canvas, [path_points], False, magenta, 16, cv2.LINE_AA)

    corner_centres = {
        "top_left": (100, 100),
        "top_right": (1000, 100),
        "bottom_right": (1000, 700),
        "bottom_left": (100, 700),
    }
    for name, centre in corner_centres.items():
        marker_id = int(config["corner_ids"][name])
        place_card(canvas, marker_card(marker_id, config["aruco_dictionary"], 92, 66), centre)

    token_positions = {
        10: (360, 235),
        11: (690, 225),
        12: (540, 450),
        20: (330, 570),
        21: (775, 560),
    }
    for marker_id, centre in token_positions.items():
        place_card(canvas, marker_card(marker_id, config["aruco_dictionary"], 66, 48), centre)

    source = np.float32([[0, 0], [1099, 0], [1099, 799], [0, 799]])
    destination = np.float32([[95, 35], [1170, 80], [1110, 865], [35, 790]])
    perspective = cv2.getPerspectiveTransform(source, destination)
    frame = cv2.warpPerspective(canvas, perspective, (1240, 900), borderValue=(235, 235, 235))
    truth: dict[str, object] = {
        "corner_ids": config["corner_ids"],
        "tokens": [
            {
                "id": marker_id,
                "center_norm": [round((centre[0] - 100) / 900, 6), round((centre[1] - 100) / 600, 6)],
            }
            for marker_id, centre in sorted(token_positions.items())
        ],
        "path_preset": "magenta",
    }
    return frame, truth


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    parser.add_argument("--output-dir", type=Path, required=True)
    args = parser.parse_args()
    config = json.loads(args.config.read_text(encoding="utf-8"))
    frame, truth = build_fixture(config)
    args.output_dir.mkdir(parents=True, exist_ok=True)
    write_image(args.output_dir / "input.png", frame)
    (args.output_dir / "ground_truth.json").write_text(
        json.dumps(truth, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(json.dumps({"image": str(args.output_dir / "input.png"), "ground_truth": truth}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
