# 0.3.14-beta.1

[简体中文](#简体中文) · [English](#english)

## 简体中文

修复点击连接后可能出现的工具未响应。连接期间停止重复轮询，状态、诊断与布局读取全部移到后台；暂停立即撤销本地控制状态，关闭窗口采用有界等待。补齐接口识别、注入和组件交接的分阶段日志，即使组件未启动也能排查。

保留此前的容量保护、收获后集中补种再采矿砍树，以及分散农田批量播种。连接与开始自动化仍是两个独立操作。

## English

Fixes possible freezes after clicking Connect. Polling pauses during connection, while snapshot, diagnostic and layout reads run in the background. Pausing revokes local control immediately; closing has a bounded wait. Connection logs now cover interface preparation, injection and component handoff before a hook is ready.

Preserves stock capacity protection, harvest → replant → mine/log scheduling, and native batch planting on disconnected field groups. Connecting does not start automation.

| 版本 / Edition | 说明 / Requirements |
| --- | --- |
| Portable | 单文件，内置 .NET / Single EXE, includes .NET |
| Lite | 单文件，需要 .NET 8 Desktop Runtime x64 / Single EXE, requires .NET 8 Desktop Runtime x64 |

暂停并关闭旧工具，启动新版后点击连接；游戏可保持运行。Stop and close the previous tool, launch this version and connect; the game can remain open.
