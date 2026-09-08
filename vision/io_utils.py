"""Unicode-safe OpenCV image I/O for Windows project paths."""

from __future__ import annotations

from pathlib import Path

import cv2
import numpy as np


def write_image(path: Path, image: np.ndarray) -> None:
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    extension = path.suffix or ".png"
    success, encoded = cv2.imencode(extension, image)
    if not success:
        raise RuntimeError(f"OpenCV could not encode image as {extension}")
    encoded.tofile(str(path))


def read_image(path: Path, flags: int = cv2.IMREAD_COLOR) -> np.ndarray:
    path = Path(path)
    data = np.fromfile(str(path), dtype=np.uint8)
    image = cv2.imdecode(data, flags)
    if image is None:
        raise RuntimeError(f"Could not read image: {path}")
    return image
