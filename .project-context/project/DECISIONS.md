# DiceRevolver 架构决策日志

## 记录规则

每项决策记录日期、状态、背景、决定、原因、影响、替代方案和相关资料。旧决策被替代时追加新决策并链接，不静默改写历史。

## ADR-001：骰面词条使用 ScriptableObject

- 日期：`2026-08-16`
- 状态：`accepted`
- 背景：骰面需要组合弹丸属性和多个事件效果，同时保持 UI、枪械与弹丸运动低耦合。
- 决定：使用 `DiceFaceEntry` 保存词条数据，使用 `BulletEventEffect` ScriptableObject 子类实现事件。
- 原因：设计者可以通过资源组合构筑内容，枪械只依赖稳定数据和事件接口。
- 影响：运行时装备保存资源引用；新增效果通常通过新 ScriptableObject 类型扩展。
- 替代方案：把所有效果硬编码进枪械，或使用字符串事件表；两者都会提高耦合或降低类型安全。
- 相关资料：[骰面构筑系统设计](../../docs/superpowers/specs/2026-08-16-dice-face-build-system-design.md)

## ADR-002：上下文采用可移植框架与项目实例分层

- 日期：`2026-08-17`
- 状态：`accepted`
- 背景：项目会在多个设备和 Codex 任务中运行，并希望通过复制一个文件夹复用到其他项目。
- 决定：除根目录薄 `AGENTS.md` 外，把通用协议、模板和脚本放在 `.project-context/framework/`，把项目事实和工作流放在 `.project-context/project/`。
- 原因：框架可以独立复制和升级，项目资料不会在普通升级中被覆盖；独立工作流降低并发冲突。
- 影响：复制后必须运行安装器完成安全重新绑定；根入口托管区块包含实例 UUID。
- 替代方案：单文件总账、分散的 `docs/context` 与 `scripts`、设备级 Codex Skill。前两者不利于并发或复制，后者削弱仓库自包含能力。
- 相关资料：[可移植上下文系统设计](../../docs/superpowers/specs/2026-08-17-project-context-system-design.md)
