# Technical Architecture V0.1

## 软件基线

- Unity `6000.3.4f1`。
- Python `3.12.10`，使用仓库内 `.venv`。
- `opencv-contrib-python==4.14.0.94`，只安装一个提供 `cv2` 命名空间的 OpenCV wheel。
- `numpy==2.4.6`。
- Git `2.51.0.windows.2`。
- OBS Studio `32.0.4`，已安装于 `D:\obs-studio`；首次 DJI Pocket 3 采集测试时补录具体场景与视频设置。

## 已确定的实体输入基线

- 60 × 90 cm 黑色磁吸板，横向平放。
- 黑色磁吸板直接作为地图底图；固定地点使用局部低黏度哑光可移除贴膜，不增加整张地图覆盖层。
- 摄像头从正上方垂直俯拍，首轮输入为 1920 × 1080、30 fps；预估高度为板面上方 70 至 100 cm，最终由镜头视角测试确定。
- P0 主摄像头为 DJI Pocket 3，通过 USB 接入 OBS；iPhone 13 作为备用输入。
- 地图板放在桌面，落地式俯拍支架独立放在桌后地面，以减少桌面操作带来的画面振动。
- 四角透视校准采用 ArUco `DICT_4X4_50`，固定使用 ID 0、1、2、3。
- Woodland 的软布只承担区域与触感表达；视觉系统读取其中央刚性木座上的 ArUco Marker，不尝试从布料轮廓判断 ID 或角度。
- Woodland 中央刚性木座固定为直径 50 mm、厚 3–4 mm 的圆形桦木片；外层不规则毛毡最大宽度约 120 mm。
- Woodland 的 ID 20–21 分别使用 30 × 30 mm ArUco 编码区，安装在中央木座顶部居中的 40 × 40 mm 白色哑光标签上；若实拍识别不稳定，优先增大编码区再调整木座。
- Woodland 四角磁吸底座只负责铺平和定位，不作为额外视觉输入；系统状态仍由中央 Marker 唯一确定。
- 每个 Woodland 毛毡锚点使用约 15 × 15 mm 薄塑料底座和 10 × 10 mm 方形磁吸片；这些部件隐藏在布面下方，算法不读取其位置。
- P0 Token 映射固定为 Food Hotspot ID 10–12、Woodland ID 20–21；P1 使用预留 ID 30–43。
- Food Hotspot Token 的实体主体固定为直径 60 mm 的圆形 3–4 mm 桦木片；视觉识别仍以其顶部 ArUco Marker 为准。
- Food Hotspot 的 ID 10–12 分别使用 35 × 35 mm ArUco 编码区，安装在顶部居中的 45 × 45 mm 白色哑光标签上；识别参数以实际打印尺寸和实拍测试为准。
- Planned Human Path 的实体输入使用约 15 mm 宽、100 cm 有效长度的高饱和哑光罗纹带；首选亮洋红色，同时测试青色和橙色。先准备 120 cm 材料并保留 20 cm 备用；薄磁吸点只用于固定，不作为视觉输入。
- 板面左右各使用一盏 5000–5600 K 柔光灯，以约 45°照射；完成校准后尽量固定曝光和白平衡。
- 四角使用固定且 ID 不同的白底校准 Marker。
- 玩家操作 Token 与彩绳，双手离开后再确认读取。

## 数据流

```text
Camera Frame
   ├─ Marker detector → token id, x, y, angle
   └─ HSV path detector → path mask / polyline
                 ↓
Coordinate normalizer
                 ↓
Versioned input packet
                 ↓
Unity Input Manager
                 ↓
Environment, Human NPC and Animal systems
                 ↓
Trace renderer and Research Logger
```

## P0 布局数据包 V0.1

```json
{
  "schema_version": "0.1",
  "packet_type": "confirmed_layout",
  "timestamp_ms": 1788883200000,
  "timestamp_utc": "2026-09-08T16:00:00Z",
  "session_id": "pilot-001",
  "cycle_index": 0,
  "scenario_id": "S001",
  "calibration_id": "local-test",
  "coordinate_system": {
    "origin": "top_left",
    "x_axis": "right",
    "y_axis": "down",
    "range": [0.0, 1.0],
    "unity_plane_mapping": "x_norm -> Unity X; y_norm -> Unity Z"
  },
  "tokens": [
    {
      "id": 10,
      "type": "food_hotspot",
      "x_norm": 0.5,
      "y_norm": 0.5,
      "angle_deg": 0,
      "in_bounds": true,
      "confidence": 1.0
    }
  ],
  "path": {
    "format": "polyline",
    "preset": "magenta",
    "points_norm": [[0.033, 0.5], [0.5, 0.3], [0.967, 0.5]],
    "point_count": 3,
    "continuous": true,
    "component_count": 1,
    "mask_area_fraction": 0.02
  },
  "validation": {
    "all_tokens_in_bounds": true,
    "path_detected": true,
    "path_continuous": true
  }
}
```

数据契约见 `data/schemas/layout_packet_v0.1.schema.json`。P0 由 Python 在 Confirm 后原子写入 `latest_layout.json`，Unity 只读取完整文件，同时将每次输入按 Session、Cycle 和时间戳归档。坐标固定为左上原点、X向右、Y向下的0–1范围，Unity 平面映射为 X/Z。

## 规划约束与因果链

```text
Planning brief and limits
          ↓
Food Hotspot, Woodland and planned path layout
          ↓
Human route connectivity and activity simulation
          ↓
Food availability and disturbance fields
          ↓
Animal reachability and autonomous behavior
          ↓
Observed crossings, feeding, waiting and avoidance
```

- Food Hotspot 是由人类停留、野餐、投喂或掉落食物形成的资源可获得性条件，不是直接生成动物结果的按钮。
- Food Hotspot validity 检查其中心是否位于中央广场内，或距 Planned Human Path 中心线不超过 10 cm；同时要求 Token 边缘距路径边缘至少 1 cm、不得与池塘重叠且完整位于板面内。
- Human connectivity 检查入口和必要活动目标之间是否存在连续路径。
- Animal reachability 检查 Woodland 到 Food Hotspot 是否可达，并把人流密度转换为路径成本，而不是把人类路径视为绝对墙体。
- Constraint Check 读取每局预算、最多改动次数和空间禁放条件；它不计算唯一正确答案或总分。
- Constraint Check UI 只返回 `human_connected`、`animal_reachable`、`food_hotspot_valid` 和 `changes_used / changes_allowed`。
- Constraint Check 不返回预测动物路线、拥堵量或最终行为；这些信息只能由 Run 结果和 Trace 呈现。
- P0 首个 Brief 固定为“周末公园重新规划”，并要求读取入口、广场、出口、主路径、Woodland 与 Food Hotspot 的空间关系。
- 入口 A 和出口 B 是 6 × 8 cm 圆角固定区域，分别贴合左、右边界，中心距上均为 30 cm；路径端点必须落入对应区域。中央广场使用 20 × 14 cm 横向椭圆固定区域，中心坐标为距左 45 cm、距上 18 cm，不通过 Marker 移动。
- 在以左上为原点、地图宽高归一化为 1 的坐标中，广场中心为 `(0.50, 0.30)`，完整形状作为静态场景数据保存。
- 小型池塘为约 18 × 12 cm 的不规则横向固定区域，中心距左 50 cm、距上 42 cm；其归一化中心约为 `(0.556, 0.70)`，不通过 Marker 识别，也不计入玩家改动次数。
- Constraint Manager 将池塘标记为人类、松鼠和狐狸的不可通行区；鸽子可在飞行路径中跨越，但池塘不提供落脚点或 Food Hotspot。
- 首轮约束只实现连续性、可达性、路径占用和最多 3 个逻辑元素调整。`changes_used` 通过比较候选布局与上次确认快照计算：每个发生变化的 Token 计 1，路径几何发生变化计 1，磁吸锚点不单独计数。
- 每个场景开始前由主持人摆放预设基准布局；首个 Plan 以该布局为比较基准，后续 Re-plan 以上次确认快照为基准。
- S001 的基准 Token 坐标、路径折线和预期 Constraint Check 由 `data/scenarios/s001_weekend_park_baseline.json` 提供，Unity 与测试代码读取同一份场景数据，避免文档和实现各自维护坐标。
- P0 路径必须单条连续、按入口 A → 中央广场 → 出口 B 的顺序通过，且不得自我交叉。
- 四角各保留 8 × 8 cm 校准安全区；Confirm 时若 Marker、Token 或路径被遮挡则不接受输入。
- Food Hotspot 的人类活动影响距离和主路径安全间距作为场景参数保存，不硬编码在 Marker 检测模块中。
- HSV 路径检测先以亮洋红色为目标候选，并与高饱和青色、橙色实拍比较后锁定最终色相与阈值；黑色板面、灰绿色 Woodland 和灰蓝色池塘均应排除在路径 Mask 外。
- 固定地图贴膜属于预先登记的静态区域，不作为玩家输入；其低饱和颜色还应通过 HSV 饱和度门限与路径色带分离。

## 交互时序与研究日志基线

- P0 Confirm 使用屏幕大号按钮，并提供空格键快捷键；触发后要求画面连续稳定约 1 秒再输出 JSON。
- 预测试默认 Plan 不倒计时、Run 60 秒、Observe 约 30 秒，每个 Session 3 个周期；正式研究参数可在预测试后调整并记录版本。
- 最小日志包含 `session_id`、`cycle_index`、时间戳、输入布局、约束结果、改动元素、动物状态变化、关键事件和 Trace 摘要。
- 默认不记录参与者姓名；如后续保存视频或其他可识别资料，必须另行经过研究伦理和同意流程。

## 模块边界

- `vision/` 只负责读取实体状态、校准、稳定和输出标准数据。
- `unity/` 只消费标准数据，不直接依赖摄像头实现。
- `data/` 只定义可公开的数据结构和脱敏样例。
- `unity/` 中的 Constraint Manager 负责规划限制与通行检查；Human 和 Animal 系统仍负责产生实际行为结果。
- 研究日志必须标注版本、会话和时间，不记录不必要的身份信息。
