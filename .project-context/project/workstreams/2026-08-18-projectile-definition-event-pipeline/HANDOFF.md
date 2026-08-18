# 子弹定义与模块化骰面事件管线交接

## 当前状态

- 运行时代码、Prefab、SO 资源、六面绑定和测试代码均已写入；最终 Unity Test Runner 与 PlayMode 验证被许可证 IPC 阻塞。

## 当前方案及原因

- 使用每次攻击独立的激活上下文关联基础事件、延迟弹丸与命中事件，避免全局事件总线和 Prefab 反向依赖左轮。
- 弹丸定义拥有全部运行时属性；生成事件只选择定义、延迟、主弹身份和攻击特效覆盖策略。
- 主弹始终允许分发本次激活的命中事件；非主弹使用 `UseProjectileDefault`、`ForceEnabled` 或 `ForceDisabled` 判定。
- `fire_1.prefab` 由 `ProjectileVisualWrapper` 在运行时实例化，原始资源不带弹丸逻辑且未被修改。

## 尚未完成

- Unity Test Runner 尚未实际执行；隔离批处理在许可证 IPC 阶段超时。
- 尚未在 Play Mode 确认 `fire_1` 的包装尺寸、朝向和运动观感。
- `ExplosionOnHitEffect.asset` 仍没有爆炸弹丸定义，因此 BlastRound 只会 warning，不会生成正式爆炸。

## 下一步首个动作

- 在已打开的 Unity 主编辑器按 `Ctrl+R` 刷新资源，然后直接运行完整 EditMode 测试；不要先重写或重新生成任何 Prefab/SO。

## 首先读取

- [设计规格](../../../../docs/superpowers/specs/2026-08-18-projectile-definition-event-pipeline-design.md)
- [实施计划](../../../../docs/superpowers/plans/2026-08-18-projectile-definition-event-pipeline.md)
- [项目状态](../../STATUS.md)

## 必须保护

- 除 `DiceFaceLoadout` 新增六个基础事件引用外，不修改 Player Prefab 其他序列化数据。
- 不修改 AimRoot 子物体、sorting layer、角色 Transform 或现有左轮调参。
- 不修改原始 `fire_1.prefab`、材质和粒子参数。

## 最近验证

- [passed] `2026-08-18`：完成 `fire_1.prefab` 静态结构与材质引用检查。
- [passed] `2026-08-18`：设计规格自审未发现占位内容或规则冲突。
- [passed] `2026-08-18`：运行时、Editor、测试程序集通过 Unity C# 响应文件编译，exit code 均为 `0`。
- [passed] `2026-08-18`：静态资源契约与 Player Prefab 限定差异检查通过。
- [blocked] `2026-08-18`：隔离 Unity Test Runner 因 `LicenseClient-dslia` channel refused 和连续超时未启动。
- [not-run] `2026-08-18`：PlayMode 射击与视觉验证未运行。

## 风险与不可假定事项

- `fire_1` 只经静态结构确认可作为视觉子 Prefab，最终尺寸和粒子滞留仍需 Play Mode 验证。
- 事件管线已迁移，编译和静态契约通过，但在 Test Runner 恢复前不能声称完整回归通过。
- 当前爆炸 Prefab 尚未配置，本轮不应假定 `BlastRound` 已有最终爆炸表现。
- 不要运行完整 `TopDownPrototypeSceneBuilder`；它可能重建并覆盖用户维护的角色和场景数据。
