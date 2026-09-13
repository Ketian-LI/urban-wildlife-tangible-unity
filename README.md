# Urban Wildlife Simulation

目标版本是一个以实体建筑 Token 进行城市规划、以 Unity 实时模拟人流、车辆与城市野生动物的多物种城市建设即时策略游戏。

玩家可以重新配置人类活动、资源、植被与空间路径，但不能直接控制动物。系统通过 Human Trace 与 Animal Trace 呈现干预随时间累积的结果。

> 2026-09-12 起，设计基线切换为 GDD V3.0。当前可运行的公园 Vertical Slice 将按 [`docs/GDD_V3_MIGRATION.md`](docs/GDD_V3_MIGRATION.md) 分批迁移为城市片区版本，未完成的新版系统不会提前标记为已实现。

## 核心研究问题

实体交互模拟如何使人类对城市野生动物生存条件的塑造变得可感知，并支持玩家反思由这种关系产生的责任？

## 核心原则

- 玩家改变条件，而不是直接控制动物。
- 规划路径不等于实际行动轨迹。
- 后果会随多轮干预累积。
- Trace 用于解释过程，不用于给玩家打道德分数。
- 生态关系需要有依据；为了交互而进行的简化必须记录。

## 系统结构

```text
实体桌面与 Token
        ↓
俯视摄像头
        ↓
OpenCV 识别与坐标标准化
        ↓ versioned JSON
CityState：建筑、绿地、道路、步行与垃圾
        ↓
Unity 2D 城市片区模拟
        ↓
研究日志与交互数据
```

## 仓库目录

```text
urban-wildlife-tangible-unity/
├─ .github/             GitHub 任务与问题模板
├─ docs/                Scope、架构、风险与研究日志
├─ hardware/            桌面、摄像头、Token 与校准记录
├─ unity/               Unity 项目与游戏逻辑
├─ vision/              OpenCV、Marker、彩绳与坐标映射
├─ data/                数据格式说明；原始研究数据不提交
├─ tests/               技术 Spike 与验收测试
├─ .gitignore
└─ README.md
```

## 当前阶段

第 1 周目标是完成技术闭环并冻结第一阶段 Scope：

- [x] 确认 MVP 与明确不做项
- [x] 建立 Python/OpenCV/ArUco 可重复运行环境
- [ ] 固定摄像头、桌面与光照条件
- [ ] 稳定识别单个无贴纸轮廓 Token
- [x] 识别高饱和彩绳并输出 Path Mask（电子效果图软件预验证）
- [x] 完成 Camera 到 Game 的坐标映射（电子稿软件预验证）
- [x] 输出统一数据包并完成电子夹具技术 Spike 演示
- [x] 建立Unity 6工程并完成轮廓版JSON读取烟雾测试
- [x] 接入Walker、Dweller、Visitor与Wheelchair User四类基础人类行为（各2名）
- [x] 接入鸽子、松鼠、狐狸三种基础动物状态机
- [x] 完成三种动物的手绘俯视Sprite与程序动画V0.1
- [x] 完成手绘公园地图、四类人物Sprite与整体显示层V0.2
- [x] 完成人类与动物六类动作的8 FPS逐帧节奏样板
- [x] 接入三种动物进食、Walker行走、Dweller坐下/起身与Visitor喂食的独立逐帧动画素材
- [x] 将Unity底图更新为有红砖街区、围栏、路灯、城市铺装与管理型池塘的城市公园V0.3
- [x] 移除广场与池塘的黄/蓝提示圈，以铺装、岸线与植被直接表达固定区域
- [x] 完成三类步行人物的等高等基线行走、Wheelchair User四帧推进，并接入三种动物的前后肢交替行走帧
- [x] 完成Plan、Confirm、Run、Observe单周期与最小研究日志输出
- [x] 将三轮升级为公园修复、周末压力与黄昏提案，并接入7–9只鸽子、3–4只松鼠和1–2只狐狸
- [x] 移除旧版Run前双项预测选择题；约束通过后可直接Run，旧日志列暂保留兼容
- [x] 建立 CityState V0.1 城市数据底座、验证器、JSON Schema 与旧 S001 适配层
- [x] 建立14件五类城市Token库存与 Scan City → Preview → Confirm Construction 规则闭环
- [x] 接入 New、Moved、Missing、Unchanged 差分；Missing 只请求拆除确认且不自动删除
- [x] 为新建筑生成Direct、Existing-network、Low-impact三条机动车道路候选并自动建立基础步行接入
- [x] 支持在屏幕Preview中新增、修改和删除额外Footpath；机动车道路不再读取实体彩带
- [x] 接入由建筑Origin/Destination产生的代表性人类Trip和车辆Trip、容量拥挤分流与往返状态
- [x] 建立独立 `City_Prototype` 可视化场景，将左侧完整地图与右侧固定信息栏分屏，并接入建筑、双路网、居民和车辆实时检查
- [x] 接入自然/人类食物、长椅/垃圾桶、容量超载、垃圾热点与局部干扰的城市环境压力闭环
- [x] 接入鸽子、灰松鼠、狐狸、刺猬四物种 Utility、6–10条事件记忆、迁入迁出及车辆实际碰撞 Roadkill
- [x] 接入 DP、施工/拆除时长、Quiet/Active/Peak/Late、五个发展阶段与五维 City Balance
- [x] 建立城市 Human/Animal/Combined Trace、City Feed、阶段 Before/After 报告及隐私安全 JSONL/CSV 日志框架
- [x] 接入跨周期滚动记忆：活动节点回访、松鼠熟悉度保留、谨慎度变化与可见日志
- [x] 接入按成功个体比例计算的100分规划表现分，并在Run实时更新、Observe冻结记录
- [x] 接入实时Human/Animal/Combined Trace、上一轮淡化叠加以及轨迹距离日志
- [x] 将连续色带升级为人类鞋印、鸽子鸟爪、松鼠小爪和狐狸大爪，并完成公园现场告示牌式UI
- [ ] 用固定俯拍、实体板和实物Token完成Vertical Slice现场验证

工作记录见 [`docs/devlog/2026-09-07.md`](docs/devlog/2026-09-07.md)、[`docs/devlog/2026-09-08.md`](docs/devlog/2026-09-08.md)、[`docs/devlog/2026-09-09.md`](docs/devlog/2026-09-09.md)、[`docs/devlog/2026-09-10.md`](docs/devlog/2026-09-10.md)、[`docs/devlog/2026-09-11.md`](docs/devlog/2026-09-11.md)、[`docs/devlog/2026-09-12.md`](docs/devlog/2026-09-12.md) 和 [`docs/devlog/2026-09-13.md`](docs/devlog/2026-09-13.md)。
材料到货前的完整软件演示步骤见 [`docs/VERTICAL_SLICE_CHECKLIST.md`](docs/VERTICAL_SLICE_CHECKLIST.md)。
已确认的城市公园方向见 [`docs/images/concepts/art-direction-drafts-2026-09-10/option-h-urban-neighbourhood-park-v01.png`](docs/images/concepts/art-direction-drafts-2026-09-10/option-h-urban-neighbourhood-park-v01.png)；当前Unity使用不含固定人物、动物和路线的V0.3底图，动态内容仍由运行时生成。
新版城市最终视觉基准另见 [`docs/visual-concepts/city-visual-direction-v02-soft-isometric.png`](docs/visual-concepts/city-visual-direction-v02-soft-isometric.png)：柔和等距微缩城市、圆润低多边形、薄荷绿/粉蓝/暖白与轻柔阴影。当前阶段先完成机制；概念图中的新增建筑不会在 Scan → Preview → Confirm → 选路 → 施工完成前出现在正式地图。

## 第一天交付

- [`docs/PROJECT_SCOPE.md`](docs/PROJECT_SCOPE.md)：Scope Freeze V1 与 P0 验收边界。
- [`docs/HARDWARE_SOFTWARE_CHECKLIST.md`](docs/HARDWARE_SOFTWARE_CHECKLIST.md)：设备、材料与软件的确认及待验证状态。
- [`docs/RESEARCH_LOG.md`](docs/RESEARCH_LOG.md)：研究问题、核心设计原则与 V1 到 V2 叙事。
- [`docs/RISK_REGISTER.md`](docs/RISK_REGISTER.md)：风险清单 V0.1 与触发信号。
- [`docs/DECISIONS.md`](docs/DECISIONS.md)：截至 2026-09-07 的已确定决策。
- [`docs/SCENARIOS.md`](docs/SCENARIOS.md)：P0 场景 S001 与首轮约束。

## 开发约定

- `main` 始终保持可演示或可恢复。
- 新功能使用 `feature/<short-name>` 分支。
- 修复使用 `fix/<short-name>` 分支。
- 提交信息使用简短动词开头，例如 `Add marker detection spike`。
- 每周至少更新一次开发日志，并记录失败尝试和 Scope 变化。
- 不要提交参与者身份信息、同意书、访谈原始文件、密钥或未脱敏研究数据。
- 网页实验 Demo 保留在独立仓库，本仓库只记录最终实体与 Unity 原型。

## 许可

当前为未公开的毕业设计与研究项目，暂不附加开源许可。项目提交和数据脱敏完成后再决定是否公开及采用何种许可证。
