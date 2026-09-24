# Development / 开发

Build on Windows with .NET 8 SDK. `build.ps1 -Locked` runs synthetic navigation, farming, cooking and component lifecycle tests without game data.

`package.ps1 -Locked` builds Portable and Lite, tests both packaged GUIs with isolated temporary data, verifies their embedded identity and runtime configuration, and emits checksums. It does not connect to a game. Optional `-ClientManaged <path>` compiles the adapter against an installed client without connecting.

`compatibility-cli` checks a local client. `contract.json` contains interface signatures, not game assemblies or tables. `diagnostics-cli connect` explicitly connects for development. Never commit runtime snapshots, credentials or journals.

自动料理走游戏原生原料选择和制作接口。调用前保存按账号区分的意图，观察原生响应并核对配方、份数和成品奖励，接受后才记为完成。结果未知保留 pending，不重新发送；暂停不撤销已提交请求。

Keep user-facing text in both locale catalogs. Game names and original diagnostic evidence retain the client's language.

## Navigation modes

`TerritorySettings.UseNavMesh` defaults to false and is shared by persisted settings and the live control lease. `RuntimeRouting.cs` owns the three dispatch points: initial approach, movement tick and post-dash resumption. The default dispatch never invokes native path planning. Native mode may fall back to A* for physical obstacles; the reverse fallback is forbidden.

The A* adapter uses ground raycasts, capsule sweeps and live NPC occupancy. `CharController` executes directions; vehicles only affect speed. Stopping/resetting an existing NavMeshAgent path and reading its base offset do not request native navigation. On disabling native mode during a dash, finish the dash before replanning. Enabling native mode leaves the current A* route intact until the next approach.

`tests/RoutingAdapterTests.cs` compiles the production dispatch unchanged against a minimal movement fixture. It checks old settings, both JSON readers, failed routes, in-flight toggles and dash recovery. `LocalNavigationTests` covers fences, enclosed targets, narrow interaction stands, node budgets and retry fairness. Packaged GUI smoke checks exercise the saved preference, control lease, reopening and both languages. None of these tests claim live terrain verification.
