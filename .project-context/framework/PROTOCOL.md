# Codex 项目上下文协议

## 启动读取顺序

1. 读取项目根目录 `AGENTS.md`。
2. 读取本协议。
3. 读取 `.project-context/project-context.json`。
4. 读取 `.project-context/project/PROJECT.md`、`STATUS.md` 和 `ENVIRONMENT.md`。
5. 根据用户请求、当前分支和状态索引定位工作流。
6. 读取工作流的 `STATE.md`、`HANDOFF.md` 及其链接资料。
7. 使用 Git、代码和实际验证结果核对记录。

完成必要读取前不得修改项目。纯只读任务也必须复核上下文；没有发现事实或状态变化时可以不改文件。

## 开始或继续工作流

- 新事项使用 `.project-context/project/workstreams/YYYY-MM-DD-topic-slug/`。
- `topic-slug` 只包含小写英文字母、数字和连字符。
- 合法状态为 `planned`、`active`、`blocked`、`completed` 和 `superseded`。
- 优先继续当前分支对应的活跃工作流；不能确定时先核对 Git 和用户目标。

## 执行期间

- 需求、范围、关键假设或架构决定变化时更新工作流状态。
- 影响全局架构时追加 `DECISIONS.md`，不静默改写旧决策。
- 多分支通常只编辑各自工作流；共享资料只记录稳定项目事实。
- 不保存聊天全文、私有推理或无价值命令输出。

## 结束前强制更新

1. 更新工作流 `STATE.md` 的真实状态。
2. 重写 `HANDOFF.md`，明确下一位 Codex 的首个动作。
3. 记录涉及文件、验证命令、实际结果和日期。
4. 同步受影响的共享项目资料。
5. 检查秘密、临时路径、失效占位符和未经验证的完成声明。
6. 运行上下文检查器。
7. 仅随正常任务提交上下文更新，不擅自制造例行文档提交。

检查失败时不得声称任务完成；可以记录为阻塞并留下可执行交接。

## 事实优先级

实际代码与资源、Git 状态、刚执行的验证结果优先于上下文记录。发生冲突时先确认事实，再修正文档并说明依据。

## 并发与合并

- 每项工作维护独立工作流目录，避免共享详细进度文件。
- `STATUS.md` 只保存索引和全局事实。
- 合并冲突时保留双方工作流，再根据合并后的仓库事实重建索引。
- 历史工作流不自动删除；被替代时使用 `superseded` 并链接继任事项。

## 安全规则

- 不记录密钥、令牌、个人信息或设备临时路径。
- 不覆盖用户未授权的改动、项目资料或受保护路径。
- 验证必须使用 `passed`、`failed`、`not-run` 或 `blocked`，并记录实际依据。
- `not-run` 表示未尝试；`blocked` 表示尝试执行但被明确条件阻止。

## 校验命令

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .project-context/framework/scripts/check.ps1
```
