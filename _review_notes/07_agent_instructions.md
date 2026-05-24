# Agent 执行指令

提示词文件位置：E:\KK_VR\_review_notes\06_consolidated_prompts.md

---

## 第一轮（并行，同时给 2 个 Agent）

### Agent A — 暗角系统

```
读取文件 E:\KK_VR\_review_notes\06_consolidated_prompts.md 中的 "Prompt 1: VRComfortVignette.cs（新文件）" 部分，按照其中的指示创建新文件。不要修改任何其他文件。
```

### Agent B — 双手缩放

```
读取文件 E:\KK_VR\_review_notes\06_consolidated_prompts.md 中的 "Prompt 2: VRTwoHandScale.cs（新文件）" 部分，按照其中的指示创建新文件。不要修改任何其他文件。
```

---

## 第二轮（等第一轮完成后）

### Agent C — 设置 + GUI + 注册

```
读取文件 E:\KK_VR\_review_notes\06_consolidated_prompts.md 中的 "Prompt 3: 设置 + GUI + VRLoader 注册" 部分，按照其中的指示修改 3 个文件：KKCharaStudioVRSettings.cs、KKCharaStudioVRGUI.cs、VRLoader.cs。不要修改任何其他文件。
```

---

## 第三轮（等第二轮完成后）

### Agent D — 核心工具修改

```
读取文件 E:\KK_VR\_review_notes\06_consolidated_prompts.md 中的 "Prompt 4: GripMoveKKCharaStudioTool.cs（所有行为修改合并）" 部分，按照其中的指示修改 GripMoveKKCharaStudioTool.cs。共 10 处修改，请严格按顺序执行。不要修改任何其他文件。
```

---

## 第四轮（等第三轮完成后）

### Agent E — 手部触摸变色

```
读取文件 E:\KK_VR\_review_notes\06_consolidated_prompts.md 中的 "Prompt 5: 手部触摸变色反馈" 部分，按照其中的指示修改 VRHandModelManager.cs 和 VRHandHapticTrigger.cs。不要修改任何其他文件。
```

---

## 全部完成后

把结果带回给 Claude 审查。
