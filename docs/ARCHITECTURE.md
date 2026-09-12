# Technical Architecture V0.1

更新日期：2026-09-12

## 软件基线

- Unity `6000.3.4f1`。
- Python `3.12.10`，使用仓库内 `.venv`。
- `opencv-contrib-python==4.14.0.94`，只安装一个提供 `cv2` 命名空间的 OpenCV wheel。
- `numpy==2.4.6`。
- Git `2.51.0.windows.2`。
- OBS Studio `32.0.4`，已安装于 `D:\obs-studio`；首次 DJI Pocket 3 采集测试时补录具体场景与视频设置。
- Unity输入层使用官方 `com.unity.nuget.newtonsoft-json 3.2.2` 解析标准数据包与S001场景配置。

## GDD V3 城市状态底座

`CityState` V0.1 是新城市模拟的共享状态快照，契约见 `data/schemas/city_state_v0.1.schema.json`。它包含以下互相引用但职责分离的数据：

- `Building`：Apartment、DetachedHouse、Commercial、CommunityFacility，以及 Housing、Origin、Destination、Vehicle、Waste、Anthropogenic Food、Disturbance 与 Footprint 参数。
- `GreenPatch`：Woodland、ShrubGarden、OpenGrass，以及 PublicPark 土地属性、Shelter、Natural Food、Human Disturbance 和 Patch Size。
- `VehicleRoad`：主路、支路和建筑接入路；记录 Existing、Direct、ExistingNetwork、LowImpact 路线来源，但不接受实体彩带直接改路。
- `PedestrianLink`：既有人行网络、自动基础接入和屏幕新增 Footpath；与 VehicleRoad 分开保存。
- `WasteSystem`：总需求、总容量、Overflow 状态、建筑输出/Bin 节点和 Litter Hotspot。

`CityStateValidator` 在状态进入后续模拟前检查 schema、唯一 ID、归一化坐标、网络几何、建筑引用与数值范围，并要求至少一块 GreenPatch 保留 Natural Food。迁移期间，`LegacyParkCityAdapter` 只在旧 S001 约束检查旁路生成兼容 CityState；旧 Human/Animal 系统继续读取原 P0 数据，不会被未完成的城市功能打断。

### 城市 Token 扫描与建设事务

`city_token_scan` V0.1 是正式城市输入契约，JSON Schema 位于 `data/schemas/city_token_scan_v0.1.schema.json`。库存为 Apartment 100–102、Detached House 110–113、Commercial 120–121、Community Facility 130–131、Green Intervention 140–142，共14件；Unity同时校验ID/类型配对、校准坐标、稳定画面、角度、置信度和每类数量上限。

摄像头不逐帧直接改写城市。玩家触发 `Scan City` 后，`CityTokenDiffer` 将扫描与最近确认 Token 快照比较为 New、Moved、Missing、Unchanged；位置变化超过归一化0.01或角度变化超过5°才算Moved。`CityConstructionManager` 把New转换为Proposed Building或GreenPatch并检查边界与建筑占地重叠；Moved和Missing会阻止确认，Missing只显示拆除请求语义，不删除数据。成功 `Confirm Construction` 是一次事务：只追加New对象、建筑Waste输出节点并把CityState revision加一。选定道路后建筑与网络仍保持Proposed；后续施工时间与DP批次负责推进状态。

### 机动车与步行网络规划

`CityRoadCandidateGenerator` 为每栋尚未接入机动车网络的 Proposed Building 生成三条独立候选：Direct投影到最近的既有道路段，Existing-network连接最近的既有网络端点，Low-impact在直连两侧建立绕行点，并以Woodland/ShrubGarden采样代价选择影响较低的一侧。候选公开长度、敏感绿地影响与按建筑Vehicle Demand标定的交通压力。该实现是确定性的几何/代价采样V0.1，不宣称等同真实交通模型；需要更复杂地图时可将内部求解器替换为隐藏Grid+A*，而不改变候选数据接口。

`CityNetworkPlanningManager` 在Route Selection Preview中要求每栋建筑选且只选一个机动车候选。确认后只把选择结果写为`BuildingAccess` VehicleRoad，并更新Building引用。Pedestrian Network使用独立数组：系统同时为每栋新建筑生成最近既有人行网络的`BasicBuildingAccess`；玩家可在屏幕中新增、调整或删除`ScreenEdited ExtraFootpath`。确认是原子事务，删除ExtraFootpath时同步移除建筑引用；任何State验证失败都保留上一版CityState。正式输入中没有机动车或步行彩带字段。

### 代表性人流与车辆

`CityTripPlanner` 只读取 `Existing` Building。住宅的 Housing Capacity 与 Human Origin Rate 决定代表性居民数量，默认最多24个且公开每个代理代表的人数；Commercial 与 Community Facility 作为目的地。目的地效用由 Destination Weight、归一化距离、稳定微扰和预计 Crowd Penalty 组成，Capacity 以代表人口而非屏幕代理数量计算。

`CityNetworkRouteBuilder` 通过建筑接入线的 `connected_road_ids` 或 `connected_link_ids` 拼接主网络：Walk只查询PedestrianLink，Drive只查询VehicleRoad。`CityTripAgent` 使用同一路线完成Outbound、目的地Dwelling和反向Returning。`CityMobilitySimulation` 按1.5秒间隔生成代理；Drive状态才公开对应Vehicle Agent，因此车辆数量可追溯到具体Trip，不存在随机背景交通。当前公式和速度是确定性玩法V0.1，不是人口或交通预测模型。

## 已确定的实体输入基线

- 60 × 90 cm 黑色磁吸板，横向平放。
- 黑色磁吸板直接作为地图底图；固定地点使用局部低黏度哑光可移除贴膜，不增加整张地图覆盖层。
- 摄像头从正上方垂直俯拍，首轮输入为 1920 × 1080、30 fps；预估高度为板面上方 70 至 100 cm，最终由镜头视角测试确定。
- P0 主摄像头为 DJI Pocket 3，通过 USB 接入 OBS；iPhone 13 作为备用输入。
- 地图板放在桌面，落地式俯拍支架独立放在桌后地面，以减少桌面操作带来的画面振动。
- 四角透视校准采用 ArUco `DICT_4X4_50`，固定使用 ID 0、1、2、3。
- Woodland 的B款120 × 90 mm叶片毛毡只承担区域与触感表达；视觉系统读取中央刚性木座的缺口轮廓，不尝试从布料轮廓判断ID或角度。
- Woodland 中央刚性木座固定为直径 50 mm、厚 3–4 mm 的圆形桦木片；外层不规则毛毡最大宽度约 120 mm。
- Woodland 逻辑ID 20–21由50 mm中央木座的方向缺口与C、A+C编码槽区分，木座顶部不贴视觉码。
- Woodland 四角磁吸底座只负责铺平和定位，不作为额外视觉输入；系统状态仍由中央 Marker 唯一确定。
- 每个 Woodland 毛毡锚点使用约 15 × 15 mm 薄塑料底座和 10 × 10 mm 方形磁吸片；这些部件隐藏在布面下方，算法不读取其位置。
- P0逻辑ID固定为Food Hotspot 10–12、Woodland 20–21；其值由轮廓缺口恢复，P1预留30–43。
- Food Hotspot Token 的实体主体固定为最大外径60 mm的A款柔和六边形3–4 mm桦木片；视觉系统读取木片外缘的方向缺口与编码缺口。
- Food Hotspot逻辑ID 10–12由六边形木片的方向缺口与A、B、A+B编码槽区分；最大直径同时作为类型复核。
- Planned Human Path 的实体彩带仅保留为旧 P0 技术验证材料；GDD V3 城市版机动车道路由 Unity 提供候选路线，额外 Footpath 在屏幕编辑，不再把彩带作为正式输入。
- 板面左右各使用一盏 5000–5600 K 柔光灯，以约 45°照射；完成校准后尽量固定曝光和白平衡。
- 四角使用固定且 ID 不同的白底校准 Marker。
- 旧 P0 测试时玩家操作 Token 与彩带并在双手离开后确认读取；正式城市版只扫描建筑与 Green Intervention Token。

## 数据流

```text
Camera Frame
   ├─ Corner ArUco detector → calibration quadrilateral
   ├─ Contour token detector → logical id, x, y, angle
   └─ HSV path detector → path mask / polyline
                 ↓
Coordinate normalizer
                 ↓
Versioned input packet
                 ↓
Unity Input Manager
   ├─ Legacy P0 systems（迁移期间继续运行）
   ├─ LegacyParkCityAdapter → CityState V0.1
   └─ CityTokenScanReader → Scan City → Difference Preview
                                      ↓ Confirm Construction
                                 Proposed CityState
                                      ↓ Route Selection Preview
                         ┌────────────┴────────────┐
                         ↓                         ↓
             selected VehicleRoad       automatic/screen PedestrianLink
                              ↓
                  Building / GreenPatch / Networks / Waste
                               ↓
              Representative Trip / Walk / Drive / Crowd
                               ↓
Environment, Human NPC, Vehicle and Animal systems
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
  "capture": {
    "mode": "stable_camera",
    "stable": true,
    "stable_seconds_required": 1.0,
    "frames_observed": 34
  },
  "recognition": {
    "token_backend": "contour",
    "token_config_version": "0.2"
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
    "path_continuous": true,
    "required_token_ids": [10, 11, 12, 20, 21],
    "missing_token_ids": [],
    "duplicate_token_ids": [],
    "all_required_tokens_detected": true
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
- `P0CycleScore`在Run中另行计算规划表现分，不改变Constraint Check。它以成功个体数而非事件总数归一化：人类40分、三物种各20分，四项封顶后合计100。
- Constraint Check UI 只返回 `human_connected`、`animal_reachable`、`food_hotspot_valid` 和 `changes_used / changes_allowed`。
- Constraint Check 不返回预测动物路线、拥堵量或最终行为；这些信息只能由 Run 结果和 Trace 呈现。
- Run开始时，Human Route Planner把确认后的单条路径转为四类路线：Walker直接通行，Dweller在中央广场停留，Visitor绕行至Food Hotspot后回到主路，Wheelchair User以较慢速度完整通行主路。
- Human Agent State Machine只使用Waiting、Moving、Dwelling、Visiting与Finished等可记录状态；完成路线的代理在同一次Run中重新进入，以持续形成活动和干扰条件。
- S001 V0.2使用2个Walker、2个Dweller、2个Visitor和2个Wheelchair User；数量、速度及停留时间属于可版本化的游戏参数，不作为真实人流预测。
- Animal Environment Planner把3个活动节点与2个Woodland分配给鸽子、松鼠和狐狸；每只动物由独立状态机处理发现、接近、进食、停留、回避和撤退，同群个体用确定性小范围偏移避免完全重叠。
- 鸽子V0.1不因人类接近而回避；松鼠和狐狸按不同干扰半径撤回Woodland，松鼠进食后增加有上限的熟悉度。它们是为了形成可观察差异的设计抽象，不是生态预测模型。
- P0使用同一张“周末公园重新规划”地图完成三轮递进Brief：公园修复、周末野餐压力和黄昏最终提案；每轮读取入口、广场、出口、主路径、Woodland 与活动节点的空间关系。
- 入口 A 和出口 B 是 6 × 8 cm 圆角固定区域，分别贴合左、右边界，中心距上均为 30 cm；路径端点必须落入对应区域。中央广场使用 20 × 14 cm 横向椭圆固定区域，中心坐标为距左 45 cm、距上 18 cm，不通过 Marker 移动。
- 在以左上为原点、地图宽高归一化为 1 的坐标中，广场中心为 `(0.50, 0.30)`，完整形状作为静态场景数据保存。
- 小型池塘为约 18 × 12 cm 的不规则横向固定区域，中心距左 50 cm、距上 42 cm；其归一化中心约为 `(0.556, 0.70)`，不通过 Marker 识别，也不计入玩家改动次数。
- Constraint Manager 将池塘标记为人类、松鼠和狐狸的不可通行区；鸽子可在飞行路径中跨越，但池塘不提供落脚点或 Food Hotspot。
- 首轮约束只实现连续性、可达性、路径占用和最多 3 个逻辑元素调整。`changes_used` 通过比较候选布局与上次确认快照计算：每个发生变化的 Token 计 1，路径几何发生变化计 1，磁吸锚点不单独计数。
- 每个场景开始前由主持人摆放预设基准布局；首个 Plan 以该布局为比较基准，后续 Re-plan 以上次确认快照为基准。
- S001 的基准 Token 坐标、路径折线和预期 Constraint Check 由 `data/scenarios/s001_weekend_park_baseline.json` 提供，Unity 与测试代码读取同一份场景数据，避免文档和实现各自维护坐标。
- 当前P0数据包中的强制主路必须单条连续、按入口 A → 中央广场 → 出口 B 的顺序通过，且不得自我交叉。可选支路将在第二种色带与数据格式验证后另加字段，不复用或破坏当前主路字段。
- 四角各保留 8 × 8 cm 校准安全区；Confirm 时若 Marker、Token 或路径被遮挡则不接受输入。
- Food Hotspot 的人类活动影响距离和主路径安全间距作为场景参数保存，不硬编码在 Marker 检测模块中。
- HSV 路径检测先以亮洋红色为目标候选，并与高饱和青色、橙色实拍比较后锁定最终色相与阈值；黑色板面、灰绿色 Woodland 和灰蓝色池塘均应排除在路径 Mask 外。
- 固定地图贴膜属于预先登记的静态区域，不作为玩家输入；其低饱和颜色还应通过 HSV 饱和度门限与路径色带分离。

## 交互时序与研究日志基线

- P0 Confirm 使用屏幕大号按钮，并提供空格键快捷键；触发后要求画面连续稳定约 1 秒再输出 JSON。
- 预测试默认 Plan 不倒计时、Run 60 秒、Observe 约 30 秒，每个 Session 3 个周期；正式研究参数可在预测试后调整并记录版本。
- `P0CycleController` 实现阶段门控：约束失败返回Plan，成功后才可从Confirm进入Run；Run与Observe到时自动推进，第三周期后进入Complete。
- `P0CycleMechanics`保存三轮情境与7/3/1、9/4/1、8/3/2的动物群体配置。旧预测字段暂保留为日志兼容字段，但不再显示选择题，也不参与Run门控。
- `P0CycleMechanics`还保存每轮结束时的不可变结果快照；运行逻辑只读取最近一轮，完整列表保留到Reset Session。快照包含人类行程、分物种进食、回避、最常使用活动节点、松鼠平均熟悉度以及Human/Animal累计轨迹距离。
- 结果快照同时冻结`P0CycleScore`分项与总分。Run界面从实时代理状态计算预览分，Observe和日志读取冻结分，避免阶段结束后的实例清除改变结果。
- `P0ControlPanel` 提供大号阶段按钮、空格快捷键、约束反馈和倒计时；它只调用控制器公开动作，不绕过布局或约束门控。
- 最小日志包含 `session_id`、`cycle_index`、时间戳、输入布局、约束结果、改动元素、动物状态变化、关键事件和 Trace 摘要。
- 默认不记录参与者姓名；如后续保存视频或其他可识别资料，必须另行经过研究伦理和同意流程。
- `P0ResearchLogger`订阅`LayoutAccepted`、`ConstraintsEvaluated`与`PhaseChanged`，将布局确认、约束检查和阶段变化追加到会话日志。
- 每条记录包含UTC时间、会话/周期/情境/阶段、布局时间戳、三项约束、改动预算、三种动物数量、人类完成行程、各物种进食次数、动物回避次数、分项/总分、轨迹采样点数、Human/Animal累计距离和实际读取的上一轮记忆字段。旧预测列暂写入`Not set`以兼容已有记录。
- 输出位于被Git忽略的 `data/raw/research-logs/<session_id>/events.jsonl` 和 `events.csv`。JSONL保留机器可读结构，CSV用于快速检查与分析。

## 模块边界

- `vision/` 只负责读取实体状态、校准、稳定和输出标准数据。
- `build_layout_packet.py` 默认在四角矫正图上使用V0.2轮廓Token后端；旧ArUco Token后端只用于回归测试。两个后端输出相同Token字段。
- `unity/` 只消费标准数据，不直接依赖摄像头实现；`LayoutPacketReader` 校验版本、新时间戳、稳定确认标记、完整P0 Token集合、坐标范围与连续路径后才发布 `LayoutAccepted` 事件。
- `P0ElectronicDemoInput`仅在材料到货前从脱敏样例生成新的标准数据包；它复用正式Reader、约束、模拟和日志链路，但必须标记为`image_input`，不得作为实体识别证据。
- `data/` 只定义可公开的数据结构和脱敏样例。
- `unity/` 中的 Constraint Manager 负责规划限制与通行检查；Human 和 Animal 系统仍负责产生实际行为结果。
- `HumanRoutePlanner` 只从已确认布局和S001参数生成路线，`HumanAgentStateMachine` 负责确定性移动/停留，`P0HumanSimulation` 负责Run阶段的Unity实例与可视化。
- `AnimalEnvironmentPlanner`负责布局到群体动物资源关系的确定性映射，`AnimalAgentStateMachine`不依赖Unity场景对象，`P0AnimalSimulation`按当前轮配置生成11–14个实例并只负责运行期感知、实例和可视化。
- Run进入Observe时，`P0AnimalSimulation`在实例被清除前汇总最常使用的活动节点和松鼠熟悉度并写入`P0CycleMechanics`。下一轮Planner将存在节点记忆的前半数代理分配回同一Token ID，并把熟悉度保留与回避产生的谨慎系数写入新代理配置。
- `P0TraceSeries`按0.08 Unity单位的最小间距采样位置并累积距离；Human与Animal Simulation各自维护LineRenderer。人物每次从入口重新开始时新建轨迹段，Run/Observe保留当前轨迹，下一轮将最近一轮复制为低透明度历史层。显示开关只影响渲染，不改变状态机、距离统计或日志。
- 动物显示层与行为状态机保持分离：`P0AnimalSimulation`从`Resources/UrbanWildlife/Animals`加载透明俯视Sprite，并只根据状态机结果处理平滑转向、待机摆动、进食脉动与状态色；替换美术不会改变生态规则或研究日志。
- V0.2松鼠和狐狸通过透明轮廓顶点的宽高比进行显示层回归检查：松鼠使用宽圆C形侧尾，狐狸使用细长直尾、黑腿和白色尾尖；检查只保护地图尺寸下的物种可辨性，不参与行为判定。
- 动物位置仍完全由状态机决定；表现层以8 FPS采样共用待机、转身、行走、进食、坐下和起身节奏。鸽子、松鼠和狐狸的Walking与Feeding各使用6张独立Sprite；行走帧直接绘制两腿或四肢的交替步态，启用时关闭整只动物的程序缩放和抬升，不改变生态逻辑。
- S001环境显示层从`Resources/UrbanWildlife/Environment`加载3:2城市公园V0.3底图；红砖街区、铺装、围栏、路灯、植被和池塘只建立空间语境。入口/出口仍有运行时门体提示；广场与池塘不再叠加黄色或青色轮廓，规划判定始终读取场景JSON中的精确坐标，底图像素不参与规则判断。
- 人类显示层从`Resources/UrbanWildlife/Humans`按Archetype加载透明Sprite。`HumanAgentStateMachine`继续决定位置和状态；Walker、Dweller和Visitor使用独立步行帧，Wheelchair User使用4张侧视推进帧。Dweller坐下/起身和Visitor喂食继续选择各自独立动作帧，其余动作保留共用离散关键帧与回退图。
- Walker、Dweller与Visitor统一采用高角度斜俯视正脸Sprite，眼、鼻和嘴在地图尺寸下可读；脸与鞋尖均朝源图下方。P0HumanSimulation先加入固定180°朝向补偿，再按移动方向整体旋转Sprite，不单独改变脸、腿或脚。
- 角色显示层以约1 Unity单位对应2米的代表性体长作为相对比例基准：Walker 0.89、Dweller 0.84、Visitor 0.85、Wheelchair User 0.80、鸽子0.30、松鼠0.44、狐狸0.72。该值只缩放Sprite，不进入路线速度、干扰半径、碰撞或生态判定。
- `LayoutDebugView`把每次确认布局组织为7个语义根对象：1个地图、1条路径和5个Token；装饰子对象不再影响输入验收计数。运行期创建的Mesh和Material在重载布局时显式释放。
- `LayoutDebugView`的V0.3装饰层包括Food活动点与中心铭牌、Woodland冠层/叶脉/木纹，以及入口和出口的双门柱与弧形门槛；其父级坐标继续由已确认布局和S001固定场景参数驱动。
- Planned Human Path在视觉层中由同一识别Polyline实时生成暖灰砂砾步道：深色土肩、浅色石质路缘、砂砾表面和确定性碎石细节均作为路径根对象的子层。实体洋红带及HSV Mask保持不变，显示宽度不参与约束判定。
- `P0ControlPanel`在Plan/Confirm显示Brief、完整约束和操作，在Run/Observe切换为运行信息与因果回顾卡；这是显示密度变化，不改变阶段状态机。
- `P0ConstraintManager` 当前实现入口→广场→出口、人类活动来源、路径安全间距、池塘避让和改动次数；动物可达性以S001只有一个不接触边界的池塘为前提，路径只增加成本而不封路。新增围栏或多个障碍时应替换为网格寻路。
- `P0ResearchLogger`只消费公开事件和汇总计数；原始研究日志写入Git忽略目录，必须标注会话和时间，不记录不必要的身份信息。
