# 子弹定义与模块化骰面事件管线

- ID: `2026-08-18-projectile-definition-event-pipeline`
- Status: `completed`
- Branch: `codex/finish-projectile-event-pipeline`
- Created: `2026-08-18`
- Updated: `2026-08-18`

## 目标

- 新增子弹定义 SO 与子弹类库。
- 使用 `fire_1.prefab` 包装基础左轮子弹运行时 Prefab。
- 把骰面基础事件与三阶段构筑事件拆分为独立模块。
- 使用单次骰面激活上下文关联延迟生成、弹丸命中和连锁事件。

## 非目标

- 不新增全局事件总线或全局时间单例。
- 不实现正式爆炸表现、有限敌人生命、穿透消费、对象池或构筑存档。
- 不修改原始 `fire_1.prefab`、角色瞄准、sorting 或现有左轮调参。

## 已确认事实

- 用户批准一次骰面激活上下文方案。
- 六个骰面都使用“发射基础左轮子弹”基础事件。
- `DoubleTap`、`BlastRound`、`LoadedFour` 分别属于开火时、命中时、开火后事件。
- 攻击特效判定同时支持子弹默认值与生成事件覆盖。
- 附加弹丸默认不触发命中事件。
- 每次激活默认事件预算为 `32`。

## 当前正在进行

- 无

## 已完成

- 检查 `fire_1.prefab` 结构、粒子系统和材质引用。
- 完成现有骰面、事件、时间系统和弹丸生成链路的代码核对。
- 用户批准单次骰面激活上下文架构和攻击特效双层判定。
- 设计规格已写入并完成占位、职责和规则一致性自审。
- 新增 `ProjectileDefinition`、`ProjectileDefinitionLibrary` 和可扩展弹丸属性端口；伤害、距离、速度、穿透和默认攻击特效均由弹丸定义拥有。
- 新增每次攻击独立的 `DiceFaceActivation`，包含骰面快照、主弹定义、生成请求、时间系统入口、命中事件关系和默认 `32` 次事件预算。
- 新增通用 `ProjectileSpawnEffect`；六个骰面基础事件均绑定“发射基础左轮子弹”。
- `DoubleTap` 保持 `0.25` 秒延迟，生成同一主弹定义的附加弹，并显式使用 `ForceDisabled` 禁止默认回触命中事件。
- `BlastRound` 保持独立命中事件，爆炸端口迁移为 `ProjectileDefinition`；当前未配置爆炸定义时仍安全跳过并输出 warning。
- `LoadedFour` 保持独立结束开火事件，不依赖主弹或附加弹生成逻辑。
- `DiceRevolverGun` 已改为依次广播开火、基础事件、开火时事件、结束开火、结束开火时事件；实际弹丸由定义生成，允许命中的弹丸再分发该次激活的命中事件。
- 新增基础左轮子弹 Prefab、定义 SO、定义库与基础生成事件；运行时视觉包装组件引用 `fire_1.prefab`，未修改原始粒子 Prefab。
- Player Prefab 新增一个 `DiceFaceLoadout` 和六个基础事件引用；Git 差异确认没有修改角色 Transform、AimRoot、sorting 或左轮调参。
- 新增/更新数据契约、事件策略、命中分发和资源引用测试。
- Unity 许可证 IPC 已恢复，完整 EditMode 回归 `94/94` 通过。
- 修正 `ProjectileDefinitionAssetTests` 对数组使用 NUnit `Has.Count` 导致的测试框架反射错误；聚焦用例 `1/1` 及随后全量回归均通过。
- 关键管线与场景冒烟范围共 27 项测试通过，覆盖基础弹丸资源、六面绑定、事件预算、主弹/附加弹命中策略、场景测试靶和碰撞契约。

## 下一步

1. 后续如需调校视觉或手感，创建新工作流并进入 Play Mode 验证六面基础子弹、DoubleTap 间隔和主弹/附加弹命中表现。
2. 验证 `fire_1` 在 `0.2` 视觉缩放和 Y 轴 `90` 度包装旋转下的尺寸与朝向；如需调整，只修改 `BasicRevolverBullet.prefab` 的 `ProjectileVisualWrapper` 端口。

## 阻塞

- 无。先前许可证 IPC 阻塞在当前会话中已无法复现，Unity 6000.3.10f1 成功连接 `LicenseClient-Administrator` 并完成测试。

## 涉及文件

- `docs/superpowers/specs/2026-08-18-projectile-definition-event-pipeline-design.md`
- `docs/superpowers/plans/2026-08-18-projectile-definition-event-pipeline.md`
- `.project-context/project/STATUS.md`
- `.project-context/project/workstreams/2026-08-18-projectile-definition-event-pipeline/STATE.md`
- `.project-context/project/workstreams/2026-08-18-projectile-definition-event-pipeline/HANDOFF.md`
- `Assets/Scripts/Prototype/ProjectileDefinition.cs`
- `Assets/Scripts/Prototype/ProjectileDefinitionLibrary.cs`
- `Assets/Scripts/Prototype/DiceFaceActivation.cs`
- `Assets/Scripts/Prototype/ProjectileSpawnEffect.cs`
- `Assets/Scripts/Prototype/ProjectileVisualWrapper.cs`
- `Assets/Scripts/Prototype/DiceRevolverGun.cs`
- `Assets/Prefab/Projectiles/BasicRevolverBullet.prefab`
- `Assets/Resources/DiceFacePrototype/Projectiles/BasicRevolverBullet.asset`
- `Assets/Resources/DiceFacePrototype/Projectiles/ProjectileDefinitionLibrary.asset`
- `Assets/Resources/DiceFacePrototype/BulletEvents/FireBasicRevolverProjectile.asset`
- `Assets/Prefab/Player.prefab`
- `Assets/Tests/EditMode/ProjectileDefinitionAssetTests.cs`

## 验证记录

- [passed] `2026-08-18`：`fire_1.prefab` 包含两个 Particle System、完整材质引用，且不包含运行时弹丸组件，符合纯视觉子 Prefab 定位。
- [passed] `2026-08-18`：设计规格占位扫描无结果，主弹、附加弹、攻击特效和六面基础事件规则一致。
- [passed] `2026-08-18`：用户已通过设计并授权直接实施，TDD 实施范围已锁定。
- [passed] `2026-08-18`：使用 Unity 当前生成的三套 C# 响应文件分别编译运行时、Editor 和 EditMode 测试程序集，三个编译命令均返回 exit code `0`；仅有手工调用旧 Mono C# 编译器时的 Unity SourceGenerator 版本 warning。
- [passed] `2026-08-18`：静态资源契约检查通过，确认基础定义属性、Prefab 组件、`fire_1` 引用、六面基础事件绑定和临时目录清理均符合预期。
- [passed] `2026-08-18`：Player Prefab Git 差异只有新增 `DiceFaceLoadout` 组件与六个基础事件引用，受保护的 Transform、sorting 和左轮序列化数据没有变化。
- [failed] `2026-08-18`：许可证恢复后的首次完整 EditMode 回归为 `93/94`；唯一错误是测试对数组使用 NUnit `Has.Count`，并非运行时行为失败。
- [passed] `2026-08-18`：最小修正后失败用例聚焦回归 `1/1` 通过，完整 Unity EditMode 回归 `94/94` 通过。
- [passed] `2026-08-18`：27 项关键事件管线与确定性场景冒烟测试全部通过；本次续作没有修改受保护资产或配置。
- [not-run] `2026-08-18`：最终 PlayMode 射击手感和 `fire_1` 视觉尺寸/朝向人工验证未执行，不阻塞本计划的确定性验证完成。

## 相关资料

- [设计规格](../../../../docs/superpowers/specs/2026-08-18-projectile-definition-event-pipeline-design.md)
