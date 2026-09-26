# BD2 Territory v0.3.10-beta.1

## 简体中文

### 更新内容

- 移除必须 100 格农田、全部连片才能种植的限制，使用当前农田即可。
- 按游戏实际预览数量批量播种，分散农田分片处理，小田与零散单格也能继续种。
- 按库存和生长中作物的预计产量持续平衡料理原料比例；固定作物模式同样适用。
- 播种费用和进度按本批实际格数显示、核对；已有设置与待结算批次保留。

### 下载

| 版本 | 运行环境 | 建议 |
| --- | --- | --- |
| **Portable** | 自带 .NET，无需另装运行库 | 大多数用户 |
| **Lite** | 需要 .NET Desktop Runtime 8 x64 | 已安装桌面运行库、希望减小下载体积 |

两版功能相同，内置简体中文／English。EXE 可独立使用；ZIP 附带双语说明与许可证。用 `SHA256SUMS.txt` 核对下载。

### 升级

暂停并关闭旧工具，打开新版，点击「连接／更新组件」后开始。无需重摆农田；配方比例在连续种植中平衡，每片仍以同种作物批量播种。

作者发布版免费。第三方收费不代表作者参与、背书或提供服务。[使用说明与风险提示](https://github.com/MadestSamurai/bd2-territory/blob/main/README.md)。

## English

### Changes

- Removes the requirement for exactly 100 connected fields. Use your existing farm layout.
- Plants actual native preview groups. Disconnected areas, smaller plots and isolated single fields are supported.
- Balances recipe ingredients using inventory and expected growing yield across successive batches. Fixed-crop mode supports the same layouts.
- Shows and checks the actual field count and charge for each batch. Settings and unresolved planting transactions are retained.

### Downloads

| Build | Runtime | Recommended for |
| --- | --- | --- |
| **Portable** | Includes .NET; no separate runtime needed | Most users |
| **Lite** | Requires .NET Desktop Runtime 8 x64 | Smaller download when the desktop runtime is installed |

Both builds have identical features and include Simplified Chinese / English. EXEs run independently; ZIPs include bilingual documentation and licenses. Verify downloads with `SHA256SUMS.txt`.

### Upgrade

Pause and close the old tool. Open this version and select Connect / update component before starting. No field rearrangement is needed. Recipe ratios balance over successive planting cycles; each native batch still uses one crop.

Official releases are free. Third-party fees do not imply the author's involvement, endorsement or support. [Usage and risk notice](https://github.com/MadestSamurai/bd2-territory/blob/main/README.en.md).

---

# BD2 Territory v0.3.9-beta.1

## 简体中文

### 更新内容

- 新增可选自动售卖：保留数量可设为 100–9900，默认 9900，只卖超出部分，默认关闭。
- 只处理领地商店可售材料、采集／种植产物与料理；锁定物品、建筑装饰、装备及属性石排除。
- 自动料理优先，售卖逐批核对游戏回执、库存与领地币；结果未知时暂停并保留记录，避免重复售卖。
- 主窗口增加免费开源署名：GitHub MadestSamurai／B站 MadSamurai。
- 新增「来源与说明」，可查看并复制官方仓库与下载链接；随界面切换中英文。
- 统一双语 README、来源与风险说明，ZIP 附带完整说明；MIT 许可证保持不变。

### 下载

| 版本 | 运行环境 | 建议 |
| --- | --- | --- |
| **Portable** | 自带 .NET，无需另装运行库 | 大多数用户 |
| **Lite** | 需要 .NET Desktop Runtime 8 x64 | 已安装桌面运行库、希望减小下载体积 |

两版功能相同，内置简体中文／English。EXE 可独立使用；ZIP 附带双语说明与许可证。用 `SHA256SUMS.txt` 核对下载。

### 升级

暂停并关闭旧工具，打开新版，点击「连接／更新组件」后开始。已有设置保留，自动售卖需手动开启。

作者发布版免费。第三方收费不代表作者参与、背书或提供服务。[使用说明与风险提示](https://github.com/MadestSamurai/bd2-territory/blob/main/README.md)。

## English

### Changes

- Adds optional surplus selling: retain 100–9900 per item, default 9900; sell only the excess. Disabled by default.
- Limits sales to eligible territory materials, produce and meals; excludes locked items, decorations, equipment and attribute stones.
- Cooking takes priority. Each sale checks the native reply, inventory and territory currency; unknown outcomes pause without resubmission.
- Adds free-release attribution to the main window: GitHub MadestSamurai / Bilibili MadSamurai.
- Adds About & source with selectable official repository and download links, following the selected UI language.
- Standardizes bilingual READMEs and source/risk notices, also included in ZIPs. The MIT License is unchanged.

### Downloads

| Build | Runtime | Recommended for |
| --- | --- | --- |
| **Portable** | Includes .NET; no separate runtime needed | Most users |
| **Lite** | Requires .NET Desktop Runtime 8 x64 | Smaller download when the desktop runtime is installed |

Both builds have identical features and include Simplified Chinese / English. EXEs run independently; ZIPs include bilingual documentation and licenses. Verify downloads with `SHA256SUMS.txt`.

### Upgrade

Pause and close the old tool. Open this version and select Connect / update component before starting. Settings are retained; surplus selling must be enabled explicitly.

Official releases are free. Third-party fees do not imply the author's involvement, endorsement or support. [Usage and risk notice](https://github.com/MadestSamurai/bd2-territory/blob/main/README.en.md).

---

# BD2 Territory v0.3.8-beta.1

## 简体中文

### 更新内容

- 减少开启自动料理后、采集区与农田之间往返时的重复寻路等待。
- 已检测的地面与通路不再按时间清空；采集、建筑移动或桥梁变化只更新受影响区域，料理和库存变化不触发重建。
- 保留粗网格搜索进度，沿游戏实际桥梁规划窄路；支持不同目标之间正向或反向复用已检测通路。
- 包含水面只走桥梁、矿点采集站位和领地地形台阶的修复。

### 下载

| 版本 | 运行环境 | 建议 |
| --- | --- | --- |
| Portable | 自带 .NET | 大多数用户 |
| Lite | .NET Desktop Runtime 8 x64 | 已安装桌面运行库 |

两版功能相同，内置中英切换。EXE 可独立运行；ZIP 包含双语说明和许可证，下载校验见 `SHA256SUMS.txt`。

### 升级

暂停并关闭旧工具，打开新版，点击「连接／更新组件」，连接完成后再开始。保留已有设置及种植进度。

## English

### Changes

- Reduces repeated route-planning waits when moving between gathering areas and farms with automatic cooking enabled.
- Retains checked terrain and routes instead of expiring them on a timer. Harvesting, moved structures and bridge changes update affected areas; cooking and inventory changes do not rebuild the map.
- Preserves coarse-grid search progress, follows native bridge corridors and reuses checked routes in either direction for different targets.
- Includes water-only-via-bridges protection, improved ore interaction positions and native terrain-step handling.

### Downloads

| Build | Runtime | Recommended for |
| --- | --- | --- |
| Portable | Includes .NET | Most users |
| Lite | .NET Desktop Runtime 8 x64 | Users with the desktop runtime installed |

Both builds have the same features and include Chinese/English switching. Each EXE runs on its own; ZIPs include bilingual documentation and licenses. Verify downloads with `SHA256SUMS.txt`.

### Upgrade

Pause and close the old tool, open the new build and select **Connect / update component** before starting. Existing settings and planting progress are retained.

---

# BD2 Territory v0.3.7-beta.1

## 简体中文

### 更新内容

- 水面一律禁止通行，只通过已放置桥梁的实际通道；桥旁、预览桥和已移除的桥不会放行。
- A*、可选原生寻路、路线复用、通行试探及冲刺脱困统一检查水域，修复底板碰撞被误当作水面通路的问题。
- 精确检查经过的水域格子边界，避免斜线走位从两个陆地路点之间切过水面。

### 下载

| 版本 | 运行环境 | 建议 |
| --- | --- | --- |
| Portable | 自带 .NET | 大多数用户 |
| Lite | .NET Desktop Runtime 8 x64 | 已安装桌面运行库 |

两版功能相同，内置中英切换。EXE 可独立运行；ZIP 包含双语说明和许可证，下载校验见 `SHA256SUMS.txt`。

### 升级

暂停并关闭旧工具，打开新版，点击「连接／更新组件」，连接完成后再开始。保留已有设置及种植进度。

## English

### Changes

- Makes water impassable except through an existing bridge's actual crossing corridor. Adjacent water, placement previews and removed bridges stay blocked.
- Applies the water guard to A*, optional native navigation, cached routes, movement trials and recovery dashes. A broad floor collider no longer counts as a path across water.
- Checks every crossed terrain-cell boundary, preventing diagonal shortcuts through water between two dry endpoints.

### Downloads

| Build | Runtime | Recommended for |
| --- | --- | --- |
| Portable | Includes .NET | Most users |
| Lite | .NET Desktop Runtime 8 x64 | Users with the desktop runtime installed |

Both builds have the same features and include Chinese/English switching. Each EXE runs on its own; ZIPs include bilingual documentation and licenses. Verify downloads with `SHA256SUMS.txt`.

### Upgrade

Pause and close the old tool, open the new build and select **Connect / update component** before starting. Existing settings and planting progress are retained.

---

# BD2 Territory v0.3.6-beta.1

## 简体中文

### 更新内容

- 修复矿点附近目标站位落入角色碰撞范围的问题；确认到不了的站位会换面重试。
- 修复领地地形台阶被误判为障碍：按连续台阶高度规划，区分上下层缓存，并读取游戏保存的正常跨阶高度。
- 区分行走时的跨阶空间和采集时的停止站位，保留 NPC／工人的原生碰撞过滤规则。

### 下载

| 版本 | 运行环境 | 建议 |
| --- | --- | --- |
| Portable | 自带 .NET | 大多数用户 |
| Lite | .NET Desktop Runtime 8 x64 | 已安装桌面运行库 |

两版功能相同，内置中英切换。EXE 可独立运行；ZIP 包含双语说明和许可证，下载校验见 `SHA256SUMS.txt`。

### 升级

暂停并关闭旧工具，打开新版，点击「连接／更新组件」，连接完成后再开始。保留已有设置及种植进度。

## English

### Changes

- Fixes gathering destinations that overlap the player's collision body near ore. An unreachable final stand is retired before trying another side.
- Fixes false obstacles on built-in terrain stairs by following consecutive tread heights, separating floor-level caches and reading the game's saved normal step height.
- Separates stepping clearance while walking from final gathering clearance, retaining native collision filtering for NPCs and workers.

### Downloads

| Build | Runtime | Recommended for |
| --- | --- | --- |
| Portable | Includes .NET | Most users |
| Lite | .NET Desktop Runtime 8 x64 | Users with the desktop runtime installed |

Both builds have the same features and include Chinese/English switching. Each EXE runs on its own; ZIPs include bilingual documentation and licenses. Verify downloads with `SHA256SUMS.txt`.

### Upgrade

Pause and close the old tool, open the new build and select **Connect / update component** before starting. Existing settings and planting progress are retained.

---

# BD2 Territory v0.3.5-beta.1

## 简体中文

### 更新内容

- 通行预测改用角色实际的碰撞体、碰撞过滤、台阶高度及坡度规则，修正可行走摆设被误判为障碍。
- 移除 NPC 和工人的额外虚拟占位，避免挡住本来可以通行或采集的位置。
- 预测失败时增加有限的正常移动验证；连续 3 秒无推进才暂记障碍，修复重算禁行区把角色困在原地的问题。

### 下载

| 版本 | 运行环境 | 建议 |
| --- | --- | --- |
| Portable | 自带 .NET | 大多数用户 |
| Lite | .NET Desktop Runtime 8 x64 | 已安装桌面运行库 |

两版功能相同，内置中英切换。EXE 可独立运行；ZIP 包含双语说明和许可证，下载校验见 `SHA256SUMS.txt`。

### 升级

暂停并关闭旧工具，打开新版，点击「连接／更新组件」，连接完成后再开始。保留已有设置及种植进度。

## English

### Changes

- Predicts traversal using the character’s actual collider, collision filters, step height and slope rules, reducing false obstacles on walkable decorations.
- Removes synthetic NPC and worker footprints that could block otherwise valid movement or gathering positions.
- Adds bounded ordinary-movement checks when prediction fails. Only 3 seconds of observed stalling creates a temporary exclusion; recovery can leave an overlapping exclusion instead of trapping itself.

### Downloads

| Build | Runtime | Recommended for |
| --- | --- | --- |
| Portable | Includes .NET | Most users |
| Lite | .NET Desktop Runtime 8 x64 | Users with the desktop runtime installed |

Both builds have the same features and include Chinese/English switching. Each EXE runs on its own; ZIPs include bilingual documentation and licenses. Verify downloads with `SHA256SUMS.txt`.

### Upgrade

Pause and close the old tool, open the new build and select **Connect / update component** before starting. Existing settings and planting progress are retained.

---

# BD2 Territory v0.3.4-beta.1

## 简体中文

### 更新内容

- 修复重新开始后沿用旧空田统计，导致已经预览 100 格却提示农田不连通的问题。
- 区分空田缓存待核对、真实占用、地块身份异常和预览数量不足；等待核对期间不付款，数量异常时尝试重新生成一次游戏预览。
- 修复部分非默认料理在播种确认时被错误拦截的问题，固定作物种植同步修复。
- 包含上一候选版的固定作物选择、自适应 A* 网格和导航缓存。

### 下载

| 版本 | 运行环境 | 建议 |
| --- | --- | --- |
| Portable | 自带 .NET | 大多数用户 |
| Lite | .NET Desktop Runtime 8 x64 | 已安装桌面运行库 |

两版功能相同，内置中英切换。EXE 可独立运行；ZIP 包含双语说明和许可证，下载校验见 `SHA256SUMS.txt`。

### 升级

暂停并关闭旧工具，打开新版，点击「连接／更新组件」，连接完成后再开始。保留已有设置及种植进度，无需为此错误重新摆放农田。

## English

### Changes

- Fixes restarting with stale empty-field counts, which could report disconnected fields even when the game preview already contained 100 cells.
- Separates pending cache checks, occupied fields, invalid field identities and incorrect preview sizes. No payment occurs while checks are pending; an incorrect preview size triggers one native refresh.
- Fixes planting confirmation incorrectly rejecting some non-default recipes, including fixed-crop planting with those recipes.
- Includes fixed-crop selection, adaptive A* grids and navigation caching from the previous candidate.

### Downloads

| Edition | Runtime requirement | Recommended for |
| --- | --- | --- |
| Portable | Includes .NET | Most users |
| Lite | .NET Desktop Runtime 8 x64 | Desktop runtime already installed |

Both editions include Chinese and English. The EXE runs independently; ZIP packages include bilingual documentation and licenses. Verify downloads with `SHA256SUMS.txt`.

### Upgrade

Pause and close the old tool, open the new version and select **Connect / update component**. Start after connecting. Existing settings and planting progress are kept; this error does not require rearranging fields.

---

# BD2 Territory v0.3.3-beta.1

## 简体中文

### 更新内容

- 新增「固定种类作物」：连接后选择已解锁作物，每批 100 格持续种植；原有按料理配比种植仍可选择。
- 切换种植方式或作物先等待当前播种确认，保留预算与进度。固定种植也可独立开启自动料理。
- 修复采石场等窄出口可能被粗网格漏判的问题；必要时自动细化网格。
- 缓存地面检测和已找到的通路，空闲时少量预热，减少重复规划等待；执行时仍实时检查工人和障碍。
- 修复起点与工人占位重叠时无法向外规划，以及到达受阻后原地停等的问题。默认继续使用 A*，NavMesh 保持可选且默认关闭。

### 下载

| 版本 | 运行环境 | 建议 |
| --- | --- | --- |
| Portable | 自带 .NET | 大多数用户 |
| Lite | .NET Desktop Runtime 8 x64 | 已安装桌面运行库 |

两版功能相同，内置中英切换。EXE 可独立运行；ZIP 包含双语说明和许可证，下载校验见 `SHA256SUMS.txt`。

### 升级

暂停并关闭旧工具，打开新版，点击「连接／更新组件」，连接完成后再开始。已有种植设置保留，默认沿用料理配比；需要固定作物时在「种植与料理」切换。

## English

### Changes

- Adds **Plant a fixed crop**: choose an unlocked crop after connecting and keep planting it in batches of 100. Recipe balancing remains available.
- Planting changes wait for the current batch to be confirmed and preserve spending and progress. Automatic cooking can also be used with fixed planting.
- Refines the grid when a narrow quarry exit cannot be found on the coarse grid.
- Caches ground checks and discovered routes, with small idle-time warmups to reduce repeated planning. Workers and obstacles are still checked live while moving.
- Fixes planning when a worker overlaps the starting position, and waiting at an obstructed arrival point. A* remains the default; NavMesh is optional and off by default.

### Downloads

| Edition | Runtime requirement | Recommended for |
| --- | --- | --- |
| Portable | Includes .NET | Most users |
| Lite | .NET Desktop Runtime 8 x64 | Desktop runtime already installed |

Both editions include Chinese and English. The EXE runs independently; ZIP packages include bilingual documentation and licenses. Verify downloads with `SHA256SUMS.txt`.

### Upgrade

Pause and close the old tool, open the new version and select **Connect / update component**. Start after connecting. Existing planting preferences are kept and default to recipe balancing; choose fixed planting under **Planting and cooking** when needed.

---

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
