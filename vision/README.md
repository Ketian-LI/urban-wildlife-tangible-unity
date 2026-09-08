# Vision

这里放置摄像头、Marker、彩绳和坐标映射代码。

第一周建议按以下顺序验证：

1. 摄像头稳定取流。
2. 单个 ArUco 或 AprilTag 的 ID、位置和角度。
3. 多 Token 与短暂遮挡容错。
4. 彩色路径 HSV 分割与去噪。
5. 四角校准与标准化坐标。
6. 统一数据包与 Debug 可视化。

在技术 Spike 完成后再锁定 Python、OpenCV 和 Marker 库版本。

## 当前工具

- `environment_check.py`：核对 Python、OpenCV、NumPy、ArUco、配置和 OBS 安装状态。
- `generate_calibration_markers.py`：生成 ID 0–3 的70 mm校准卡和A4打印稿。
- `camera_probe.py`：列出视频输入，或进行无窗口的短时取流测试。
- `detect_markers.py`：从图片或摄像头读取 ArUco ID、像素中心和角度，并保存标注图与JSON。
- `calibrate_corners.py`：识别四角 Marker，计算透视矩阵，输出校准标注图、俯视矫正图和 Camera → Game 标准化坐标参数。
- `config/calibration.json`：四角顺序、打印尺寸、板面和视频输入基线。

## 一键配置

在仓库根目录运行：

```powershell
powershell -ExecutionPolicy Bypass -File scripts\setup_vision.ps1
```

生成文件位于 `hardware/printables/`。真实摄像头截图和检测结果默认写入 `data/raw/`，不会提交到 GitHub。

## 今天的最小验证

```powershell
.\.venv\Scripts\python.exe vision\environment_check.py
.\.venv\Scripts\python.exe vision\camera_probe.py --list
```

找到可用摄像头编号后：

```powershell
.\.venv\Scripts\python.exe vision\camera_probe.py --source 0 --seconds 3
.\.venv\Scripts\python.exe vision\detect_markers.py --camera 0
```

## 四角校准

把 ID 0、1、2、3 分别放在活动区域的左上、右上、右下、左下。四张 Marker 的中心共同定义游戏活动区域边界，然后运行：

```powershell
.\.venv\Scripts\python.exe vision\calibrate_corners.py --camera 1 --output-dir data\raw\calibration-tests\physical-01
```

也可以先用图片测试：

```powershell
.\.venv\Scripts\python.exe vision\calibrate_corners.py --image path\to\frame.png --output-dir data\raw\calibration-tests\image-01
```

输出包括：

- `calibration.json`：Camera → Board 透视矩阵、四角像素位置和坐标约定。
- `calibration_overlay.png`：四角顺序和有效区域检查图。
- `warped.png`：校正为 900 × 600 像素的俯视图。

归一化坐标原点在左上，X 向右、Y 向下，范围均为 0–1；接入 Unity 平面时建议映射为 `X = normalized.x`、`Z = normalized.y`。正式板面固定后必须重新运行并保存现场校准结果。
