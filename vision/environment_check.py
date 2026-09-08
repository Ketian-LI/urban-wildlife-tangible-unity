"""Check the reproducible local vision-toolchain baseline."""

from __future__ import annotations

import json
import platform
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CONFIG_PATH = ROOT / "vision" / "config" / "calibration.json"


def main() -> int:
    config = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))
    result: dict[str, object] = {
        "project_root": str(ROOT),
        "python": platform.python_version(),
        "python_3_12": sys.version_info[:2] == (3, 12),
        "configuration": str(CONFIG_PATH.relative_to(ROOT)),
    }

    critical_ok = result["python_3_12"]
    try:
        import cv2
        import numpy

        dictionary_id = getattr(cv2.aruco, config["aruco_dictionary"])
        cv2.aruco.getPredefinedDictionary(dictionary_id)
        result.update(
            {
                "opencv": cv2.__version__,
                "numpy": numpy.__version__,
                "aruco_available": hasattr(cv2, "aruco"),
                "aruco_dictionary": config["aruco_dictionary"],
            }
        )
        critical_ok = bool(critical_ok and result["aruco_available"])
    except Exception as exc:  # pragma: no cover - diagnostic path
        result["vision_import_error"] = f"{type(exc).__name__}: {exc}"
        critical_ok = False

    obs_path = Path(config["obs_executable"])
    result["obs_executable"] = str(obs_path)
    result["obs_installed"] = obs_path.is_file()
    result["ready_for_camera_test"] = bool(critical_ok and result["obs_installed"])

    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0 if critical_ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
