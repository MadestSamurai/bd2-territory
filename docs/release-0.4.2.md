# BD2 Territory v0.4.2

## 简体中文

### 更新内容

- 修复旧售卖记录长期显示“结果未知”，手动清理库存后仍无法开始自动化的问题。
- 缺失确认时，等待游戏请求结束，再重新读取服务器领地库存和领地币；保留旧记录，按当前余量继续，不重复执行旧售卖。
- 已确认的售卖不再因后续采集、料理或手动操作改变库存而卡住。未确认交易不会计入已确认售卖统计。
- 补充售卖请求、库存核对与恢复记录，并覆盖配方切换后费用核对的回归测试。

### 下载

| 版本 | 运行环境 | 建议 |
| --- | --- | --- |
| **Portable** | 内置 .NET | 大多数用户，下载即用 |
| **Lite** | 需要 [.NET Desktop Runtime 8 x64](https://dotnet.microsoft.com/download/dotnet/8.0) | 已安装运行时，下载更小 |

适用于 Windows x64。EXE 可独立运行，无需 Python 或其他工具；ZIP 附双语说明与许可证。使用 `SHA256SUMS.txt` 校验下载。

### 升级

暂停并关闭旧工具，打开新版，点击「连接／更新组件」，连接后开始自动化。保留原有设置、种植进度和售卖记录，不需要手动删除数据。如果旧组件已被未确认售卖卡住、无法交接，正常重启游戏一次后再连接新版。

[使用说明与风险提示](https://github.com/MadestSamurai/bd2-territory/blob/main/README.md)

## English

### Changes

- Fixes old sales remaining pending indefinitely, even after stock is cleared manually.
- When confirmation is missing, waits for game requests to finish and refreshes territory inventory and currency from the server. Preserves the old record and replans from current stock without replaying the old sale.
- Confirmed sales no longer become stuck when gathering, cooking or manual actions subsequently change balances. Unconfirmed transactions are not included in confirmed sales totals.
- Adds sale dispatch, inventory reconciliation and recovery diagnostics, plus regression coverage for payment checks after changing recipes.

### Downloads

| Edition | Runtime requirement | Recommended for |
| --- | --- | --- |
| **Portable** | .NET included | Most users; download and run |
| **Lite** | Requires [.NET Desktop Runtime 8 x64](https://dotnet.microsoft.com/download/dotnet/8.0) | Smaller download with an installed runtime |

For Windows x64. EXEs run independently without Python or other tools; ZIPs include bilingual documentation and licenses. Verify downloads with `SHA256SUMS.txt`.

### Upgrade

Pause and close the previous tool, open this version, select **Connect / update component**, then start automation. Keep existing settings, planting progress and sale records; no manual data deletion is required. If an old component is already stuck waiting for a sale and refuses handoff, restart the game normally once before connecting this version.

[Usage and risk notice](https://github.com/MadestSamurai/bd2-territory/blob/main/README.en.md)