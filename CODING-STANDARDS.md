# 点心世界 代码规范

## 1. 命名规范

| 类型 | 规范 | 示例 |
|---|---|---|
| 命名空间 | PastryWorld.{Module} | PastryWorld.Craft |
| 类名 | PascalCase | CraftStepWeighing |
| 方法名 | PascalCase | OnInputBegin |
| 私有字段 | _camelCase | _currentTolerance |
| 公开属性 | PascalCase | PointerPosition |
| 常量 | UPPER_SNAKE_CASE | MAX_TOLERANCE |
| 接口 | I 前缀 | IInputProvider |

## 2. ScriptableObject 命名

- 文件名格式：{Type}_{Name}（如 StepConfig_Weighing）
- 存放路径：Assets/ScriptableObjects/{Module}/{Type}/
- 类名后缀：SO 或 Config

## 3. 注释规范

- 公开类/方法必须有 XML 注释（///）
- 私有复杂方法加简要注释
- TODO格式：// TODO(T5): 实现触摸压力检测
- FIXME格式：// FIXME: 此处假设单点触摸

## 4. 文件夹结构

```
Assets/
├── Scripts/
│   ├── Input/          ← 输入抽象层 (T5)
│   ├── Craft/          ← 制作系统 (T1-T8)
│   ├── Exploration/    ← 探索系统 (T9-T12)
│   ├── Narrative/      ← 叙事系统 (T13-T16)
│   ├── Reality/        ← 现实世界系统 (T17-T20)
│   ├── Codex/          ← 图鉴系统 (T21-T23)
│   ├── Core/           ← 核心框架
│   ├── Utils/          ← 工具类
│   └── Editor/         ← Editor脚本
├── ScriptableObjects/  ← 按模块分目录
├── Settings/           ← Input/URP/UI 配置资产
├── Art/                ← Characters/Environments/UI/Effects
├── Scenes/             ← Bootstrap/Main/...
├── Prefabs/
├── _SpikeReports/      ← Spike报告
└── Resources/          ← 最小化使用
```

## 5. Assembly Definition

- 每个模块目录创建 .asmdef 文件
- 模块间通过 asmdef 引用，实现编译隔离
- 模块之间不互相引用！跨模块通信通过 Core 层的 EventBus

## 6. Git 提交规范

- 格式：[模块] 类型: 简述
- 类型：feat / fix / refactor / docs / chore / spike
- 示例：[T5] feat: 实现 MouseInputProvider 指针位置读取
