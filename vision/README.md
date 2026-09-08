# Vision

这里放置摄像头、Marker、彩绳和坐标映射代码。

第一周建议按以下顺序验证：

1. 摄像头稳定取流。
2. 四角 ArUco 校准，以及单个轮廓编码 Token 的逻辑ID、位置和角度。
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
- `detect_path.py`：按 HSV 颜色预设分割彩色路径，进行形态学去噪并输出 Path Mask、检查图和JSON统计。
- `build_layout_packet.py`：从同一帧提取四角校准、Token 和路径，原子写入 Unity 使用的 `latest_layout.json` 并按 Session/Cycle 归档。
- `generate_layout_fixture.py`：生成不含真实环境与个人信息的完整流程测试画面。
- `detect_contour_tokens.py`：在900 × 600矫正板面中识别无贴纸轮廓 Token 的逻辑ID、中心、标准化坐标和方向。
- `generate_contour_token_fixture.py`：生成5个轮廓编码和一个干扰亮块的无隐私验证画面。
- `config/calibration.json`：四角顺序、打印尺寸、板面和视频输入基线。
- `config/path_detection.json`：亮洋红、青色与橙色路径的初始 HSV 阈值和去噪参数。
- `config/contour_tokens.json`：无贴纸 Token 的直径、方向缺口、三位轮廓编码与实物验收阈值。
- `config/contour_tokens_v0.2.json`：已选A款Food＋B款Woodland造型、毫米尺寸、轮廓编码与毛毡磁吸点位置。

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

## 彩色路径分割

默认检测亮洋红色，也可以使用青色或橙色预设：

```powershell
.\.venv\Scripts\python.exe vision\detect_path.py --camera 1 --preset magenta --output-dir data\raw\path-tests\physical-01
.\.venv\Scripts\python.exe vision\detect_path.py --image path\to\frame.png --preset cyan --output-dir data\raw\path-tests\image-01
```

输出包括二值 `path_mask.png`、叠加检查图 `path_overlay.png` 和面积、边界框等统计信息 `path_detection.json`。目前阈值是软件预设；彩带、黑色板面和固定灯光同时到位后，需要分别实测三个候选颜色，再锁定唯一的 P0 颜色与阈值。

## 统一 Unity 数据包

在 Plan 完成、双手离开并点击 Confirm 后，对一张稳定画面运行：

```powershell
.\.venv\Scripts\python.exe vision\build_layout_packet.py --camera 1 --session-id pilot-001 --cycle-index 0 --scenario-id S001 --path-preset magenta
```

程序用四角 ID 0–3完成透视矫正，再从矫正图读取无贴纸轮廓Token，把它们转换为0–1板面坐标和角度，并将最大路径区域采样为标准化折线。默认使用 `contour_tokens_v0.2.json`；旧ArUco Token夹具仍可通过 `--token-backend aruco` 回归。缺少或重复任一ID 10、11、12、20、21时不会确认布局。默认输出：

- `data/raw/layout-packets/latest_layout.json`：Unity 只读取这个完整文件。
- `data/raw/layout-packets/archive/<session>/...json`：每个周期的不可变记录。
- `path_mask.png`、`packet_overlay.png`、`calibration_overlay.png`：本地 Debug 证据，不提交真实参与者画面。

`latest_layout.json` 使用“先完整写临时文件、再原子替换”的方式，避免 Unity 读到半份 JSON。正式数据契约见 `data/schemas/layout_packet_v0.1.schema.json`。当前折线采样针对 P0 单条、从左至右且不自交的路径约束。

## 无贴纸轮廓 Token

轮廓检测必须在四角校正后的900 × 600图像上运行：

```powershell
.\.venv\Scripts\python.exe vision\detect_contour_tokens.py --image path\to\warped.png --output-dir data\raw\contour-token-tests\physical-01
```

输出 `candidate_mask.png`、`contour_overlay.png` 和 `contour_detections.json`。电子夹具已经覆盖5个ID和8个旋转角；这只能证明算法与几何编码闭环，木件反光、刀缝、木纹、阴影及真实毛毡背景仍需实测。
