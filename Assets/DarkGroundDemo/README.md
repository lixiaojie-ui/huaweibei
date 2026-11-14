# 数字孪生暗场地面 Demo

## 打开方式

1. 用 Unity/Tuanjie 打开本项目。
2. 打开 `Assets/DarkGroundDemo/DarkGroundDemo.unity`。
3. 点 Play。环境、粒子、后期都是运行时生成的，运行后 Hierarchy 里会出现：
   - `Environment / GridPlane`（100m 可见范围的全息网格）
   - `Environment / Platform`（台体 + 发光灯带 + 蓝紫柔光）
   - `Environment / ScanMarkers`（3 个呼吸扫描标记）
   - `Environment / LightBeams`（3 根竖直光柱）
   - `Particles`（悬浮光点，300 个）
   - `Global Volume`（Bloom / Vignette / ACES / 色差）
   - `CameraRig`（缓慢右移 + 微噪声）

> 本工程是 **Built-in Render Pipeline + Post Processing v2**，没有安装 URP 和 Cinemachine。
> 因此本 Demo 用内置管线实现，Cinemachine 用 `DemoCameraRig` 的 Perlin 噪声替代，后期用 PPv2 的 Volume。
> 画面逻辑与参数和 URP 版一致；移植到 URP 时只需换 Shader 的 Pass 标签/包含文件，并把 PPv2 Volume 换成 URP Volume。

## 交付文件

| 文件 | 作用 |
|---|---|
| `Resources/Shaders/HoloGrid.shader` | 网格 Shader：世界坐标双层网格、fwidth 抗锯齿、相机距离渐隐、Z 轴扫描带 |
| `Resources/Shaders/ParticleAdditive.shader` | 无贴图软圆点，粒子与平台柔光共用 |
| `Scripts/DarkGroundDemoBootstrap.cs` | 运行时生成场景；粒子预设参数集中在 `SetupParticles()` |
| `Scripts/DemoCameraRig.cs` | FOV 35、俯角、0.1m/s 右移、微噪声（替代 Cinemachine） |
| `Scripts/DemoMarkerPulse.cs` | 扫描标记 2.5s 正弦呼吸 |
| `DarkGroundDemo.unity` | Demo 场景（只有相机和 Bootstrap） |
| `Resources/DarkGroundPostProcessResources.asset` | PPv2 资源副本，供运行时创建 PostProcessLayer |

粒子是代码预设：进入 Play 后，把 Hierarchy 里的 `Particles` 拖到 Project 窗口，即可保存成可复用的 `.prefab`。

## 可调参数

### Canvas / Grid（`DarkGroundDemo` Inspector）

| 参数 | 默认 | 说明 |
|---|---:|---|
| `gridColor` | #19C8FF | 网格主色 |
| `cellSize` | 0.6 | 细网格尺寸（米） |
| `majorEvery` | 5 | 每 5 格一条主网格 |
| `fadeStart` / `fadeEnd` | 60 / 100 | 以相机为圆心的渐隐范围（米） |
| `gridIntensity` | 1 | 网格总强度 |
| `scanPeriod` | 15 | 扫描带往返周期（秒） |
| `scanWidth` | 8 | 扫描带宽度（米） |
| `scanIntensity` | 1.6 | 扫描带亮度 |
| `particleCount` | 300 | 目标存活粒子数 |
| `particleMax` | 400 | Max Particles |
| `platformPosition` | (-15, 0, 24) | 平台位置 |
| `platformSize` | (12, 1.6, 7) | 平台尺寸 |
| `cameraPitch` | 15 | 仅在场景没有相机时用于新建相机 |

### HoloGrid Shader

| 属性 | 默认 | 说明 |
|---|---:|---|
| `_MinorWidth` / `_MajorWidth` | 0.012 / 0.03 | 细线/主线宽度（米） |
| `_MinorIntensity` / `_MajorIntensity` | 0.55 / 1.35 | 细线/主线亮度 |
| `_ScanMinZ` / `_ScanMaxZ` | -40 / 60 | 扫描带 Z 轴活动范围 |

Shader 用 `worldPos.xz / _CellSize` 计算 UV，不依赖平铺贴图；`fwidth` 做边缘抗锯齿，细网格在屏幕密度过高时自动淡出，因此相机平移时不会抖动或拉伸。

### 粒子（`SetupParticles()`）

| 参数 | 值 |
|---|---|
| 数量 | 300（Max 400） |
| Start Speed | 0.02–0.05 |
| Lifetime | 8–15s |
| Start Size | 0.02–0.08 |
| 颜色/透明度 | #8FE6FF，alpha 0.2–0.5 |
| 发射区域 | 50 × 50 × 10 的 Box，中心高度 5.5m |
| 发射率 | 约 26 个/秒 |
| Size over Lifetime | 1 → 0.8 → 0.35（先大后小） |
| Color over Lifetime | 0 → 1 → 0.65 → 1 → 0（呼吸式淡入淡出） |
| Noise | 强度 0.03，频率 0.2，横向轻摆 |

### 相机（`DemoCameraRig`）

| 参数 | 默认 |
|---|---:|
| FOV | 35 |
| 俯角 | 15°（15–25°可调） |
| 右移速度 | 0.1 m/s |
| 最大位移 | 2m（20 秒后停住） |
| 噪声幅度/频率 | 0.3 / 0.3 |

### 后期（`SetupPostFx()`）

| 效果 | 参数 |
|---|---|
| Bloom | Intensity 0.8，Threshold 0.9，Soft Knee 0.5，Diffusion 7 |
| Vignette | Intensity 0.35，Smoothness 0.45 |
| Tone Mapping | ACES |
| Chromatic Aberration | 0.03 |
| Color Grading | Contrast +10 |

PPv2 的 Bloom 没有 URP 的 Scatter 参数，这里用 Diffusion 7 达到相近的扩散效果。

## 实现说明

- 网格 Plane 实际放大到 200×200：可见范围由 60–100m 距离渐隐控制，Plane 比可见范围大一圈就不会露出硬边。
- 平台、扫描标记、光柱都用内置 Standard 自发光材质 + 立方体拼装，只有网格和软光点用了自定义 Shader，文件量很小。
- 场景里没有光源污染：只有一盏冷色平行光和两盏平台点光，阴影关闭。
- 全场景约 30 个渲染对象、最多 400 粒子、一次 Bloom/Vignette 全屏后期，1080p 下可稳定 60fps。
- 本 Demo 与 `SampleScene`、`展示车间场景` 的现有动态背景互不影响；打开本场景不会修改其它场景。
