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
| 3 | 道路与步行网络 | 为新建筑生成 Direct、Existing-network、Low-impact 三条候选机动车路线；自动基础步行接入；屏幕编辑额外 Footpath | 已完成 |
| 4 | 人流与车辆 | 建筑 Origin/Destination、代表性 Trip、Walk/Drive、Destination Capacity、Crowding 与车辆代理 | 已完成 |
| 5 | 城市环境压力 | 三类 Green Patch、Natural/Anthropogenic Food、Bin、Bench、Waste Capacity、Overflow 与局部 Disturbance | 已完成 |
| 6 | 四物种与永久后果 | 鸽子、灰松鼠、狐狸、刺猬；Utility、性格、事件记忆、迁入迁出、真实碰撞 Roadkill | 机制已完成；最终动画待换皮 |
| 7 | 五阶段与策略反馈 | DP、施工与拆除、Quiet/Active/Peak/Late、五个 Development Phase、五维 City Balance Score | 机制已完成 |
| 8 | Trace、事件与最终界面 | Human/Animal/Combined Trace、City Feed、阶段 Before/After、City Report、完整研究日志与城市 UI | 数据/日志已完成；最终界面待完善 |

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

## 批次 3 验收

- 每栋需要机动车接入且尚未选路的 Proposed Building 自动获得 Direct、Existing-network、Low-impact 三个候选；候选连接最近合理的既有 Vehicle Road，而不是地图中心。
- Direct 优先最短连接，Existing-network 连接到最近既有网络端点，Low-impact 使用绿地代价采样选择绕行侧；候选同时公开预计长度、敏感绿地影响和交通压力，供后续 UI 比较。
- 所有新建筑必须各选一条机动车候选后才能确认；确认只提交被选路线，未选候选不进入 CityState。机动车接入与步行接入分别保存，不再把同一条线兼作两个网络。
- 每栋需要步行接入的新建筑自动获得 Basic Building Access；额外 Footpath 支持在屏幕 Preview 中新增、修改与删除，并以 `ScreenEdited` 标注，实体输入不读取道路或步行彩带。
- 路网确认作为一次事务更新 Building 引用与 CityState revision。屏幕 Footpath 删除时同步清除 Building 引用；验证失败不会部分写入城市。
- 当前路线生成器是可重复的几何投影与代价采样 V0.1；更复杂的隐藏 Grid/A*、施工时间与 DP 花费在后续批次接入，不提前声称为真实交通规划算法。

## 批次 4 验收

- 只有 `Existing` 住宅生成出行；Apartment 与 Detached House 按 Housing Capacity 和 Human Origin Rate 生成代表性居民，而不是把每个市民都画成一个 NPC。默认上限为24，接口强制不超过30。
- 每个 Trip 固定保存 Origin、Destination、Walk/Drive、代表人数、出发时间、停留时间与完整路网路线；代表性居民按1.5秒间隔进入，完成 Outbound → Dwelling → Returning → Complete。
- 目的地选择同时读取 Destination Weight、距离、Comfortable Capacity 和已分配代表人口；超过舒适容量后产生 Crowd Penalty，使后续居民倾向其他商业或社区设施。
- Walk 只使用 PedestrianLink，Drive 只使用 VehicleRoad；两套网络的接入线显式引用主网络。Drive Trip 才会产生 Vehicle Agent，车辆沿道路往返，不生成随机背景车。
- 当前人口、速度、容量、权重与选择公式均为确定性 V0.1 游戏参数，用于验证城市因果闭环，不表示真实伦敦人口或交通预测。独立 `City_Prototype` 场景已接入实时可视化集成壳，最终城市美术和正式摄像头工作流仍在后续批次完成。

## 城市集成检查点

- 在批次5前建立独立城市场景，避免已完成规则长期停留在不可见后台；旧公园Vertical Slice继续保留，城市场景不破坏回归底座。
- 左侧地图与右侧城市信息栏采用77/23固定分屏，UI不再覆盖地图。视觉层参考明亮城市规划桌游：浅蓝水岸、浅绿公共空间、浅灰道路、米白步行道及柔和建筑分类色。场景实时显示6栋建筑、机动车与步行网络、11名代表性居民、Drive车辆及目的地压力，并提供暂停、重启与Footpath显隐检查。
- 此检查点已接入环境压力、四物种、五阶段、城市平衡与观察数据，并已在右侧栏接通Scan → Preview → 选路 → DP → 施工控制流程。电子样例只作为实体板到货前的替代输入；动物仍以机制检查Token显示，Trace、City Feed、报告和最终建筑/角色美术尚未完成最终视觉呈现。

## 批次 5–8 机制验收

- 环境层把Natural Food与Anthropogenic Food分开，Bin提供容量并降低附近暴露食物，Bench增加局部人类活动；只有需求持续超过容量才生成Overflow与Litter Hotspot。
- 城市动物层默认演示8只鸽子、4只灰松鼠、2只狐狸和1只刺猬。Utility读取食物、庇护、人类干扰、交通风险、路程与正负记忆；记忆保留6–10条，负面记忆衰减更慢。
- Migration、死亡和Roadkill作为永久事件保存。Roadkill只由带ID的Vehicle Agent与地面动物实际距离碰撞触发；鸽子不进入地面碰撞判定，拆除设施不会删除动物记忆。
- 策略层使用DP和Time Block推进施工/拆除，速度只允许Pause、1x、2x；五阶段为Population Growth、Public Life、Waste Pressure、Mobility Pressure、Redevelopment。
- City Balance由Development、Accessibility、Waste Management、Habitat Connectivity、Wildlife Safety五项等权20%组成；最后一个Natural Food Patch禁止拆除。
- 观察层分开采样Human、Animal与Combined Trace，记录City Feed，输出阶段Before/After和JSONL/CSV城市日志；城市原型已渲染语义鞋印、轮迹与四种动物足迹，并显示最新Feed及阶段快照。统一等距资产与最终排版仍属于表现层工作。

## 每批通用完成条件

每批必须同时满足：Unity 编译通过、批处理烟雾测试通过、现有 Python 测试通过、关键行为可在 Play Mode 中观察、文档与日志字段同步更新，并且不把尚未实现的 GDD 内容标记为已完成。
