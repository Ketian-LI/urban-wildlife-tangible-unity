# OBS 俯拍输入基线

## 已确认的软件位置

- OBS Studio：`D:\obs-studio\bin\64bit\obs64.exe`
- 第一轮输入：1920 × 1080、30 fps
- 主摄像头：DJI Pocket 3
- 备用摄像头：iPhone 13

## 首次连接时的设置顺序

1. 打开 OBS，新建场景 `Overhead Camera Test`。
2. 添加“视频采集设备”，先选择当前能够连接的摄像头。
3. 将分辨率/FPS类型改为“自定义”。
4. 设置分辨率为 `1920 × 1080`，FPS 为 `30`。
5. 让画面覆盖整个画布，不裁掉四角 Marker。
6. 关闭自动曝光和自动白平衡之前，先记录自动模式下的可用数值；锁定后的最终数值需在正式板面到货后确定。
7. 双手离开板面，确认四个角都清晰、无遮挡、不过曝。
8. 保存一张截图到 `data/raw/camera-test/`。该目录不会上传到 GitHub。

## 本地测试命令

列出可访问的视频输入：

```powershell
.\.venv\Scripts\python.exe vision\camera_probe.py --list
```

对编号 0 的输入运行3秒测试：

```powershell
.\.venv\Scripts\python.exe vision\camera_probe.py --source 0 --seconds 3
```

读取一帧并识别 Marker：

```powershell
.\.venv\Scripts\python.exe vision\detect_markers.py --camera 0
```

摄像头编号可能不是0。先运行 `--list`，再使用能够返回画面的编号。
