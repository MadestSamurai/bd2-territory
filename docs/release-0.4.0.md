# BD2 Territory v0.4.0

## 简体中文

### 更新内容

- 首个正式版，软件窗口、程序版本与下载包统一为 0.4.0。
- 修复连接时界面未响应：状态、诊断和布局读取在后台执行，暂停立即生效，关闭窗口不再无限等待。
- 支持兼容组件在同一游戏进程内更新和交接，补齐连接各阶段的诊断日志。
- 容量保护始终开启，只出售超出保留阈值的部分。阈值为 100–9900，默认 9900；保留物品分类和锁定检查。
- 收完本批农作物后集中补种，再采矿与砍树；按实际农田分组批量播种，不要求 100 格或全部连片。
- 保留配方与固定作物种植、自动料理、A* 寻路、布局预检与导入，以及中英语言切换。

### 下载

| 版本 | 运行环境 | 建议 |
| --- | --- | --- |
| **Portable** | 内置 .NET | 大多数用户，下载即用 |
| **Lite** | 需要 [.NET Desktop Runtime 8 x64](https://dotnet.microsoft.com/download/dotnet/8.0) | 已安装运行时，下载更小 |

适用于 Windows x64。EXE 可独立运行，无需 Python 或其他工具；ZIP 附双语说明与许可证。使用 `SHA256SUMS.txt` 校验下载。

### 升级

暂停并关闭旧工具，打开新版，点击「连接／更新组件」，连接完成后再开始。已有设置及种植进度保留；支持交接的组件无需重启游戏，旧组件首次迁移可能需要正常重启游戏一次。

[使用说明与风险提示](https://github.com/MadestSamurai/bd2-territory/blob/main/README.md)

## English

### Changes

- First stable release. Application windows, version metadata and downloads use 0.4.0.
- Fixes connection freezes: state, diagnostic and layout reads run in the background. Pause takes effect immediately; closing no longer waits indefinitely.
- Supports compatible component updates and handoff within the same game process, with diagnostics for each connection stage.
- Capacity protection stays enabled and sells only stock above the retained quantity. Set the threshold from 100 to 9900, default 9900; item eligibility and lock checks remain in place.
- Harvests the current crop batch and replants before mining and logging. Batch planting uses actual field groups without requiring 100 connected fields.
- Retains recipe and fixed-crop planting, automatic cooking, A* routing, layout preview/import and Chinese/English switching.

### Downloads

| Edition | Runtime requirement | Recommended for |
| --- | --- | --- |
| **Portable** | .NET included | Most users; download and run |
| **Lite** | Requires [.NET Desktop Runtime 8 x64](https://dotnet.microsoft.com/download/dotnet/8.0) | Smaller download with an installed runtime |

For Windows x64. EXEs run independently without Python or other tools; ZIPs include bilingual documentation and licenses. Verify downloads with `SHA256SUMS.txt`.

### Upgrade

Pause and close the previous tool, open this version and select **Connect / update component**. Start after connection completes. Existing settings and planting progress are retained. Components supporting handoff do not require a restart; older components may require one normal game restart during migration.

[Usage and risk notice](https://github.com/MadestSamurai/bd2-territory/blob/main/README.en.md)
