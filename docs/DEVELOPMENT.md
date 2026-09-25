# Development / 开发

Build on Windows with .NET 8 SDK. `build.ps1 -Locked` runs synthetic navigation, farming, cooking and component lifecycle tests without game data.

`package.ps1 -Locked` builds Portable and Lite, tests both packaged GUIs with isolated temporary data, verifies their embedded identity and runtime configuration, and emits checksums. It does not connect to a game. Optional `-ClientManaged <path>` compiles the adapter against an installed client without connecting.

`compatibility-cli` checks a local client. `contract.json` contains interface signatures, not game assemblies or tables. `diagnostics-cli connect` explicitly connects for development. Never commit runtime snapshots, credentials or journals.

自动料理走游戏原生原料选择和制作接口。调用前保存按账号区分的意图，观察原生响应并核对配方、份数和成品奖励，接受后才记为完成。结果未知保留 pending，不重新发送；暂停不撤销已提交请求。

Keep user-facing text in both locale catalogs. Game names and original diagnostic evidence retain the client's language.

## Navigation modes

`TerritorySettings.UseNavMesh` defaults to false and is shared by persisted settings and the live control lease. `RuntimeRouting.cs` owns the three dispatch points: initial approach, movement tick and post-dash resumption. The default dispatch never invokes native path planning. Native mode may fall back to A* for physical obstacles; the reverse fallback is forbidden.

The A* adapter uses ground raycasts and capsule sweeps with the actual CharacterController shape, skin tolerance, collision pairs/layers, step height and slope limit. `CharController` executes directions; vehicles only affect speed. Stopping/resetting an existing NavMeshAgent path do not request native navigation. On disabling native mode during a dash, finish the dash before replanning. Enabling native mode leaves the current A* route intact until the next approach.

`tests/RoutingAdapterTests.cs` compiles the production dispatch unchanged against a minimal movement fixture. It checks old settings, both JSON readers, failed routes, in-flight toggles and dash recovery. `LocalNavigationTests` covers fences, enclosed targets, narrow interaction stands, node budgets and retry fairness. Packaged GUI smoke checks exercise the saved preference, control lease, reopening and both languages. None of these tests claim live terrain verification.

Prediction is not an exact dry run of Unity’s native `CharacterController.Move`. The game temporarily disables stepping after a failed side collision; the adapter resolves the game's saved normal step-height field from the installed client's IL and reads it for planning, without changing the game controller. Root height is derived from the actual capsule foot, not NavMeshAgent.baseOffset. Only sustained observed stalls add attempt-local exclusions. Strict prediction falls back to a bounded ground-supported trial using normal directional input; it never disables collision or teleports. Trial geometry is not cached as confirmed physical clearance. `traversal-tests` compiles the production predictor and follower against query fixtures, covering false positives, actual stalls and native movement requests. Live Unity traversal remains a separate verification step.

Traversal regression coverage includes the captured rotated ore box and full-radius controller contact, short remaining-distance stalls, consecutive native-style treads with capsule overlap, and low-floor failures followed by higher-floor samples of the same X/Z. Intermediate route points can use legal step clearance; final gathering stands always require full-body clearance, including during fallback. Ground samples include height in cache identity and the root's skin contact offset. Scene diagnostics record the effective step height, root lift and native controller dimensions. These fixture checks do not substitute for live Unity physics verification.
