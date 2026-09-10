# Urban Wildlife Simulation

一个以实体 Token 和彩色路径作为输入、以数字公园中的人类与动物自主行为作为反馈的城市生态桌面游戏。

玩家可以重新配置人类活动、资源、植被与空间路径，但不能直接控制动物。系统通过 Human Trace 与 Animal Trace 呈现干预随时间累积的结果。

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
Unity 2D 城市公园模拟
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
- [x] 接入Walker、Dweller、Visitor三类基础人类行为
- [x] 接入鸽子、松鼠、狐狸三种基础动物状态机
- [x] 完成三种动物的手绘俯视Sprite与程序动画V0.1
- [x] 完成手绘公园地图、三类人物Sprite与整体显示层V0.2
- [x] 完成人类与动物六类动作的8 FPS逐帧节奏样板
- [x] 完成Plan、Confirm、Run、Observe单周期与最小研究日志输出
- [ ] 用固定俯拍、实体板和实物Token完成Vertical Slice现场验证

工作记录见 [`docs/devlog/2026-09-07.md`](docs/devlog/2026-09-07.md)、[`docs/devlog/2026-09-08.md`](docs/devlog/2026-09-08.md)、[`docs/devlog/2026-09-09.md`](docs/devlog/2026-09-09.md) 和 [`docs/devlog/2026-09-10.md`](docs/devlog/2026-09-10.md)。
材料到货前的完整软件演示步骤见 [`docs/VERTICAL_SLICE_CHECKLIST.md`](docs/VERTICAL_SLICE_CHECKLIST.md)。
当前Unity显示层组合预览（三类人物与动物采用接近真实的相对大小，规划路径显示为写实砂砾公园步道）见 [`docs/images/unity/visual-layer-v06-realistic-road-preview.png`](docs/images/unity/visual-layer-v06-realistic-road-preview.png)。

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
