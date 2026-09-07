# Technical Architecture V0.1

## 已确定的实体输入基线

- 60 × 90 cm 黑色磁吸板，横向平放。
- 摄像头从正上方垂直俯拍，预估高度为板面上方 70 至 100 cm，最终由镜头视角测试确定。
- 四角使用固定且 ID 不同的白底校准 Marker。
- 玩家操作 Token 与彩绳，双手离开后再确认读取。

## 数据流

```text
Camera Frame
   ├─ Marker detector → token id, x, y, angle
   └─ HSV path detector → path mask / polyline
                 ↓
Coordinate normalizer
                 ↓
Versioned input packet
                 ↓
Unity Input Manager
                 ↓
Environment, Human NPC and Animal systems
                 ↓
Trace renderer and Research Logger
```

## 建议的数据包

```json
{
  "schema_version": "0.1",
  "timestamp_ms": 0,
  "calibration_id": "local-test",
  "tokens": [
    {
      "id": 0,
      "type": "food",
      "x_norm": 0.5,
      "y_norm": 0.5,
      "angle_deg": 0,
      "confidence": 1.0
    }
  ],
  "path": {
    "format": "polyline",
    "points_norm": []
  }
}
```

这是第一版接口草案。通信方式、采样频率、坐标原点和丢帧策略需要在技术 Spike 后锁定。

## 模块边界

- `vision/` 只负责读取实体状态、校准、稳定和输出标准数据。
- `unity/` 只消费标准数据，不直接依赖摄像头实现。
- `data/` 只定义可公开的数据结构和脱敏样例。
- 研究日志必须标注版本、会话和时间，不记录不必要的身份信息。
