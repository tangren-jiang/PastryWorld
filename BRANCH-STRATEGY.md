# 分支策略

## 分支模型

| 分支 | 用途 | 规则 |
|---|---|---|
| main | 稳定主干 | 永远可编译可运行，每周从develop合并 |
| develop | 开发集成 | 日常合并目标，PR目标分支 |
| feature/* | 功能分支 | 如 feature/T5-input-system |
| fix/* | 修复分支 | 如 fix/memory-leak |
| spike/* | 技术验证 | 如 spike/T22-ui-framework |

## 合并规则

- feature → develop：PR + 至少1人Review
- develop → main：每周定期合并，打Tag
- main 上的 Tag 格式：v0.1.0（Demo阶段从v0.x开始）
