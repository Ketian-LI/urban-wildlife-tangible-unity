# GDD V3 Migration Plan

更新日期：2026-09-12

## 设计基线

项目的目标版本以 `Urban_Wildlife_Multispecies_City_GDD_CN_V3.0(1).docx` 为准。旧版公园 Vertical Slice 继续作为可运行技术底座，但不再作为最终玩法定义。迁移期间必须保持 `main` 可运行，并且只在对应模块通过自动验证后替换旧逻辑。

不可改变的原则：玩家规划城市条件而不直接控制动物；摄像头只产生待确认的 TokenState；Scan 结果必须经过 Preview 与 Confirm；机动车道路不再由绳索输入；Natural Food 必须存在；Roadkill 必须由车辆与地面动物实际碰撞产生；动物记忆、死亡与历史 Trace 不因拆除设施而自动清空。

## 分批实施顺序

| 批次 | 目标 | 主要交付 | 状态 |
| --- | --- | --- | --- |
| 0 | 对齐新版交互基线 | 移除 Run 前选择题和预测门槛；保留旧日志列以兼容已有记录 | 已完成 |
| 1 | 城市片区数据底座 | 建立 Building、GreenPatch、Road、Pedestrian、Waste 与 CityState 数据模型；保留旧场景适配层 | 已完成 |
| 2 | 五类实体 Token 与建设流程 | Apartment、Detached、Commercial、Community、Green Intervention；Scan City → Preview → Confirm Construction；New、Moved、Missing、Unchanged 差分 | 已完成 |
| 3 | 道路与步行网络 | 为新建筑生成 Direct、Existing-network、Low-impact 三条候选机动车路线；自动基础步行接入；屏幕编辑额外 Footpath | 下一步 |
| 4 | 人流与车辆 | 建筑 Origin/Destination、代表性 Trip、Walk/Drive、Destination Capacity、Crowding 与车辆代理 | 待开始 |
| 5 | 城市环境压力 | 三类 Green Patch、Natural/Anthropogenic Food、Bin、Bench、Waste Capacity、Overflow 与局部 Disturbance | 待开始 |
| 6 | 四物种与永久后果 | 鸽子、灰松鼠、狐狸、刺猬；Utility、性格、事件记忆、迁入迁出、真实碰撞 Roadkill | 待开始 |
| 7 | 五阶段与策略反馈 | DP、施工与拆除、Quiet/Active/Peak/Late、五个 Development Phase、五维 City Balance Score | 待开始 |
| 8 | Trace、事件与最终界面 | Human/Animal/Combined Trace、City Feed、阶段 Before/After、City Report、完整研究日志与城市 UI | 待开始 |

## P0 边界

第一轮可玩版本优先完成：单张城市片区地图、四类核心建筑、Marker Scan、道路候选、基础步行接入、三物种、人流、车辆、Roadkill、Green Patch、两类食物、基础记忆、Human/Animal Trace、三个 Development Phase、五维 City Balance Score 与基础日志。

刺猬、Green Intervention 三模式、Bench/Bin 完整规则、Waste Overflow、Commercial Peak、Community Event、Intoxicated/Conflict、五阶段、Combined Trace 与完整 City Report 在 P0 稳定后进入 P1。

## 批次 0 验收

- Confirm 阶段不再显示物种与压力区域选择题。
- 约束通过后 `START RUN` 立即可用，空格键也可直接开始。
- Observe 只陈述实际结果，不再显示“预测命中”或“预测不同”。
- 旧版 `predicted_top_feeder` 与 `predicted_conflict_area` 日志列暂时保留并写入 `Not set`，避免破坏已有 CSV/JSONL 读取流程；在城市日志 schema 升级时再统一移除。

## 批次 1 验收

- `CityState` V0.1 同时保存 Building、GreenPatch、VehicleRoad、PedestrianLink 与 WasteSystem，所有空间位置继续使用左上原点的归一化坐标。
- Building 固定支持 Apartment、Detached House、Commercial 与 Community Facility 四种核心类型，并保存人流、目的地、车辆、垃圾、食物、干扰和占地变量。
- GreenPatch 固定支持 Woodland、Shrub/Garden 与 Open Grass；`PublicPark` 是可叠加的土地属性，验证器要求城市仍保留 Natural Food。
- VehicleRoad 与 PedestrianLink 是两个独立网络，分别记录来源和连接建筑；新城市不会把实体彩带重新解释为机动车道路。
- `LegacyParkCityAdapter` 在旧 P0 约束检查旁路生成 CityState，不改变旧场景行为；后续批次用正式城市 Token 输入替换该适配层。
- JSON Schema 位于 `data/schemas/city_state_v0.1.schema.json`，Unity 自动检查 ID、坐标、引用、网络几何和数值范围。

## 批次 2 验收

- 城市实体库存固定为 Apartment 3、Detached House 4、Commercial 2、Community Facility 2、Green Intervention 3，共 14 个唯一 Token；ID 与类型不匹配时拒绝扫描。
- `city_token_scan` V0.1 保存校准 ID、稳定画面、识别后端、开发阶段以及每个 Token 的 ID、类型、归一化位置、角度和置信度；不稳定画面不得进入 Preview。
- `Scan City` 把当前扫描与最近一次确认快照比较为 New、Moved、Missing、Unchanged；位置容差为板面归一化 0.01，角度容差为 5°。
- New 生成 Proposed Construction；已确认 Token 被移动时必须先拆除，Missing 只提出 `Demolish?` 并阻止确认，不能自动删除城市对象；Unchanged 保持原状态。
- `Confirm Construction` 只提交无冲突的 New 项，更新 CityState revision，并为新建筑建立 Waste 输出节点；对象继续保持 Proposed，等待批次 3 选择机动车道路后再进入施工阶段。
- 脱敏电子夹具覆盖 Scan → Preview → Confirm、重复扫描、Moved 阻止、Missing 不自动删除、库存及稳定画面验证；本批完成数据与规则闭环，正式城市画面入口随道路批次接入。

## 每批通用完成条件

每批必须同时满足：Unity 编译通过、批处理烟雾测试通过、现有 Python 测试通过、关键行为可在 Play Mode 中观察、文档与日志字段同步更新，并且不把尚未实现的 GDD 内容标记为已完成。
