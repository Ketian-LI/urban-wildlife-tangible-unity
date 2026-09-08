# Toolchain Baseline V0.1

更新日期：2026-09-08

## 已锁定

| 工具 | 项目版本 | 本机状态 | 选择理由 |
| --- | --- | --- | --- |
| Unity | 6000.3.4f1 | 已安装 | 属于 Unity 6.3 LTS，项目期间不跨大版本升级 |
| Python | 3.12.10 | 已安装 | 比本机默认 3.14 更成熟，且与选定 OpenCV/NumPy 有 Windows 预编译包 |
| OpenCV | opencv-contrib-python 4.14.0.94 | 已安装并通过 ArUco 测试 | 保留 4.x API，包含 ArUco 所需模块，不在 P0 中切换 OpenCV 5 |
| NumPy | 2.4.6 | 已安装并通过导入测试 | 与 Python 3.12 和 OpenCV 4.14 的依赖解析通过，避免使用刚发布的最新补丁作为首个基线 |
| Git | 2.51.0.windows.2 | 已安装 | 记录当前本机版本 |
| OBS Studio | 32.0.4 | 已安装于 `D:\obs-studio` | 首次 Pocket 3 采集测试时记录画布、输入设备、色彩与帧率设置 |

## Python 环境规则

项目必须使用仓库内 `.venv`，不要直接使用系统默认的 Python 3.14。

```powershell
py -3.12 -m venv .venv
.\.venv\Scripts\python -m pip install --upgrade pip
.\.venv\Scripts\python -m pip install -r requirements.txt
```

只安装 `opencv-contrib-python`，不要同时安装 `opencv-python`、headless 或其他 OpenCV wheel，以避免它们共同覆盖 `cv2` 命名空间。

安装后最低验证：

```powershell
.\.venv\Scripts\python -c "import cv2, numpy; print(cv2.__version__, numpy.__version__); print(cv2.aruco.getPredefinedDictionary(cv2.aruco.DICT_4X4_50))"
```

## Unity 版本规则

- 项目首次创建时明确使用 `6000.3.4f1`。
- 开发期间不切换到本机其他 Unity 版本。
- 只有出现阻塞性编辑器问题时才考虑在 Unity 6.3 LTS 内升级补丁，并在开发日志记录升级前后版本与原因。

## 依据

- [Unity 6 releases and support](https://unity.com/releases/unity-6/support)
- [Unity 6000.3.4f1 release page](https://unity.com/releases/editor/whats-new/6000.3.4f1)
- [opencv-contrib-python 4.14.0.94](https://pypi.org/project/opencv-contrib-python/4.14.0.94/)
- [NumPy releases](https://pypi.org/project/numpy/)
