"""Convert the image-generator's uniform green isolation plate to an RGBA sprite."""

from __future__ import annotations

import argparse

import cv2
import numpy as np
from PIL import Image


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source")
    parser.add_argument("destination")
    args = parser.parse_args()

    rgb = np.asarray(Image.open(args.source).convert("RGB"), dtype=np.float32)
    red, green, blue = rgb[..., 0], rgb[..., 1], rgb[..., 2]

    # The generated isolation plate is close to #07F908. Find the largest
    # non-green connected silhouette, then fill it so green/teal clothing is
    # never mistaken for background.
    greenness = green - np.maximum(red, blue)
    rough = np.where(greenness < 145.0, 255, 0).astype(np.uint8)
    contours, _ = cv2.findContours(rough, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    if not contours:
        raise RuntimeError("No foreground silhouette found")
    silhouette = np.zeros_like(rough)
    cv2.drawContours(silhouette, [max(contours, key=cv2.contourArea)], -1, 255, cv2.FILLED)
    silhouette = cv2.GaussianBlur(silhouette, (0, 0), sigmaX=0.65, sigmaY=0.65)
    green_matte = np.clip((150.0 - greenness) / 80.0, 0.0, 1.0)
    alpha = np.minimum(silhouette, (green_matte * 255.0).astype(np.uint8))

    # Remove chroma spill only along the antialiased boundary. The fully opaque
    # interior stays byte-for-byte identical to the generated character.
    foreground = rgb.copy()
    boundary = (alpha > 0) & (alpha < 250)
    neutral_edge_green = np.maximum(red, blue) + 4.0
    foreground[..., 1][boundary] = np.minimum(green[boundary], neutral_edge_green[boundary])
    foreground[alpha == 0] = 0.0

    rgba = np.dstack((foreground.astype(np.uint8), alpha.astype(np.uint8)))
    Image.fromarray(rgba, "RGBA").save(args.destination)


if __name__ == "__main__":
    main()
