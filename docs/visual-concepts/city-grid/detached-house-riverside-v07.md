# Detached House Riverside V0.7

## 用途

两张1024 × 1024透明RGBA独栋住宅Sprite，用于Unity稀疏开局。蓝顶与珊瑚顶共用同一结构、角度和比例，地图地面、草坪、树木与道路均由运行时或底图单独控制。

## 输入角色

- 编辑目标：`detached-house-reference-blue-v06.png` / `detached-house-reference-coral-v06.png`
- 风格与比例依据：用户确认的Riverside Park完成态地图参考图

## 内置图像编辑提示词

### 蓝顶

```text
Use case: precise-object-edit. Asset type: Unity map sprite. Image 1 is the existing detached-house sprite whose single-house identity and transparent cutout must be preserved. Image 2 is the authoritative Riverside Park visual reference for palette, simplification, map-scale volume, camera, and shadow. Create exactly one small two-storey detached residential building as a clean 2.5D map icon, viewed from a higher three-quarter top-down camera (about 55–60 degrees downward), slightly from the upper-right so the visible front facade is about half the area of the current sprite. Keep a compact rectangular footprint. Simplify aggressively: one plain gable roof with absolutely no rib lines, seams, tiles, or thick fascia; warm pale cream walls (#F3EFE4 family); only a few tiny blue-grey rectangular windows; one small centered door; a minimal flat doorstep; one small simple chimney with a coral-red cap. Use soft, low-volume shading and a very short pale shadow tucked close to the building toward lower-right. Match the reference's clean, bright, low-saturation, lightweight map illustration. Maintain generous transparent padding and similar object scale/framing to Image 1. Output true RGBA transparency, 1024x1024. Do not include grass, a lawn/base plate, paths, trees, bushes, roads, text, labels, border, checkerboard, scenery, or any second object. Roof variant: muted clear Riverside blue (#78A8DD family). Keep the chimney cap coral-red.
```

### 珊瑚顶

```text
Use case: precise-object-edit. Asset type: Unity map sprite. Image 1 is the existing detached-house sprite whose single-house identity and transparent cutout must be preserved. Image 2 is the authoritative Riverside Park visual reference for palette, simplification, map-scale volume, camera, and shadow. Create exactly one small two-storey detached residential building as a clean 2.5D map icon, viewed from a higher three-quarter top-down camera (about 55–60 degrees downward), slightly from the upper-right so the visible front facade is about half the area of the current sprite. Keep a compact rectangular footprint. Simplify aggressively: one plain gable roof with absolutely no rib lines, seams, tiles, or thick fascia; warm pale cream walls (#F3EFE4 family); only a few tiny blue-grey rectangular windows; one small centered door; a minimal flat doorstep; one small simple chimney with a coral-red cap. Use soft, low-volume shading and a very short pale shadow tucked close to the building toward lower-right. Match the reference's clean, bright, low-saturation, lightweight map illustration. Maintain generous transparent padding and similar object scale/framing to Image 1. Output true RGBA transparency, 1024x1024. Do not include grass, a lawn/base plate, paths, trees, bushes, roads, text, labels, border, checkerboard, scenery, or any second object. Roof variant: softened Riverside coral-red (#ED7770 family), natural and not neon. Keep the chimney cap in the same coral family but slightly darker.
```

### 背景提取

```text
Use case: background-extraction. Preserve the single house exactly: its geometry, colors, windows, door, chimney, camera angle, scale, and clean illustration style. Remove the entire background and every background artifact. Keep only the house and a very small, soft, semi-transparent shadow immediately under and slightly lower-right of it. Output a clean RGBA PNG with generous transparent padding. No checkerboard, no white or gray backdrop, no grass, no base plate, no scenery, no text, no border, no second object.
```

## 执行说明

- 模式：Codex内置图像生成/编辑工具（`precise-object-edit`与`background-extraction`），未使用CLI/API回退。
- 内置输出未保留真实Alpha时，先以纯洋红隔离背景，再只对背景做确定性透明哑光提取；房屋结构不再重绘。
- 最终按目标图抽样范围校正蓝顶与珊瑚顶色相，并用Unity实机截图校准显示系数为0.95。
