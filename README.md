# StreetFighterUnity

把 [AlloyTeam 的 Canvas 版街头霸王](../StreetFighter)（`js/` + `images/*.gif`）复刻成 Unity 项目。

Unity 版本：**2022.3.62f3c1**（2D 项目，无第三方依赖，只用内置模块 + UGUI）。

---

## 快速开始

1. 用 Unity Hub 打开 `StreetFighterUnity` 目录。
2. 打开场景 `Assets/Scenes/Main.unity`。
3. 点 Play。

场景里只有一个 `GameManager` 对象和一台正交摄像机，其余（角色、背景、血条、特效）都在运行时由 `GameManager` 创建。

如果场景丢失，可以用菜单 **StreetFighter → Build Scene** 重新生成（会同时刷新序列帧纹理的导入设置并写入 Build Settings）。

---

## 操作

| | 主机（P1，RYU1） | 副机（P2，RYU2） |
|---|---|---|
| 移动 | `W` 上 / `S` 下 / `A` 后 / `D` 前 | `↑` `↓` `←` `→` |
| 轻拳 / 重拳 | `J` / `K` | 小键盘 `1` / `2` |
| 轻腿 / 重腿 | `U` / `I` | 小键盘 `4` / `5` |

**出招**（双方通用）：

- 波动拳：下 → 前 → 拳
- 旋风腿：下 → 后 → 腿
- 升龙拳：前 → 下 → 前 → 拳

**其他**：

- `F2` 暂停 / 继续
- `1` 人机对战（P2 由 AI 接管）
- `2` 双人对打

> 副机使用小键盘；主键盘数字 `1` `2` 专门用于切换模式，不参与攻击。

角色靠近到一定距离会自动切换朝向，此时移动键的前后含义会镜像（与原版一致）。

---

## 从原版迁移了什么

原版核心是四件事，Unity 版本全部保持机制等价：

1. **配置驱动状态机** —— `config.js` 的 `Config` 对象经 Node 导出为 `Assets/Resources/config.json`，运行时读取。61 个角色状态、48 个 `play` 动作组合、出招表、判定框参数与原版数值一致，不手写常量。`easing` 原本是 JS 函数，在 C# 中按名字重新实现。
2. **输入缓冲连招识别** —— 移动键持续采样 + 攻击键边缘触发 + 短动作序列拼接成字符串匹配 `attack.special`（如 `forward,crouch,forward,heavy_boxing` → 升龙拳）。
3. **基于状态的攻防判定** —— 攻击等级互拼、防御削血、受击/击飞/倒地/起身短暂无敌、飞行道具相消。
4. **统一帧驱动** —— `GameClock` 以 17ms 固定步长推进所有子系统（等价于原版 `setInterval(Config.fps)`），回调注册顺序与 `timer.js` 一致；`setTimeout` 也换成游戏内时钟，暂停时一并冻结。

### 资源处理

- 原版 114 张 gif 横向序列帧条带转成 png 放在 `Assets/Resources/Art/`（Unity 不支持 gif），运行时按 `framesNum` 切片成 Sprite，pivot 左上角、PPU=1。
- 13 个 mp3 放在 `Assets/Resources/Sound/`。
- `Assets/Editor/TextureSettings.cs` 统一设置导入参数：Point 过滤、不压缩、保留透明通道。

### 坐标与画面

- 沿用原版画布像素坐标（900×490，y 向下）：世界原点在画布中心，**1 世界单位 = 1 像素**。
- 摄像机 `orthographicSize = 245` + 强制 `aspect = 900/490` + `camera.rect` 居中留黑边，保证任意窗口比例下画面比例不变。
- HUD 是挂在摄像机下的 WorldSpace Canvas（900×490），与画面严格对齐，不会随分辨率漂移。
- 原版方向 `-1` 时用 canvas 变换做镜像，这里统一为「包围盒 `[left, left + width*zoom]` 不变、`scale.x = ±zoom`」，与 `changeBg` 中的左边界补偿完全吻合。

---

## 目录结构

```
Assets/
├── Resources/
│   ├── config.json          # 由 js/config.js 导出
│   ├── Art/                 # 序列帧 png（g/ hitEffect/ magic/ RYU1/ RYU2/）
│   └── Sound/               # mp3
├── Scenes/Main.unity
├── Editor/
│   ├── TextureSettings.cs   # 纹理导入设置
│   └── SceneBuilder.cs      # 生成场景
└── Scripts/StreetFighter/
    ├── Core/                # 引擎层：GameClock / Ani / Art / Sfx / JVal+Json / Evt,Q,Lock / Easing / Cfg
    ├── Runtime/             # 玩法层：Spirit / SpiritFrames / Status / Collider / Melee / Wave / KeyInput / Ai
    ├── View/                # 表现层：SpriteView / SpiritView / WaveView
    └── Game/                # 流程层：GameManager / Stage / BloodBar
```

对应关系：

| Unity | 原版 |
|---|---|
| `Core/GameClock.cs` | `js/timer.js` |
| `Core/Ani.cs`、`Core/Easing.cs` | `js/interface.js` 的 `Animate` + `config.easing` |
| `Core/JVal.cs` | —— （Unity `JsonUtility` 不支持字典与混合类型数组，故自带解析器） |
| `Runtime/Spirit.cs` | `js/main.js` 的 `Block` + `Spirit` |
| `Runtime/Melee.cs` / `Wave.cs` | `js/main.js` 的 `Fighter` / `WaveBoxing` |
| `Runtime/KeyInput.cs` | `js/interface.js` 的 `KeyManage` |
| `Runtime/Ai.cs` | `js/ai.js` |
| `Game/GameManager.cs` | `js/main.js` 的 `Game` / `gameStart` |
| `Game/Stage.cs` | `js/map.js` 的 `Stage` |
| `Game/BloodBar.cs` | `js/main.js` 的 `Blood` |

---

## 二次开发提示

- 想调数值（伤害、判定框、帧数、位移）直接改 `Assets/Resources/config.json`，不要改代码常量；需要重新导出时用 Node 执行 `vm` 加载 `StreetFighter/js/config.js` 即可。
- 想加角色：在 `config.json` 的 `Spirit` 下加一份配置（含 `states` / `keyMap`），并在 `GameManager.StartMatch()` 里实例化。
- 想关掉 AI：`1`/`2` 切到双人模式，或删除 `StartMatch()` 中的 `Ai` 创建。

---

图片素材来自互联网，原作者 Random，原版实现版权归 Tencent AlloyTeam。本项目仅供学习研究。
