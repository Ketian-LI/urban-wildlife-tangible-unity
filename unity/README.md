# Unity

这里放置 Unity 2D 项目。

创建项目后补充：

- Unity 编辑器版本与所需模块。
- 打开和运行场景的方法。
- 主场景名称。
- Input Manager、Environment Manager、NPC、Animal、Trace 与 Logger 的位置。
- Build 与运行时配置步骤。

不要提交 `Library`、`Temp`、`Obj`、`Logs`、`UserSettings` 或本机构建输出。

## P0 输入约定

- Python 在 Confirm 后原子更新 `data/raw/layout-packets/latest_layout.json`。
- Unity Input Manager 只接受 `schema_version = 0.1` 和 `packet_type = confirmed_layout`。
- 若时间戳不比上次读取的新、JSON 不完整或版本不支持，则保持上一份有效布局并记录错误。
- `x_norm` 映射 Unity X，`y_norm` 映射 Unity Z；四角 Marker 不会出现在 Token 数组中。
- 正式字段定义见 `data/schemas/layout_packet_v0.1.schema.json`；隐私安全示例见 `docs/images/vision/layout-packet-validation/latest_layout.json`。
