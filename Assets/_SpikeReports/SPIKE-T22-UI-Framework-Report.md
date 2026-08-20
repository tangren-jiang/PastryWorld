# SPIKE-T22 UI 框架选型报告

> 报告编号：SPIKE-T22 · 日期：2026-08-__ · 执行人：程基岩

## 1. 对比表

| 维度 | UGUI | UI Toolkit | 判定 |
|---|---|---|---|
| 手账纸张纹理还原 | Image+Shader，灵活 | USS background-image，受限 | |
| 水墨边缘效果 | 自定义Shader，好 | USS不支持自定义Shader | |
| 复杂列表性能 | ScrollRect虚拟化一般 | 内置虚拟化，更好 | |
| 翻页动画 | DOTween，成熟 | Transition API，原生 | |
| 开发效率 | 熟悉，快 | 需学习USS，慢 | |
| 文字排版 | TextMeshPro，强大 | TextCore，接近 | |
| 生态/社区 | 成熟，资源多 | 新，资源少 | |
| 手账风格还原度 | __分 | __分 | |

## 2. 实测指标

### UGUI 原型
- 开发耗时：__ 分钟
- 代码行数：__ 行
- 纸张纹理还原度：__ /5
- 水墨边缘效果：__ /5
- 翻页动画流畅度：__ /5
- 截图：`_SpikeReports/img/ugui.png`

### UI Toolkit 原型
- 开发耗时：__ 分钟
- USS 行数：__ 行
- 纸张纹理还原度：__ /5
- 水墨边缘效果：__ /5
- 翻页动画流畅度：__ /5
- 截图：`_SpikeReports/img/uitoolkit.png`

## 3. 结论（二选一）

- [ ] 方案A：图鉴UI用 UGUI（水墨Shader需求高）
- [ ] 方案B：图鉴UI用 UI Toolkit（排版优势明显）

## 4. 待确认项关闭

- T22-2「UI Toolkit vs UGUI 手账风格还原度」→ 已关闭，决策：____
