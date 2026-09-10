# Unity P0 Input Project

这是可直接由 Unity Hub 打开的Unity 6工程，项目路径为仓库中的 `unity/`，编辑器版本固定为 `6000.3.4f1`。

当前主场景为 `Assets/Scenes/P0_InputSpike.unity`。场景包含布局输入、约束、回合控制、基础人类代理、三种动物状态机和最小研究日志；Trace 会在技术闭环稳定后继续加入。

不要提交 `Library`、`Temp`、`Obj`、`Logs`、`UserSettings` 或本机构建输出。

## P0 输入约定

- Python 在 Confirm 后原子更新 `data/raw/layout-packets/latest_layout.json`。
- Unity Input Manager 只接受 `schema_version = 0.1` 和 `packet_type = confirmed_layout`。
- Unity只接受带有 `capture.stable = true` 的确认数据；实时摄像头包由Python先通过约1秒稳定门控。
- 若时间戳不比上次读取的新、JSON 不完整或版本不支持，则保持上一份有效布局并记录错误。
- `x_norm` 映射 Unity X，`y_norm` 映射 Unity Z；四角 Marker 不会出现在 Token 数组中。
- 正式字段定义见 `data/schemas/layout_packet_v0.1.schema.json`；隐私安全示例见 `docs/images/vision/layout-packet-validation/latest_layout.json`。

## 打开和验证

1. 在Unity Hub中选择 **Add project from disk**，打开本仓库的 `unity` 文件夹。
2. 确认编辑器版本为 `6000.3.4f1`，打开 `Assets/Scenes/P0_InputSpike.unity`。
3. 在仓库根目录用Python生成真实或测试用 `data/raw/layout-packets/latest_layout.json`。
   如需立刻查看完整行为，无需手动复制文件：在Play后的Plan阶段按 `D`，或点击 **LOAD ELECTRONIC DEMO**。系统会从脱敏样例生成当前周期的新数据包，因此连续3轮不会被旧时间戳拦截。
4. 点击Play，使用左上控制面板的大号 **CONFIRM LAYOUT** 按钮，或按一次空格键。
5. 识别数据有效时会生成S001手绘公园地图、由确认Polyline实时生成的写实砂砾步道、3个柔和六边形Food与2个叶片式Woodland规划标记，并在面板显示Constraint Check。实体路径仍使用洋红罗纹带完成HSV识别；Unity中的土肩、石质路缘与碎石只属于显示层。地图同时用精确矢量边界标出入口A、出口B、广场和池塘，规则不依赖背景图像近似位置。所有约束通过后点击 **START RUN** 或再按一次空格；未通过则自动回到Plan。
6. Run显示60秒倒计时，随后自动进入30秒Observe；三个周期结束后可用 **RESET SESSION** 重新开始。
   Run中会出现2个Walker、2个Dweller和2个Visitor：Walker沿主路通行，Dweller在广场停留并播放坐下/起身，Visitor绕行至Food Hotspot并播放喂食循环。人物表现层采用8 FPS离散关键帧：3帧待机、3帧转身、6帧行走、6帧喂食、4帧坐下和4帧起身。Run与Observe期间控制面板自动收起为紧凑信息卡，减少对地图的遮挡。
7. 同一Run会生成鸽子、松鼠和狐狸各1只。三者使用透明背景的手绘侧视Sprite，并按接近真实的相对体长显示；移动折返时先播放3帧转身，行走使用6帧节奏，Feeding状态使用6帧进食循环，Resting与离开食物分别使用4帧坐下/起身姿态。鸽子直接利用Food并短暂停留；松鼠与狐狸在人类进入各自干扰半径时回避并退回Woodland，松鼠进食后会积累有限的熟悉度。
8. 会话事件写入 `data/raw/research-logs/<session_id>/events.jsonl` 与 `events.csv`。该目录不会提交Git，只记录会话ID、阶段、约束及汇总行为次数，不含参与者姓名。
9. 版本错误、旧时间戳、未通过稳定门控、越界坐标、路径不连续、Token缺失或重复都会保留上一份有效布局并输出原因。

轮廓版无隐私输入样例位于 `docs/images/vision/layout-packet-contour-v02-validation/latest_layout.json`。编辑器批处理验证命令为：

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.4f1\Editor\Unity.exe" `
  -batchmode -nographics -projectPath ".\unity" `
  -executeMethod UrbanWildlife.EditorTools.P0InputSceneTools.BatchVerifyFixture `
  -quit -logFile ".\data\raw\unity-smoke.log"
```

成功日志包含输入、约束、人类、动物、研究日志、回合和UI标记。其中新增的关键标记为 `UNITY_ANIMAL_ROUTE_SMOKE_OK species=3`、`UNITY_ANIMAL_STATE_SMOKE_OK` 与 `UNITY_RESEARCH_LOG_SMOKE_OK`。

## 已实现组件

- `LayoutPacketModels.cs`：与Python JSON对应的数据模型，支持路径二维数组。
- `LayoutPacketReader.cs`：读取原子文件，校验版本、时间戳、完整Token集合、数值范围和连续路径；只有全部通过才发布 `LayoutAccepted` 事件。
- `P0ElectronicDemoInput.cs`：在材料到货前，从脱敏夹具原子生成带新时间戳、Session和Cycle的电子演示包；不产生实体识别已经通过的主张。
- `LayoutDebugView.cs`：把归一化坐标转换为9 × 6 Unity单位的俯视板面、路径与Token调试物件。
- `Assets/Resources/UrbanWildlife/Environment/`：S001的3:2手绘公园底图、完整生成提示词与来源说明；地图规则仍来自JSON并由精确边界叠加显示。
- `Assets/Resources/UrbanWildlife/Humans/`：Walker、Dweller、Visitor透明俯视Sprite、背景提取记录与完整提示词。
- `P0ScenarioModels.cs`、`P0ConstraintEvaluator.cs` 与 `P0ConstraintManager.cs`：读取S001参数并输出人类连通、动物可达、Food有效性和改动次数。
- `P0CycleStateMachine.cs` 与 `P0CycleController.cs`：管理Plan、Confirm、60秒Run、30秒Observe和每Session 3个周期；未通过约束不能进入Run。
- `P0ControlPanel.cs`：提供大号阶段按钮、空格快捷键、周期/倒计时和四项约束反馈。
- `HumanRoutePlanner.cs`：把确认路径、中央广场与Food Hotspot组合为Walker、Dweller和Visitor路线。
- `HumanAgentStateMachine.cs`：以确定性状态管理进入、移动、广场停留、Hotspot访问和路线完成。
- `SteppedCharacterAnimation.cs`：定义人类与动物共用的8 FPS离散动画采样器，以及待机、转身、行走、喂食/进食、坐下和起身六类动作的关键帧数量与姿态。
- `P0HumanSimulation.cs`：在Run阶段生成并更新6个人类Sprite代理，把路线状态映射为逐帧转身、行走、坐下、喂食和起身；Observe保留最后状态，Plan清除上一轮实例。
- `AnimalEnvironmentPlanner.cs`：把3个Food与2个Woodland映射为鸽子、松鼠和狐狸的起点、资源点及庇护点。
- `AnimalAgentStateMachine.cs`：实现觅食、停留、回避人类、退回庇护地及松鼠熟悉度等确定性状态。
- `P0AnimalSimulation.cs`：在Run阶段生成3只动物、读取最近人类距离、更新状态和调试颜色，把移动、Resting与Feeding映射为共用逐帧表现，并汇总进食与回避次数。
- `Assets/Resources/UrbanWildlife/Animals/`：鸽子、松鼠和狐狸的透明手绘俯视Sprite、生成提示词与素材来源说明。
- `P0AnimalSpriteImporter.cs`：统一环境、人物和动物Sprite的透明度、单Sprite、双线性过滤、Clamp、尺寸上限与压缩导入规则。
- `P0ResearchLogger.cs` 与 `ResearchLogRecord.cs`：将布局确认、约束和阶段事件追加为隐私安全的JSONL/CSV。
- `P0InputSceneTools.cs`：可重复创建P0输入场景，并在批处理模式验证脱敏夹具、9个调试元素、S001初始失败状态及两次移动后的有效修复。

## 当前边界

- Unity已完成可测试的逐帧动画节奏样板。当前六种动作使用离散变形关键帧，并复用每个角色已有的两张行走图；最终艺术方向锁定后仍需把关键动作替换为逐张绘制的独立Sprite。拥堵寻路和Trace尚未实现。
- 动物参数是为了验证因果链和交互节奏的P0设计抽象，不是伦敦公园的生态预测。
- 当前Animal Reachable只适用于S001的单一内部池塘：人类路径增加成本但不封路；未来加入围栏或多个障碍时再升级为网格寻路。
- 当前控制面板使用Unity内置即时GUI完成可操作原型；正式视觉语言和无障碍测试留到核心行为闭环稳定后处理。
- `Library`、`Temp`、`Obj`、`Logs`、`UserSettings` 和本机构建输出均不提交。
