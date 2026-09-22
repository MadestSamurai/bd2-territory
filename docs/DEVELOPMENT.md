# Development / 开发

Build on Windows with .NET 8 SDK. `build.ps1 -Locked` runs synthetic navigation, farming, cooking and component lifecycle tests without game data.

`package.ps1 -Locked` builds Portable and Lite, tests both packaged GUIs with isolated temporary data, verifies their embedded identity and runtime configuration, and emits checksums. It does not connect to a game. Optional `-ClientManaged <path>` compiles the adapter against an installed client without connecting.

`compatibility-cli` checks a local client. `contract.json` contains interface signatures, not game assemblies or tables. `diagnostics-cli connect` explicitly connects for development. Never commit runtime snapshots, credentials or journals.

自动料理走游戏原生原料选择和制作接口。调用前保存按账号区分的意图，观察原生响应并核对配方、份数和成品奖励，接受后才记为完成。结果未知保留 pending，不重新发送；暂停不撤销已提交请求。

Keep user-facing text in both locale catalogs. Game names and original diagnostic evidence retain the client's language.
