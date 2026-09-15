# Riverside建筑族 V0.9

## 参考职责

- 成熟期图：建筑类别、城市高密度下的尺寸关系。
- 早期图：开局尺度、道路与林地中的建筑占比。
- 常规期图：暖白墙体、灰蓝窗、蓝/珊瑚/橙/蓝绿色强调色。
- 动物热点图版本：只约束右下角热点卡片，不作为建筑相机参考。

## 统一相机

- 建筑正面沿画面水平线摆正并朝向屏幕下方。
- 只显示窄右侧面和薄顶面，不旋转成45度菱形。
- 体积感来自很轻的明暗面，不使用厚描边、强投影或写实材质。

## Built-in imagegen提示词摘要

- `Detached`: warm-white single-storey cottage, blue or coral gable roof, one tiny door and few blue windows, aligned front facade, narrow right gable, transparent background.
- `Apartment`: slender five-storey warm-white block, two columns of blue windows, shallow flat parapet, narrow right side, transparent background.
- `Market`: low warm-white shop, thin flat parapet, orange-white striped awning, aligned storefront, transparent background.
- `Community`: symmetrical warm-white civic building, shallow portico, blue-green dome and tiny coral flag, transparent background.

使用内置imagegen的`precise-object-edit`模式，以用户提供的Riverside截图局部作为角度与结构参考；未使用CLI/API回退。最终资源收紧透明边距后接入Unity。
