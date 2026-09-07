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
        ↓ JSON / UDP / OSC
Unity 2D 城市公园模拟
        ↓
研究日志与交互数据
```

## 仓库目录

```text
urban-wildlife-simulation/
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

- [ ] 确认 MVP 与明确不做项
- [ ] 固定摄像头、桌面与光照条件
- [ ] 稳定识别单个 Marker Token
- [ ] 识别高饱和彩绳并输出 Path Mask
- [ ] 完成 Camera 到 Game 的坐标映射
- [ ] 输出统一数据包并完成技术 Spike 演示

今天的工作记录见 [`docs/devlog/2026-09-07.md`](docs/devlog/2026-09-07.md)。

## 开发约定

- `main` 始终保持可演示或可恢复。
- 新功能使用 `feature/<short-name>` 分支。
- 修复使用 `fix/<short-name>` 分支。
- 提交信息使用简短动词开头，例如 `Add marker detection spike`。
- 每周至少更新一次开发日志，并记录失败尝试和 Scope 变化。
- 不要提交参与者身份信息、同意书、访谈原始文件、密钥或未脱敏研究数据。

## 许可

当前为未公开的毕业设计与研究项目，暂不附加开源许可。项目提交和数据脱敏完成后再决定是否公开及采用何种许可证。
