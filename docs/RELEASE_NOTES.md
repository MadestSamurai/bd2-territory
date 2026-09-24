# BD2 Territory v0.3.2-beta.1

## 简体中文

### 更新内容

- 砍树落点参考真实树干碰撞体，缩小与树干的距离，减少在探测圈边缘空挥。
- 修复重新走位被误判为“已经到达”的问题。空挥后的新路线必须实际走到位。
- 一次完整空挥后改换站位，并暂存失败站位；收获旁边资源不会掩盖所选目标未命中。
- 保持 A* 为默认，NavMesh 默认关闭；保留动作及服务器结算等待。

### 下载

| 版本 | 运行环境 | 建议 |
| --- | --- | --- |
| Portable | 自带 .NET | 大多数用户 |
| Lite | .NET Desktop Runtime 8 x64 | 已安装桌面运行库 |

两版功能相同，内置中英切换。EXE 可独立运行；ZIP 包含双语说明和许可证，下载校验见 `SHA256SUMS.txt`。

### 升级

暂停并关闭旧工具，打开新版，点击「连接／更新组件」。等待连接完成后重新开始，原配方及预算设置保留。

## English

### Changes

- Logging approaches the actual trunk collider instead of stopping at the edge of its detection circle.
- Fixes recovery routes reporting arrival before the character has moved to the new stand.
- Repositions after one fully settled missed action and temporarily remembers failed stands. Harvesting a nearby resource no longer hides a miss against the selected target.
- A* remains the default and NavMesh stays off by default. Tool animations and server settlement still finish before recovery begins.

### Downloads

| Edition | Runtime requirement | Recommended for |
| --- | --- | --- |
| Portable | Includes .NET | Most users |
| Lite | .NET Desktop Runtime 8 x64 | Desktop runtime already installed |

Both editions include Chinese and English. The EXE runs independently; ZIP packages include bilingual documentation and licenses. Verify downloads with `SHA256SUMS.txt`.

### Upgrade

Pause and close the old tool, open the new version and select **Connect / update component**. Start automation after connecting. Existing recipe and budget preferences are kept.

---

# BD2 Territory v0.3.1-beta.1

## 简体中文

### 更新内容

- 默认使用 A* 网格计算绕障，通过方向移动接近采矿、砍树和种植目标。
- 新增「使用游戏 NavMesh 寻路」开关，默认关闭，选择自动保存。关闭后立即切回 A*；开启从下一次移动生效。
- A* 保留载具赶路、冲刺脱困和工人避障；找不到路线时保留目标重试，不会自动启用 NavMesh。
- 修复同一目标重试耗尽后可能无法重新规划的问题，并在到达时核对实际操作范围。

### 下载

| 版本 | 运行环境 | 建议 |
| --- | --- | --- |
| **Portable** | 自带 .NET，无需另装运行库 | 大多数用户 |
| **Lite** | 需要 .NET Desktop Runtime 8 x64 | 已安装桌面运行库、希望减小下载体积 |

两版功能一致，均内置中英切换。EXE 可独立运行；ZIP 附双语说明和许可证。可用 `SHA256SUMS.txt` 校验下载。

### 升级

暂停并关闭旧工具，打开新版，点击「连接／更新组件」，再开始自动化。已有配方和进度保留；旧设置中的 NavMesh 默认关闭。Beta 版建议先观察一次短距离采集。

## English

### Changes

- A* grid routing is now the default for approaching mining, logging and farming targets using directional movement.
- Adds an optional **Use game NavMesh navigation** setting, off by default and saved automatically. Turning it off switches to A* immediately; turning it on applies to the next route.
- A* retains vehicles, recovery dashes and worker avoidance. Failed routes keep their targets queued for retry and never enable NavMesh automatically.
- Fixes exhausted retry budgets preventing the same target from being planned again, and checks the actual interaction range on arrival.

### Downloads

| Edition | Runtime requirement | Recommended for |
| --- | --- | --- |
| **Portable** | Includes .NET | Most users |
| **Lite** | .NET Desktop Runtime 8 x64 | Users with the desktop runtime installed |

Both editions have identical features and built-in Chinese/English switching. The EXE runs on its own; ZIP packages include bilingual documentation and licenses. Verify downloads with `SHA256SUMS.txt`.

### Upgrade

Pause and close the old tool, launch the new version, select **Connect / update component**, then start automation. Recipes and progress are kept; older settings default to NavMesh off. For this beta, observe a short gathering route first.

---

# 0.3.0-beta.1 / Beta

## 简体中文

独立领地助手首次公开测试版，内置中英切换。

- 配方显示实际料理名、原料比例与解锁状态。
- 保留成熟资源采集、实体与工人避障、百格配方种植和布局预检／导入。
- 新增可选自动料理，每批上限 1–1000，默认关闭；制作记录和服务器回执核对避免重复消耗。
- 双语说明、独立 Portable／Lite、下载校验和无游戏安装构建。

## English

First public beta of the standalone territory assistant, with Chinese and English in one application.

- Recipes display actual dish names, ingredient ratios and unlock status.
- Includes mature-resource gathering, obstacle and worker avoidance, 100-field recipe farming and layout preview/import.
- Adds optional automatic cooking with a batch limit of 1–1000, off by default. Journals and server confirmation prevent blind duplicate requests.
- Bilingual documentation, Portable/Lite editions, checksums and builds without a game installation.

| 版本 / Edition | 运行环境 / Runtime | 建议 / Recommendation |
| --- | --- | --- |
| Portable | 自带 .NET / Includes .NET | 大多数用户 / Most users |
| Lite | .NET Desktop Runtime 8 x64 | 已有桌面运行库 / Runtime already installed |

两版功能相同。Beta 建议先用小预算、小批量确认效果。 / Both editions have the same features. Start with a small budget and batch size when evaluating this beta.
