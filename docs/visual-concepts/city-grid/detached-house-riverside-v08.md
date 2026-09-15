# Detached House Riverside V0.8

## 纠正目标

V0.7属于通用卡通两层住宅，正立面与细节过多。V0.8不再以旧房屋为结构输入，而是直接截取目标图左上区域的蓝顶与珊瑚顶小住宅作为形状锚点，重建为可在Unity地图中使用的透明Sprite。

## 蓝顶提示词

```text
Use case: precise-object-edit. Asset type: high-resolution Unity map sprite reconstruction. Image 1 is the authoritative close crop of the exact small blue-roof house to reconstruct; Image 2 is context only for Riverside Park scale and rendering language. Isolate and faithfully upscale only the blue-roof house visible in Image 1. Reproduce its exact compact silhouette and proportions instead of designing a generic suburban house. Use a high top-down 2.5D view; the roof must occupy about 70–75% of the visible building; only a very shallow narrow pale facade is visible at the bottom; compact almost-square footprint; simple blue hip/gable roof with one short central ridge and a tiny darker front roof plane. At most two tiny blue-grey window marks. No tall wall mass, large door, doorstep, chimney, roof ribs, roof tiles, fascia thickness, garden, grass, base plate, trees, bushes, paths, roads, people, vehicles, labels, text, border, or second object. Match the clean, tiny, low-volume Riverside Park map-building style. Use muted blue around #6090D0–#70A0E0 and warm off-white around #F2F0E8. Center one building with generous padding on a perfectly flat #FF00FF chroma background.
```

## 珊瑚顶提示词

```text
Use case: precise-object-edit. Asset type: high-resolution Unity map sprite reconstruction. Image 1 is the authoritative close crop of the exact small coral-roof house to reconstruct; Image 2 is context only for Riverside Park scale and rendering language. Isolate and faithfully upscale only the coral-roof house visible in Image 1. Reproduce its exact compact silhouette and proportions instead of designing a generic suburban house. Use a high top-down 2.5D view; the coral roof must occupy about 75–80% of the visible building; only a very shallow pale-grey facade is visible as a narrow strip at the bottom; compact rectangular footprint; simple low hip roof with rounded-soft corners. At most one or two tiny dark window marks. No tall wall mass, large door, doorstep, chimney, roof ribs, roof tiles, fascia thickness, garden, grass, base plate, trees, bushes, paths, roads, people, vehicles, labels, text, border, or second object. Match the clean, tiny, low-volume Riverside Park map-building style. Use softened coral around #F06070–#F07070 and warm pale-grey around #E9E9E3. Center one building with generous padding on a perfectly flat #FF00FF chroma background.
```

## 执行

- 使用Codex内置图像编辑工具的`precise-object-edit`模式；未使用CLI/API回退。
- 以目标图局部裁切为编辑目标、完整目标图为上下文参考；生成后仅移除隔离用洋红底，输出1024 × 1024透明RGBA。
- Unity表现系数为0.72；蓝房不透明宽度约0.37单位，珊瑚房约0.41单位，真实逻辑占地仍为5 × 5单位。
