# Unity P0 Input Project

这是可直接由 Unity Hub 打开的Unity 6工程，项目路径为仓库中的 `unity/`，编辑器版本固定为 `6000.3.4f1`。

当前主场景为 `Assets/Scenes/P0_InputSpike.unity`。场景只搭建输入闭环，包含 `Layout Input Manager` 和正交摄像机；Environment、NPC、Animal、Trace 与 Logger 会在输入协议稳定后逐层加入。

不要提交 `Library`、`Temp`、`Obj`、`Logs`、`UserSettings` 或本机构建输出。

## P0 输入约定

- Python 在 Confirm 后原子更新 `data/raw/layout-packets/latest_layout.json`。
- Unity Input Manager 只接受 `schema_version = 0.1` 和 `packet_type = confirmed_layout`。
- 若时间戳不比上次读取的新、JSON 不完整或版本不支持，则保持上一份有效布局并记录错误。
- `x_norm` 映射 Unity X，`y_norm` 映射 Unity Z；四角 Marker 不会出现在 Token 数组中。
- 正式字段定义见 `data/schemas/layout_packet_v0.1.schema.json`；隐私安全示例见 `docs/images/vision/layout-packet-validation/latest_layout.json`。

## 打开和验证

1. 在Unity Hub中选择 **Add project from disk**，打开本仓库的 `unity` 文件夹。
2. 确认编辑器版本为 `6000.3.4f1`，打开 `Assets/Scenes/P0_InputSpike.unity`。
3. 在仓库根目录用Python生成真实或测试用 `data/raw/layout-packets/latest_layout.json`。
4. 点击Play，在Hierarchy选择 `Layout Input Manager`，从 `P0CycleController` 组件菜单执行 **Confirm Current Plan**。
5. 识别数据有效时会生成黑色板面、洋红路径、3个Food与2个Woodland调试物件，并输出Constraint Check。所有约束通过后执行 **Start Run**；未通过则自动回到Plan。
6. 版本错误、旧时间戳、越界坐标、路径不连续、Token缺失或重复都会保留上一份有效布局并输出原因。

轮廓版无隐私输入样例位于 `docs/images/vision/layout-packet-contour-v02-validation/latest_layout.json`。编辑器批处理验证命令为：

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.4f1\Editor\Unity.exe" `
  -batchmode -nographics -projectPath ".\unity" `
  -executeMethod UrbanWildlife.EditorTools.P0InputSceneTools.BatchVerifyFixture `
  -quit -logFile ".\data\raw\unity-smoke.log"
```

成功日志包含：`UNITY_INPUT_SMOKE_OK tokens=5 path_points=11 backend=contour`。

## 已实现组件

- `LayoutPacketModels.cs`：与Python JSON对应的数据模型，支持路径二维数组。
- `LayoutPacketReader.cs`：读取原子文件，校验版本、时间戳、完整Token集合、数值范围和连续路径；只有全部通过才发布 `LayoutAccepted` 事件。
- `LayoutDebugView.cs`：把归一化坐标转换为9 × 6 Unity单位的俯视板面、路径与Token调试物件。
- `P0ScenarioModels.cs`、`P0ConstraintEvaluator.cs` 与 `P0ConstraintManager.cs`：读取S001参数并输出人类连通、动物可达、Food有效性和改动次数。
- `P0CycleStateMachine.cs` 与 `P0CycleController.cs`：管理Plan、Confirm、60秒Run、30秒Observe和每Session 3个周期；未通过约束不能进入Run。
- `P0InputSceneTools.cs`：可重复创建P0输入场景，并在批处理模式验证脱敏夹具、9个调试元素、S001初始失败状态及两次移动后的有效修复。

## 当前边界

- Unity目前把输入实例化为纯调试几何，还没有最终公园美术、碰撞或行为逻辑。
- `LayoutAccepted` 是下一步供 Environment Manager 与规划约束模块订阅的唯一入口。
- 当前Animal Reachable只适用于S001的单一内部池塘：人类路径增加成本但不封路；未来加入围栏或多个障碍时再升级为网格寻路。
- 当前阶段切换通过组件菜单验证，尚未制作屏幕按钮、倒计时文本和阶段面板。
- `Library`、`Temp`、`Obj`、`Logs`、`UserSettings` 和本机构建输出均不提交。
