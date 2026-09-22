# Localization / 本地化

One executable supports `zh-CN` and `en-US`. UI labels are defined in `desktop/UiLabels.cs`; translations live in `localization/*.json`. Keep catalog keys identical and preserve placeholders.

Language is saved separately in `language.json`. Switching must not write automation settings, restart a task or alter a control lease. Packaged GUI tests check this and static label coverage.

料理、作物、设施名称由游戏动态提供，不附带静态翻译表。原始日志与底层异常保留原文；常见状态须提供完整的英文句子。
