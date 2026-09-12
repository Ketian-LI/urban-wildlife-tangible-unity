# P0 Electronic Vertical Slice Checklist

更新日期：2026-09-08

这份检查用于材料到货前验证Unity内部的完整交互闭环。电子布局只替代本轮输入，不证明固定俯拍、实体板、轮廓Token或实体彩带已经稳定。

## 三周期操作顺序

1. 用Unity `6000.3.4f1` 打开仓库中的 `unity/`，进入 `Assets/Scenes/P0_InputSpike.unity` 并点击Play。
2. 在Plan阶段按 `D`，或点击 **LOAD ELECTRONIC DEMO**。面板应提示当前周期的电子布局已经准备好。
3. 按空格或点击 **CONFIRM LAYOUT**。三项约束和改动预算应全部显示 `PASS`。
4. 约束通过后直接按空格或点击 **START RUN**。观察8个人类和当前轮11–14只动物在60秒Run中的移动、停留、进食、回避与实时轨迹。
5. Run结束后进入30秒Observe；此时代理和本轮轨迹保持最后状态。依次点击Human、Animal、Combined，确认显示切换正确。
6. 回到下一轮Plan后重新按 `D` 生成带新时间戳和正确周期号的布局，再重复步骤3至5。
7. 完成3轮后，阶段应进入Complete；如需新会话，点击 **RESET SESSION**。

## 屏幕验收

| 检查项 | 通过条件 |
| --- | --- |
| 布局 | 黑色90 × 60比例板面、单条洋红路径、3个Food和2个Woodland均出现 |
| 约束 | Human route、Animal route、Food hotspots和Changes全部为`PASS` |
| 人类 | `Walker 2 / Dweller 2 / Visitor 2 / Wheelchair user 2`；可看到通行、广场停留、Food访问和轮椅推进 |
| 动物 | 三轮依次为`P/S/F 7/3/1`、`9/4/1`、`8/3/2`；三种动物至少能进入觅食状态 |
| 差异 | 附近有人时，松鼠/狐狸可回避；鸽子不使用同一回避规则 |
| Trace | Run实时增长；Human冷色、Animal暖色；Combined同时显示；下一轮可见较淡的上一轮 |
| 时序 | Plan→Confirm→Run 60秒→Observe 30秒，3轮后Complete |
| 日志 | 会话目录同时出现可读取的`events.jsonl`与`events.csv` |

## 日志位置与检查

日志保存在：

```text
data/raw/research-logs/<electronic-demo-session>/events.jsonl
data/raw/research-logs/<electronic-demo-session>/events.csv
```

至少应包含 `layout_confirmed`、`constraint_evaluated`、`phase_changed` 和 `cycle_memory_recorded`。检查记录中的`cycle_index`依次为0、1、2，并确认人类行程、三物种进食、动物回避、轨迹点数和Human/Animal距离会随Run更新。`data/raw/`被Git忽略，不上传原始会话数据。

## 建议保留的非隐私证据

- Plan阶段且约束全部通过的截图。
- Run阶段同时出现人类和动物的截图。
- Complete阶段截图。
- 脱敏后的CSV字段标题或汇总截图。
- 任何布局遮挡、行为难以分辨或界面超出屏幕的失败记录。

通过本清单后，只能标记“电子Vertical Slice通过”。实体Vertical Slice仍需在到货后使用固定俯拍、真实四角Marker、实体Token和路径带重新执行。
