# Project Scope V0.1

更新日期：2026-09-07

## 项目目标

完成一个实体桌面与 Unity 2D 相连的可研究原型。玩家通过 Token 与彩色路径改变城市公园的条件，系统让人类 NPC 与动物自主响应，并通过 Trace 呈现累积后果。

## P0 MVP

- 单张 2D 公园地图。
- 固定俯视摄像头与可重复的四角校准。
- Marker Token 的 ID、位置和角度识别。
- 彩色路径的 HSV 分割、去噪与 Path Mask 输出。
- Camera 到 Game 的坐标标准化。
- Food 与 Woodland 两类核心条件。
- Walker、Dweller 或 Visitor 等基础人类行为。
- 鸽子、松鼠和狐狸三种动物的基础状态机。
- Plan、Confirm、Run、Observe 的完整单周期。
- 最小可用的研究日志输出。

## P1 Final Target

- Bench、Human Activity、Bin 与 Management Sign。
- Human Trace、Animal Trace 与 Combined Trace。
- 刺猬作为第 4 物种。
- 6 至 8 个系统事件。
- Session Summary、声音和视觉完善。

P1 功能只在 P0 技术闭环和 Vertical Slice 稳定后进入开发。

## 明确不做

- 不制作真实伦敦公园的精确生态预测模型。
- 不制作以唯一正确答案或总分为目标的教育软件。
- 不允许玩家直接拖拽、生成或控制动物。
- 不在 MVP 阶段制作多地图、联网、多玩家或复杂 3D 内容。
- 不在核心循环稳定前增加新物种、事件或装饰系统。

## Scope 变更规则

1. 新功能必须说明它支持哪一个研究问题或验收目标。
2. 若硬里程碑延期超过 3 天，优先删除 P1 功能。
3. 每次删减或调整都记录在开发日志中，作为 Research Through Design 的过程证据。
4. 2026-10-25 后不增加核心功能，只修复严重问题和可理解性问题。
