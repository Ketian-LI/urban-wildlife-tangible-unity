# Vision

这里放置摄像头、Marker、彩绳和坐标映射代码。

第一周建议按以下顺序验证：

1. 摄像头稳定取流。
2. 单个 ArUco 或 AprilTag 的 ID、位置和角度。
3. 多 Token 与短暂遮挡容错。
4. 彩色路径 HSV 分割与去噪。
5. 四角校准与标准化坐标。
6. 统一数据包与 Debug 可视化。

在技术 Spike 完成后再锁定 Python、OpenCV 和 Marker 库版本。
