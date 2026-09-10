# StreetFightUnity

使用 AI 复刻的街头霸王 Unity 版本（**2022.3.62f3c1**，2D，无第三方依赖，只用内置模块 + UGUI + Input System）。
全部玩法数值由 ScriptableObject 配置资产驱动，代码里不写常量。

![街头霸王](Display.gif)

https://github.com/WarrenMondeville/StreetFightUnity.git

> 完整文档见 **[项目 Wiki](https://github.com/WarrenMondeville/StreetFightUnity/wiki)**：架构、配置字段、玩法系统、改键与常见问题都在那边。

---

## 快速开始

1. Unity Hub 打开 `StreetFighterUnity` 目录。
2. 打开场景 `Assets/Scenes/Main.unity`。
3. 点 Play。

场景里只有 `GameManager` 和一台正交摄像机，角色 / 背景 / 血条 / 特效都在运行时创建。
场景丢失时用菜单 **StreetFighter → Build Scene** 重新生成。

前提：已装 `Input System 1.14.2`，且 `Project Settings → Player → Active Input Handling` = `Input System Package (New)`。
详见 [快速开始](https://github.com/WarrenMondeville/StreetFightUnity/wiki/%E5%BF%AB%E9%80%9F%E5%BC%80%E5%A7%8B)。

---

## 操作

| | 主机（P1） | 副机（P2） | 手柄 |
|---|---|---|---|
| 移动 | `W` `S` `A` `D` | `↑` `↓` `←` `→` | 左摇杆 / 十字键 |
| 轻拳 / 重拳 | `J` / `K` | 小键盘 `1` / `2` | □ / △ |
| 轻腿 / 重腿 | `U` / `I` | 小键盘 `4` / `5` | ✕ / ○ |

出招（双方通用）：波动拳 = 下→前→拳，旋风腿 = 下→后→腿，升龙拳 = 前→下→前→拳。

`F2` 暂停、`1` 人机、`2` 双人、`F1` 判定框。详见 [操作与出招](https://github.com/WarrenMondeville/StreetFightUnity/wiki/%E6%93%8D%E4%BD%9C%E4%B8%8E%E5%87%BA%E6%8B%9B)。

---

## 文档（Wiki）

| 分类 | 页面 |
|---|---|
| 上手 | [快速开始](https://github.com/WarrenMondeville/StreetFightUnity/wiki/%E5%BF%AB%E9%80%9F%E5%BC%80%E5%A7%8B) · [操作与出招](https://github.com/WarrenMondeville/StreetFightUnity/wiki/%E6%93%8D%E4%BD%9C%E4%B8%8E%E5%87%BA%E6%8B%9B) |
| 架构 | [架构总览](https://github.com/WarrenMondeville/StreetFightUnity/wiki/%E6%9E%B6%E6%9E%84%E6%80%BB%E8%A7%88) · [玩法系统](https://github.com/WarrenMondeville/StreetFightUnity/wiki/%E7%8E%A9%E6%B3%95%E7%B3%BB%E7%BB%9F) · [美术与音频资源](https://github.com/WarrenMondeville/StreetFightUnity/wiki/%E7%BE%8E%E6%9C%AF%E4%B8%8E%E9%9F%B3%E9%A2%91%E8%B5%84%E6%BA%90) |
| 配置 | [配置资产](https://github.com/WarrenMondeville/StreetFightUnity/wiki/%E9%85%8D%E7%BD%AE%E8%B5%84%E4%BA%A7) · [配置编辑器](https://github.com/WarrenMondeville/StreetFightUnity/wiki/%E9%85%8D%E7%BD%AE%E7%BC%96%E8%BE%91%E5%99%A8) |
| 开发 | [代码风格与约定](https://github.com/WarrenMondeville/StreetFightUnity/wiki/%E4%BB%A3%E7%A0%81%E9%A3%8E%E6%A0%BC%E4%B8%8E%E7%BA%A6%E5%AE%9A) · [常见问题与坑](https://github.com/WarrenMondeville/StreetFightUnity/wiki/%E5%B8%B8%E8%A7%81%E9%97%AE%E9%A2%98%E4%B8%8E%E5%9D%91) |

---

## 二次开发速查

- **改数值**：改 `Assets/Resources/Config/` 下的资产，或用 **StreetFighter → Config Editor**，不要改代码常量。
- **加角色**：`Resources/Config/Fighters/` 下 `Create → StreetFighter → Fighter`（或复制 RYU1），再到 `GameManager.StartMatch()` 实例化。
- **改键盘**：改角色资产 `KeyMap → Mappings` 的 keyCode；**改手柄**改 `FighterInput.GamepadAttackPaths`。
- **关 AI**：按 `2` 切双人，或删掉 `StartMatch()` 里的 `AiController`。
- **出场位置 / 追帧上限 / 重开节奏**：`GameManager` 的 Inspector 字段。

代码约定（一个文件一个类型、字段 `_` 前缀、状态名走 `StateNames`、枚举替字符串、输入只用 Input System、时间只用 `GameClock`）见
[代码风格与约定](https://github.com/WarrenMondeville/StreetFightUnity/wiki/%E4%BB%A3%E7%A0%81%E9%A3%8E%E6%A0%BC%E4%B8%8E%E7%BA%A6%E5%AE%9A)。

---

图片素材来自互联网，原作者 Random，原版实现版权归 Tencent AlloyTeam。本项目仅供学习研究。
