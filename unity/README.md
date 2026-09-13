# Unity P0 Input Project

这是可直接由 Unity Hub 打开的Unity 6工程，项目路径为仓库中的 `unity/`，编辑器版本固定为 `6000.3.4f1`。

当前保留两个场景：`Assets/Scenes/P0_InputSpike.unity` 是旧公园回归底座；`Assets/Scenes/City_Prototype.unity` 是新版城市整合场景，优先用于检查建筑、双路网、代表性居民/车辆、环境压力、四物种、五阶段与城市观察数据。

不要提交 `Library`、`Temp`、`Obj`、`Logs`、`UserSettings` 或本机构建输出。

## P0 输入约定

- Python 在 Confirm 后原子更新 `data/raw/layout-packets/latest_layout.json`。
- Unity Input Manager 只接受 `schema_version = 0.1` 和 `packet_type = confirmed_layout`。
- Unity只接受带有 `capture.stable = true` 的确认数据；实时摄像头包由Python先通过约1秒稳定门控。
- 若时间戳不比上次读取的新、JSON 不完整或版本不支持，则保持上一份有效布局并记录错误。
- `x_norm` 映射 Unity X，`y_norm` 映射 Unity Z；四角 Marker 不会出现在 Token 数组中。
- 正式字段定义见 `data/schemas/layout_packet_v0.1.schema.json`；隐私安全示例见 `docs/images/vision/layout-packet-validation/latest_layout.json`。

## 打开和验证

如需检查新版框架，打开 `Assets/Scenes/City_Prototype.unity` 后点击Play。左侧77%为独立地图视口，右侧23%为可滚动的固定城市信息栏，不发生遮挡。点击 **SCAN CITY · ELECTRONIC TEST** 可依次检查Preview、三种道路候选、DP提交、Pause/1x/2x与跨Time Block施工；电子夹具只是实体板到货前的替代输入。Preview与施工中只显示青色占地/线路提示，第二个施工时段完成后才生成正式城市对象。地图会滚动保留最多900个人类鞋印、车辆双轮迹、鸟爪、松鼠小爪、狐狸大爪和刺猬短点印；右侧可切换People/Wildlife/All，并查看最新City Feed与阶段Balance变化。场景基线仍有6栋既有建筑、8条机动车路段、8条步行连接、11名代表性居民（代表132人）和15只四物种动物；Drive Trip才生成车辆。

1. 在Unity Hub中选择 **Add project from disk**，打开本仓库的 `unity` 文件夹。
2. 确认编辑器版本为 `6000.3.4f1`，打开 `Assets/Scenes/P0_InputSpike.unity`。
3. 在仓库根目录用Python生成真实或测试用 `data/raw/layout-packets/latest_layout.json`。
   如需立刻查看完整行为，无需手动复制文件：在Play后的Plan阶段按 `D`，或点击 **LOAD ELECTRONIC DEMO**。系统会从脱敏样例生成当前周期的新数据包，因此连续3轮不会被旧时间戳拦截。
4. 点击Play，使用左上控制面板的大号 **CONFIRM LAYOUT** 按钮，或按一次空格键。
5. 识别数据有效时会生成S001城市公园地图、由确认Polyline实时生成的写实砂砾步道、3个柔和六边形Food与2个叶片式Woodland规划标记，并在面板显示Constraint Check。底图用红砖街区、铺装、黑色围栏与路灯明确城市语境；实体路径仍使用洋红罗纹带完成HSV识别，Unity步道只属于显示层。广场和池塘由底图中的铺装、岸线与植被表达，不再叠加突兀的黄圈或蓝圈；规划规则继续读取场景JSON中的精确坐标。所有约束通过后点击 **START RUN** 或再按一次空格；未通过则自动回到Plan。
6. 约束通过后 **START RUN** 立即启用，不再要求回答预测选择题。Run显示60秒倒计时、实时规划表现分和实时轨迹；控制面板可切换Human、Animal、Combined三种轨迹视图。随后自动进入30秒Observe并保留本轮轨迹，方便以冷色人类线和暖色动物线检查实际压力交叠。下一轮保留低透明度的上一轮轨迹作为对照；Reset会清空跨周期记忆和历史轨迹。
   Run中会出现2个Walker、2个Dweller、2个Visitor和2个Wheelchair User。Walker、Dweller和Visitor在道路移动状态中以4 FPS播放经核对的左右交替姿态；Dweller在广场停留并以4张Sprite坐下/起身，Visitor绕行Food Hotspot并播放6张喂食Sprite。Wheelchair User以较慢速度完整通行主路，使用右侧面4帧“发力—回手—发力—回手”推进循环，运行时按方向水平翻转。四类人物覆盖不同年龄、性别、肤色和行动方式，均使用相同的公园调色与木制桌游Token边框。Run与Observe期间控制面板自动收起为紧凑信息卡，减少对地图的遮挡。
7. 三轮依次生成鸽子/松鼠/狐狸7/3/1、9/4/1和8/3/2只。三者使用透明背景的手绘侧视Sprite，并按接近真实的相对体长显示；移动折返时先播放3帧转身，行走使用各自6张独立Sprite。鸽子的两腿、松鼠与狐狸的前后肢均按左右/对角步态前后交替。三种动物的Feeding状态也使用各自6张独立Sprite。上一轮成功使用最多的活动节点会吸引下一轮至少一半同物种代理回访；松鼠保留80%熟悉度，松鼠与狐狸还会依据上一轮回避次数形成最高25%的谨慎距离增幅。
8. 规划表现分满分100：至少完成一次通行的人类比例占40分，至少进食一次的鸽子、松鼠、狐狸比例各占20分；重复事件不刷分，回避不直接扣分。会话事件写入 `data/raw/research-logs/<session_id>/events.jsonl` 与 `events.csv`，包含分项、总分、公式版本、Human/Animal轨迹采样点数和累计移动距离；该目录不会提交Git，也不含参与者姓名。
9. 版本错误、旧时间戳、未通过稳定门控、越界坐标、路径不连续、Token缺失或重复都会保留上一份有效布局并输出原因。

轮廓版无隐私输入样例位于 `docs/images/vision/layout-packet-contour-v02-validation/latest_layout.json`。编辑器批处理验证命令为：

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.4f1\Editor\Unity.exe" `
  -batchmode -nographics -projectPath ".\unity" `
  -executeMethod UrbanWildlife.EditorTools.P0InputSceneTools.BatchVerifyFixture `
  -quit -logFile ".\data\raw\unity-smoke.log"
```

成功日志包含输入、城市建设、路网、城市出行、约束、人类、动物、研究日志、回合、轨迹和UI标记。其中关键标记为 `UNITY_CITY_CONSTRUCTION_SMOKE_OK inventory=14 types=5`、`UNITY_CITY_NETWORK_SMOKE_OK proposed_buildings=4 route_candidates=3_each`、`UNITY_CITY_MOBILITY_SMOKE_OK origins=2 representative_agents=6 represented_population=72`、`UNITY_ANIMAL_ROUTE_SMOKE_OK species=3 agents=11 feeding_agents=11`、`UNITY_OPTIONAL_PREDICTION_SCHEMA_SMOKE_OK required=0`、`UNITY_CYCLE_MEMORY_SMOKE_OK`、`UNITY_SCORE_SMOKE_OK`、`UNITY_TRACE_SMOKE_OK` 与 `UNITY_RESEARCH_LOG_SMOKE_OK`。

## 已实现组件

- `CityStateModels.cs`：统一保存四类核心 Building、三类 GreenPatch、独立 VehicleRoad/PedestrianLink 网络与 WasteSystem。
- `CityStateValidator.cs`：验证城市状态版本、ID、坐标、数值、引用关系和 Natural Food 保留原则。
- `LegacyParkCityAdapter.cs`：在不改变现有 P0 行为的情况下，把旧 S001 布局旁路映射为 CityState；正式城市输入接入后移除该过渡职责。
- `CityTokenScanModels.cs`、`CityTokenInventory.cs` 与 `CityTokenScanReader.cs`：定义五类14件实体库存，读取并验证稳定的城市扫描、校准信息、Token位置、角度、置信度及ID/类型对应关系。
- `Construction/`：比较最近确认快照，输出New/Moved/Missing/Unchanged Preview，验证建筑占地，并只把可确认的New项目追加为Proposed Construction；Missing不自动删除。
- `Networks/CityRoadCandidateGenerator.cs`：把新建筑投影到最近既有机动车/步行网络，生成Direct、Existing-network、Low-impact三种候选及自动Basic Building Access。
- `Networks/CityNetworkPlanningManager.cs`：要求每栋建筑选一条机动车路线，管理屏幕额外Footpath的新增、修改与删除，并用单次事务更新路网和建筑引用。
- `Mobility/CityTripPlanner.cs`：从已建住宅生成最多24个代表性居民，按权重、距离与拥挤惩罚选择目的地，并在独立网络间选择Walk或Drive。
- `Mobility/CityNetworkRouteBuilder.cs`：沿Building Access与主网络拼接归一化路线，步行和机动车路线互不混用。
- `Mobility/CityTripAgent.cs` 与 `CityMobilitySimulation.cs`：分批生成Trip，运行Outbound、Dwelling、Returning、Complete状态；只有Drive Trip产生Vehicle Agent。
- `Environmental/`：同步Building与Bin垃圾节点，生成两类食物、持续Overflow、Litter Hotspot和Green Patch局部压力。
- `Ecology/`：提供四物种Profile、Utility选点、6–10条事件记忆、基础占地避让、迁入迁出及实际Vehicle碰撞Roadkill。
- `Strategy/`：管理DP、施工/拆除项目、四个Time Block、五个Development Phase和五维等权City Balance。
- `Planning/CityPlanningWorkflow.cs`：把稳定扫描、差异Preview、建设确认、道路选择、DP预检和跨时段施工串成单一可操作状态机。
- `Reporting/`：采样Human/Animal/Combined Trace，生成City Feed与阶段Before/After报告，并输出城市JSONL/CSV日志。
- `Reporting/CityTraceVisualizer.cs`：把Trace快照绘制为滚动鞋印、双轮迹、鸟爪、小/大爪和刺猬点印，最多显示最新900个标记并支持三层切换。
- `Prototype/CityPrototypeStateFactory.cs` 与 `CityPrototypeDemo.cs`：提供可重复的城市整合夹具和实时表现层；分屏显示既有基线建筑、双路网、人流、车辆、环境压力、四物种和阶段反馈。
- `CityPrototypeSceneTools.cs`：用菜单创建、打开并自动验证 `City_Prototype` 场景，不替换旧P0回归场景。
- `LayoutPacketModels.cs`：与Python JSON对应的数据模型，支持路径二维数组。
- `LayoutPacketReader.cs`：读取原子文件，校验版本、时间戳、完整Token集合、数值范围和连续路径；只有全部通过才发布 `LayoutAccepted` 事件。
- `P0ElectronicDemoInput.cs`：在材料到货前，从脱敏夹具原子生成带新时间戳、Session和Cycle的电子演示包；不产生实体识别已经通过的主张。
- `LayoutDebugView.cs`：把归一化坐标转换为9 × 6 Unity单位的俯视板面、路径与Token调试物件。
- `Assets/Resources/UrbanWildlife/Environment/`：S001的3:2手绘公园底图、完整生成提示词与来源说明；地图规则仍来自JSON并由精确边界叠加显示。
- `Assets/Resources/UrbanWildlife/Humans/`：Walker、Dweller、Visitor与Wheelchair User透明Sprite、背景提取记录与完整提示词。
- `P0ScenarioModels.cs`、`P0ConstraintEvaluator.cs` 与 `P0ConstraintManager.cs`：读取S001参数并输出人类连通、动物可达、Food有效性和改动次数。
- `P0CycleStateMachine.cs` 与 `P0CycleController.cs`：管理Plan、Confirm、60秒Run、30秒Observe和每Session 3个周期；未通过约束不能进入Run。
- `P0CycleMechanics.cs`：定义三轮情境、每轮动物群体规模、100分规划表现分、Observe因果回顾和跨周期滚动记忆；旧预测字段仅为日志兼容保留。
- `P0ControlPanel.cs`：提供大号阶段按钮、空格快捷键、周期/倒计时、四项约束与观察反馈；约束通过后可直接开始Run。
- `HumanRoutePlanner.cs`：把确认路径、中央广场与Food Hotspot组合为Walker、Dweller、Visitor和Wheelchair User路线。
- `HumanAgentStateMachine.cs`：以确定性状态管理进入、移动、广场停留、Hotspot访问和路线完成。
- `SteppedCharacterAnimation.cs`：定义人类与动物共用的8 FPS离散动画采样器，以及待机、转身、行走、喂食/进食、坐下和起身六类动作的关键帧数量与姿态。
- `P0TraceSeries.cs`：以固定最小距离采样连续位置，累积移动距离，并用LineRenderer生成当前及上一轮的可切换轨迹。
- `P0HumanSimulation.cs`：在Run阶段生成并更新8个人类Sprite代理，把道路移动状态映射为步行或轮椅推进动画，并处理逐帧转身、坐下、喂食和起身；Observe保留最后状态，Plan清除上一轮实例。
- `AnimalEnvironmentPlanner.cs`：把3个活动节点与2个Woodland映射为群体鸽子、松鼠和狐狸的起点、资源点及庇护点；读取上一轮节点偏好、松鼠熟悉度和谨慎系数，并给同群个体小范围确定性偏移。
- `AnimalAgentStateMachine.cs`：实现觅食、停留、回避人类、退回庇护地及松鼠熟悉度等确定性状态。
- `P0AnimalSimulation.cs`：在Run阶段按轮次生成11–14只动物、读取最近人类距离、更新状态和调试颜色，把移动、Resting与Feeding映射为共用逐帧表现，并汇总进食与回避次数。
- `Assets/Resources/UrbanWildlife/Animals/`：鸽子、松鼠和狐狸的透明手绘俯视Sprite、生成提示词与素材来源说明。
- `P0AnimalSpriteImporter.cs`：统一环境、人物和动物Sprite的透明度、单Sprite、双线性过滤、Clamp、尺寸上限与压缩导入规则。
- `P0ResearchLogger.cs` 与 `ResearchLogRecord.cs`：将布局确认、情境、预测、群体数量、约束和阶段事件追加为隐私安全的JSONL/CSV。
- `P0InputSceneTools.cs`：可重复创建P0输入场景，并在批处理模式验证脱敏夹具、9个调试元素、S001初始失败状态及两次移动后的有效修复。

## 当前边界

- Unity已完成可测试的逐帧动画节奏样板、角色标志性动作和实时Trace。三类步行人物以4 FPS播放交替行走姿态，Wheelchair User以4 FPS播放4帧推进循环；三种动物各有6帧独立行走与进食，Dweller有4帧坐下并可反向起身，Visitor有6帧喂食。待机与转身仍使用共用离散关键帧；无障碍坡度、道路宽度碰撞、拥堵寻路和压力区域自动分类尚未实现。
- 动物参数是为了验证因果链和交互节奏的P0设计抽象，不是伦敦公园的生态预测。
- 当前Animal Reachable只适用于S001的单一内部池塘：人类路径增加成本但不封路；未来加入围栏或多个障碍时再升级为网格寻路。
- 当前控制面板使用Unity内置即时GUI完成可操作原型；正式视觉语言和无障碍测试留到核心行为闭环稳定后处理。
- 城市Token、路网、出行、环境压力、四物种、施工计时、DP、五阶段、City Balance、Trace/Feed/Report数据与城市日志均已接入机制框架和自动验证；Scan City流程已用电子夹具接入Play Mode，实体摄像头源仍需到货后替换验证。城市轨迹、City Feed和阶段快照已有可操作原型，但当前建筑、设施、动物和界面仍是程序化检查物，统一等距美术与最终排版仍待完成。
- `Library`、`Temp`、`Obj`、`Logs`、`UserSettings` 和本机构建输出均不提交。
