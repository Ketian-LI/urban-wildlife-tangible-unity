"""Normalize generated RGBA frames to a reference sprite without changing pose."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


def alpha_bbox(image: Image.Image, threshold: int = 8) -> tuple[int, int, int, int]:
    alpha = image.getchannel("A")
    mask = alpha.point(lambda value: 255 if value > threshold else 0)
    bbox = mask.getbbox()
    if bbox is None:
        raise ValueError("sprite has no visible alpha silhouette")
    return bbox


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("reference", type=Path)
    parser.add_argument("destination", type=Path)
    parser.add_argument("sources", type=Path, nargs="+")
    parser.add_argument("--axis", choices=("width", "height"), required=True)
    args = parser.parse_args()

    reference = Image.open(args.reference).convert("RGBA")
    ref_left, ref_top, ref_right, ref_bottom = alpha_bbox(reference)
    ref_width = ref_right - ref_left
    ref_height = ref_bottom - ref_top
    ref_extent = ref_width if args.axis == "width" else ref_height
    ref_canvas_extent = reference.width if args.axis == "width" else reference.height
    centre_x_ratio = ((ref_left + ref_right) * 0.5) / reference.width
    bottom_ratio = ref_bottom / reference.height

    args.destination.mkdir(parents=True, exist_ok=True)
    for source_path in args.sources:
        source = Image.open(source_path).convert("RGBA")
        left, top, right, bottom = alpha_bbox(source)
        subject = source.crop((left, top, right, bottom))
        current_extent = subject.width if args.axis == "width" else subject.height
        target_extent = (ref_extent / ref_canvas_extent) * (
            source.width if args.axis == "width" else source.height
        )
        scale = target_extent / max(1, current_extent)
        resized = subject.resize(
            (
                max(1, round(subject.width * scale)),
                max(1, round(subject.height * scale)),
            ),
            Image.Resampling.LANCZOS,
        )

        output = Image.new("RGBA", source.size, (0, 0, 0, 0))
        target_centre_x = centre_x_ratio * source.width
        target_bottom = bottom_ratio * source.height
        paste_x = round(target_centre_x - resized.width * 0.5)
        paste_y = round(target_bottom - resized.height)
        if (
            paste_x < 0
            or paste_y < 0
            or paste_x + resized.width > source.width
            or paste_y + resized.height > source.height
        ):
            raise ValueError(
                f"normalized sprite would be clipped: {source_path.name} "
                f"at {(paste_x, paste_y)} size={resized.size} canvas={source.size}"
            )
        output.alpha_composite(resized, (paste_x, paste_y))
        output.save(args.destination / source_path.name)
        print(
            f"{source_path.name}: bbox={right-left}x{bottom-top} "
            f"scale={scale:.4f} normalized={resized.width}x{resized.height} "
            f"baseline={paste_y + resized.height}"
        )


if __name__ == "__main__":
    main()
