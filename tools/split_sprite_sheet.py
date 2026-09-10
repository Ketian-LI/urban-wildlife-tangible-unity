"""Split a regular sprite sheet into equally sized frame images."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("destination", type=Path)
    parser.add_argument("prefix")
    parser.add_argument("--columns", type=int, required=True)
    parser.add_argument("--rows", type=int, required=True)
    args = parser.parse_args()

    if args.columns <= 0 or args.rows <= 0:
        raise ValueError("columns and rows must be positive")

    image = Image.open(args.source).convert("RGBA")
    if image.width % args.columns or image.height % args.rows:
        raise ValueError(
            f"sheet size {image.width}x{image.height} is not divisible by "
            f"{args.columns}x{args.rows}"
        )

    frame_width = image.width // args.columns
    frame_height = image.height // args.rows
    args.destination.mkdir(parents=True, exist_ok=True)
    index = 1
    for row in range(args.rows):
        for column in range(args.columns):
            left = column * frame_width
            top = row * frame_height
            frame = image.crop((left, top, left + frame_width, top + frame_height))
            frame.save(args.destination / f"{args.prefix}-{index:02d}.png")
            index += 1

    print(
        f"frames={args.columns * args.rows} "
        f"frame_size={frame_width}x{frame_height} destination={args.destination}"
    )


if __name__ == "__main__":
    main()
