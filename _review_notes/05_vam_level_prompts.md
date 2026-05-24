# VAM-Level VR Experience — Full Architecture + Agent Prompts

与 VAM 的体验差距分析：
1. 没有舒适系统（移动时无暗角/vignette，容易晕）
2. 没有双手缩放（无法双手捏合缩放场景/物体）
3. 触摸角色无法自动选中（必须在树状视图里点选）
4. 高亮球不会脉冲动画（静态的半透明球不够直观）
5. IK 操控是瞬移的（没有弹簧阻尼插值，感觉像提线木偶）
6. 手部触碰角色没有颜色反馈（手模型不变色）
7. 没有抓取距离指示线（抓住 IK 点后看不到连线）

下面按优先级分 3 批，每批可以并行给不同 Agent。

---

## Batch A — 舒适系统 + 高亮动画（2 个新文件 + 3 个修改）

### Agent Prompt A1: VRComfortVignette.cs（新文件）

```
创建新文件：E:\KK_VR\KKCharaStudioVRPlugin\KKCharaStudioVR\VRComfortVignette.cs

背景：Koikatu Chara Studio VR 插件，Unity 5.x，.NET 3.5，LangVersion 11.0。
SDK 风格 csproj 自动包含所有 .cs，不要修改 csproj。

任务：创建一个 VR 舒适暗角（vignette）系统，在用户进行摇杆移动/转向时
渐变显示屏幕边缘的黑色遮罩，减少周边视野以防止 VR 晕动症。

技术要求：
- 使用运行时程序化网格创建环形遮罩（不用外部资源）
- 遮罩渲染在 VR 相机的最前层（overlay）
- 需要平滑的淡入淡出（不能突然出现/消失）

命名空间：KKCharaStudioVR

需要的 using：
- UnityEngine
- VRGIN.Core

类设计：

```csharp
public class VRComfortVignette : MonoBehaviour
{
    public static VRComfortVignette Instance { get; private set; }

    private GameObject _vignetteObj;
    private MeshRenderer _renderer;
    private MeshFilter _meshFilter;
    private Material _material;
    private float _currentIntensity = 0f;  // 0 = no vignette, 1 = full vignette
    private float _targetIntensity = 0f;

    // 由外部调用来设置是否正在移动
    public void SetMoving(bool isMoving)
    {
        _targetIntensity = isMoving ? 1f : 0f;
    }

    void Start()
    {
        Instance = this;
        CreateVignetteMesh();
    }

    void Update()
    {
        // 读取设置
        var settings = VR.Manager?.Context?.Settings as KKCharaStudioVRSettings;
        if (settings == null || !settings.ComfortVignetteEnabled)
        {
            if (_vignetteObj != null) _vignetteObj.SetActive(false);
            _currentIntensity = 0f;
            return;
        }

        // 平滑插值
        float fadeSpeed = 8f;
        _currentIntensity = Mathf.Lerp(_currentIntensity, _targetIntensity, Time.deltaTime * fadeSpeed);

        // 低于阈值时隐藏
        if (_currentIntensity < 0.01f)
        {
            if (_vignetteObj != null) _vignetteObj.SetActive(false);
            _currentIntensity = 0f;
            return;
        }

        if (_vignetteObj != null)
        {
            _vignetteObj.SetActive(true);

            // 跟随 VR 相机
            Transform head = VR.Camera.Head;
            if (head != null)
            {
                _vignetteObj.transform.position = head.position;
                _vignetteObj.transform.rotation = head.rotation;
            }

            // 调整遮罩强度
            float vignetteRadius = settings.ComfortVignetteRadius; // 0.3~0.8
            float alpha = _currentIntensity * 0.95f;
            _material.SetFloat("_Radius", vignetteRadius);
            _material.SetFloat("_Alpha", alpha);
        }
    }

    private void CreateVignetteMesh()
    {
        // 在头部前方创建一个覆盖全视野的矩形面片
        _vignetteObj = new GameObject("VRComfortVignette");
        _vignetteObj.transform.SetParent(null);
        Object.DontDestroyOnLoad(_vignetteObj);

        _meshFilter = _vignetteObj.AddComponent<MeshFilter>();
        _renderer = _vignetteObj.AddComponent<MeshRenderer>();

        // 创建全屏四边形网格（足够大以覆盖整个视野）
        Mesh mesh = new Mesh();
        float size = 0.5f; // 半径
        float dist = 0.15f; // 距离眼睛很近

        // 使用更密集的网格以实现径向渐变
        int segments = 32;
        int rings = 8;
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Color> colors = new List<Color>();

        // 中心点（完全透明）
        verts.Add(new Vector3(0, 0, dist));
        colors.Add(new Color(0, 0, 0, 0));

        for (int r = 1; r <= rings; r++)
        {
            float ringRadius = (float)r / rings * size;
            // 外圈越来越不透明
            float ringAlpha = Mathf.Clamp01(((float)r / rings - 0.4f) / 0.6f);
            ringAlpha = ringAlpha * ringAlpha; // 二次曲线，更自然

            for (int s = 0; s < segments; s++)
            {
                float angle = (float)s / segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * ringRadius;
                float y = Mathf.Sin(angle) * ringRadius;
                verts.Add(new Vector3(x, y, dist));
                colors.Add(new Color(0, 0, 0, ringAlpha));
            }
        }

        // 三角形：中心到第一环
        for (int s = 0; s < segments; s++)
        {
            int next = (s + 1) % segments;
            tris.Add(0);
            tris.Add(1 + s);
            tris.Add(1 + next);
        }

        // 三角形：环与环之间
        for (int r = 0; r < rings - 1; r++)
        {
            int ringStart = 1 + r * segments;
            int nextRingStart = 1 + (r + 1) * segments;
            for (int s = 0; s < segments; s++)
            {
                int next = (s + 1) % segments;
                tris.Add(ringStart + s);
                tris.Add(nextRingStart + s);
                tris.Add(nextRingStart + next);

                tris.Add(ringStart + s);
                tris.Add(nextRingStart + next);
                tris.Add(ringStart + next);
            }
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetColors(colors);
        mesh.RecalculateBounds();
        _meshFilter.mesh = mesh;

        // 使用顶点颜色 shader
        // Unity 内置的 "Particles/Alpha Blended" 或类似 shader
        // 但我们需要一个不受光照影响、总在最前面渲染的 shader
        _material = new Material(Shader.Find("Particles/Alpha Blended"));
        if (_material.shader == null)
        {
            // 回退到 unlit
            _material = new Material(Shader.Find("Unlit/Transparent"));
        }
        _material.renderQueue = 5000; // 最前面
        _material.SetInt("_ZTest", 0); // Always
        _material.SetInt("_ZWrite", 0);
        _renderer.material = _material;
        _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _renderer.receiveShadows = false;

        _vignetteObj.SetActive(false);
        _vignetteObj.layer = 0;
    }

    void OnDestroy()
    {
        if (_vignetteObj != null) Destroy(_vignetteObj);
    }
}
```

注意：这个实现使用顶点颜色来做径向渐变，中心透明，边缘不透明。
`_Radius` 和 `_Alpha` 通过 material.SetFloat 设置，但标准粒子 shader 
可能不支持这些属性。实际实现中应该直接通过修改顶点颜色的 alpha 值来
控制遮罩范围，而不是通过 shader 属性。

修正方案：不用 SetFloat，直接在 Update 中重建顶点颜色：

```csharp
void Update()
{
    // ... 前面的代码不变 ...

    if (_vignetteObj != null && _vignetteObj.activeSelf)
    {
        // 跟随相机
        Transform head = VR.Camera.Head;
        if (head != null)
        {
            _vignetteObj.transform.position = head.position;
            _vignetteObj.transform.rotation = head.rotation;
        }

        // 通过修改顶点颜色控制遮罩
        UpdateVignetteColors(settings.ComfortVignetteRadius, _currentIntensity);
    }
}

private void UpdateVignetteColors(float radius, float intensity)
{
    if (_meshFilter == null || _meshFilter.mesh == null) return;
    Mesh mesh = _meshFilter.mesh;
    Vector3[] vertices = mesh.vertices;
    Color[] newColors = new Color[vertices.Length];

    float dist = 0.15f;
    for (int i = 0; i < vertices.Length; i++)
    {
        Vector3 v = vertices[i];
        float r = Mathf.Sqrt(v.x * v.x + v.y * v.y);
        float maxR = 0.5f;
        float normalizedR = r / maxR;

        // radius 控制透明区域的大小（0.3 = 只有中心30%透明，0.8 = 80%透明）
        float alpha = Mathf.Clamp01((normalizedR - radius) / (1f - radius));
        alpha = alpha * alpha * intensity;
        newColors[i] = new Color(0, 0, 0, alpha);
    }
    mesh.colors = newColors;
}
```

这样就不依赖 shader 属性了。

完整的类应该合并上面的逻辑：Start 创建网格，Update 跟随相机 + 更新颜色。
确保：
- 命名空间 KKCharaStudioVR
- using UnityEngine; using VRGIN.Core; using System.Collections.Generic;
- 不要 using UnityEngine.Rendering（.NET 3.5 可能有兼容性问题）
- shadowCastingMode 用数值替代枚举：(ShadowCastingMode)0
- SetInt("_ZTest", 0) 可能在某些 shader 上不可用，改用 material.SetOverrideTag
```

### Agent Prompt A2: 修改设置和 GUI 添加舒适选项

```
修改 2 个文件。项目：E:\KK_VR\KKCharaStudioVRPlugin
Unity 5.x，.NET 3.5，LangVersion 11.0，SDK 风格 csproj 自动包含 .cs

### 文件 1：E:\KK_VR\KKCharaStudioVRPlugin\KKCharaStudioVR\KKCharaStudioVRSettings.cs

在 ProximityGrabRadius 属性后面、Load 方法前面添加：

```csharp
	private bool _ComfortVignetteEnabled = true;
	private float _ComfortVignetteRadius = 0.5f;

	[XmlComment("Enable comfort vignette during movement")]
	public bool ComfortVignetteEnabled
	{
		get { return _ComfortVignetteEnabled; }
		set { _ComfortVignetteEnabled = value; TriggerPropertyChanged("ComfortVignetteEnabled"); }
	}

	[XmlComment("Vignette clear radius (0.3 = strong, 0.8 = subtle)")]
	public float ComfortVignetteRadius
	{
		get { return _ComfortVignetteRadius; }
		set { _ComfortVignetteRadius = value; TriggerPropertyChanged("ComfortVignetteRadius"); }
	}
```

### 文件 2：E:\KK_VR\KKCharaStudioVRPlugin\KKCharaStudioVR\KKCharaStudioVRGUI.cs

在 FuncWindowGUI 中的 Proximity Grab 部分之后、`GUILayout.Space(10)` 之前添加新的节：

```csharp
				GUILayout.Space(5);
				GUILayout.Label("--- 舒适设置 ---", headerStyle);
				settings.ComfortVignetteEnabled = GUILayout.Toggle(settings.ComfortVignetteEnabled, "Movement Vignette");
				if (settings.ComfortVignetteEnabled)
				{
					GUILayout.Label($"Vignette Radius: {settings.ComfortVignetteRadius:F2}");
					settings.ComfortVignetteRadius = GUILayout.HorizontalSlider(settings.ComfortVignetteRadius, 0.3f, 0.8f);
				}
```

在 Reset to Default 处理中添加：
```csharp
					settings.ComfortVignetteEnabled = true;
					settings.ComfortVignetteRadius = 0.5f;
```
```

### Agent Prompt A3: 集成舒适系统 + 高亮脉冲

```
修改 2 个文件。项目：E:\KK_VR\KKCharaStudioVRPlugin
Unity 5.x，.NET 3.5，LangVersion 11.0。

### 文件 1：E:\KK_VR\KKCharaStudioVRPlugin\KKCharaStudioVR\VRLoader.cs

在 VRQuickActions 添加之后（`val.AddComponent<VRQuickActions>();` 行后），添加：
```csharp
				val.AddComponent<VRComfortVignette>();
```

### 文件 2：E:\KK_VR\KKCharaStudioVRPlugin\KKCharaStudioVR\GripMoveKKCharaStudioTool.cs

做两处修改：

**修改 1：在 HandleThumbstickLocomotion 中通知 vignette 系统**

在 HandleThumbstickLocomotion 方法中，找到 `if (Mathf.Abs(axis.y) > 0.1f || Mathf.Abs(axis.x) > 0.1f)` 这个判断。

在这个 if 块的末尾（最后的闭合花括号之后），添加 else 分支和 vignette 通知：

将整个 if 块改为：
```csharp
		if (Mathf.Abs(axis.y) > 0.1f || Mathf.Abs(axis.x) > 0.1f)
		{
			// ... 原有移动/转向代码不变 ...

			// 通知 vignette 系统正在移动
			if (VRComfortVignette.Instance != null)
				VRComfortVignette.Instance.SetMoving(true);
		}
		else
		{
			if (VRComfortVignette.Instance != null)
				VRComfortVignette.Instance.SetMoving(false);
		}
```

注意：只在 else 里面调用 SetMoving(false)。SetMoving(true) 要放在原有
if 块的最后、花括号之前。这样只有摇杆有输入时触发移动状态。

**修改 2：近距离高亮脉冲动画**

在 ShowProximityHighlight 方法中，将静态缩放改为脉冲动画。

找到 `_proximityHighlight.transform.localScale = Vector3.one * 0.035f;`
替换为：
```csharp
		// 脉冲动画由 UpdateProximityDetection 中的位置更新代码处理
		_proximityHighlight.transform.localScale = Vector3.one * 0.035f;
```

然后在 UpdateProximityDetection 方法中，找到所有更新 highlight 位置的地方
（共 3 处设置 `_proximityHighlight.transform.position` 的地方），
在每处位置更新之后追加脉冲缩放：

```csharp
		// 脉冲动画
		float pulse = 0.035f * (1f + 0.2f * Mathf.Sin(Time.time * 5f));
		_proximityHighlight.transform.localScale = Vector3.one * pulse;
```

具体说就是在以下三个位置，每次设置了 position 之后追加这两行：
1. 第一个 if 块（throttle 跳过帧时的位置更新）
2. 第二个（新目标被设置后的 ShowProximityHighlight 之后不需要，因为 Show 已经设置了初始 scale）
3. 第三个（else if 块中的已有目标位置更新）

实际上最简单的做法：在 UpdateProximityDetection 方法的最后（return 之前），
统一处理脉冲动画：

在 UpdateProximityDetection 方法的最末尾（闭合花括号之前）添加：

```csharp
		// 统一处理高亮脉冲动画
		if (_proximityHighlight != null && _proximityHighlight.activeSelf)
		{
			float pulse = 0.035f * (1f + 0.2f * Mathf.Sin(Time.time * 5f));
			_proximityHighlight.transform.localScale = Vector3.one * pulse;
		}
```
```

---

## Batch B — 触摸选中角色 + 抓取连线 + 手部颜色反馈（修改 3 个文件）

### Agent Prompt B1: 触摸角色自动选中 + 抓取距离线

```
修改 1 个文件：E:\KK_VR\KKCharaStudioVRPlugin\KKCharaStudioVR\GripMoveKKCharaStudioTool.cs

背景：Koikatu Chara Studio VR 插件。Unity 5.x，.NET 3.5，LangVersion 11.0。
SDK 风格 csproj 自动包含 .cs，不要修改 csproj。

关键 API：
- Tool 基类：Owner = VRGIN.Controls.Controller，Controller(属性) = SteamVR_Controller.Device
- Singleton<GuideObjectManager>.Instance.selectObject 可设置选中的 GuideObject
- guideObject.guideSelect.treeNodeObject.OnClickSelect() 选中对象
- Manager.Singleton<Character>.Instance.dictEntryChara 获取所有角色
- OCIChar 是角色的 ObjectCtrlInfo 子类

任务 1：当用 trigger 点击 proximity grab 的高亮目标时，自动在工作区树中选中该角色。

在 HandleButtonEvents 方法中，找到现有的 trigger 点击处理逻辑。
在 `bool flag = false;` 之后、现有的 trigger+lastGrabbedObject 检查之前，
添加 proximity target 的选中逻辑：

```csharp
		// Trigger 点击 proximity 目标时自动选中
		if (controller.GetPressDown(EVRButtonId.k_EButton_Axis1) && _proximityTarget != null 
		    && (Object)(object)_proximityTarget.guideObject != (Object)null)
		{
			GuideObject pg = _proximityTarget.guideObject;
			if ((Object)(object)pg.guideSelect != (Object)null 
			    && (Object)(object)pg.guideSelect.treeNodeObject != (Object)null)
			{
				pg.guideSelect.treeNodeObject.OnClickSelect();
			}
			else
			{
				Singleton<GuideObjectManager>.Instance.selectObject = pg;
			}
			flag = true;
		}
```

任务 2：当抓取 IK 目标时，显示从手到目标的连线（像 VAM 的抓取线）。

添加新字段（在 _proximityCheckCounter 之后）：
```csharp
	private LineRenderer _grabLine;
```

添加新方法（在 ClearProximityHighlight 之后）：
```csharp
	private void UpdateGrabLine()
	{
		if (grabbingObject == null)
		{
			if (_grabLine != null)
				((Component)_grabLine).gameObject.SetActive(false);
			return;
		}

		// 只对有 guideObject 的 MoveableGUIObject 显示连线
		MoveableGUIObject mgo = grabbingObject.GetComponent<MoveableGUIObject>();
		if (mgo == null || (Object)(object)mgo.guideObject == (Object)null)
		{
			if (_grabLine != null)
				((Component)_grabLine).gameObject.SetActive(false);
			return;
		}

		if (_grabLine == null)
		{
			GameObject lineObj = new GameObject("_VRGrabLine");
			Object.DontDestroyOnLoad((Object)(object)lineObj);
			_grabLine = lineObj.AddComponent<LineRenderer>();
			_grabLine.material = new Material(Shader.Find("Particles/Alpha Blended"));
			_grabLine.material.renderQueue = 3600;
			_grabLine.SetVertexCount(2);
			_grabLine.useWorldSpace = true;
			_grabLine.SetWidth(0.002f, 0.001f);
		}

		((Component)_grabLine).gameObject.SetActive(true);

		Vector3 handPos = ((Component)this).transform.position;
		Vector3 targetPos = mgo.guideObject.transformTarget.position;
		float dist = Vector3.Distance(handPos, targetPos);

		// 颜色根据距离变化：近=绿色半透明，远=黄色
		Color lineColor;
		if (dist < 0.1f)
			lineColor = new Color(0f, 1f, 0.5f, 0.3f);
		else if (dist < 0.3f)
			lineColor = new Color(1f, 1f, 0f, 0.5f);
		else
			lineColor = new Color(1f, 0.5f, 0f, 0.7f);

		_grabLine.SetColors(lineColor, lineColor);
		_grabLine.SetPosition(0, handPos);
		_grabLine.SetPosition(1, targetPos);
	}
```

在 OnUpdate 中，在 HandleObjectGrab() 之后添加：
```csharp
		UpdateGrabLine();
```

在 OnDisable 中添加：
```csharp
		if (_grabLine != null) ((Component)_grabLine).gameObject.SetActive(false);
```

在 OnDestroy 中添加：
```csharp
		if ((Object)(object)_grabLine != (Object)null)
			Object.Destroy((Object)(object)((Component)_grabLine).gameObject);
```

注意 LineRenderer 的 SetVertexCount 是 Unity 5.x 的 API（不是 positionCount）。
SetWidth 而不是 startWidth/endWidth。SetColors 而不是 startColor/endColor。
```

### Agent Prompt B2: 手部模型触摸变色

```
修改 1 个文件：E:\KK_VR\KKCharaStudioVRPlugin\KKCharaStudioVR\VRHandModelManager.cs

背景：Unity 5.x，.NET 3.5，LangVersion 11.0。
SDK 风格 csproj 自动包含 .cs。

任务：当手部触碰角色时（DynamicBone 碰撞），手部模型颜色从默认的
浅灰色渐变为暖色（淡粉红），给予视觉触摸反馈。

实现方式：VRHandHapticTrigger 已经在 OnTriggerStay 中检测角色触碰。
让它通知 VRHandModelManager 有触碰发生。

在 VRHandModelManager 中添加以下内容：

1. 在 HandContext 类中添加字段：
```csharp
            public float touchFeedback; // 0 = not touching, 1 = touching
```

2. 添加公共方法供 VRHandHapticTrigger 调用：
```csharp
        public void NotifyTouch(bool isLeft)
        {
            HandContext h = isLeft ? leftHand : rightHand;
            if (h != null) h.touchFeedback = 1f;
        }
```

3. 在 UpdateSingleHand 中（UpdateHandAnimation 调用之前），添加颜色混合：
```csharp
            // 触摸反馈：接触时变暖色
            h.touchFeedback = Mathf.Lerp(h.touchFeedback, 0f, Time.deltaTime * 4f);
            if (h.material != null)
            {
                float t = h.touchFeedback;
                // 默认色 (0.8, 0.8, 0.9) → 触摸色 (1.0, 0.7, 0.75)
                float r = Mathf.Lerp(0.8f, 1.0f, t);
                float g = Mathf.Lerp(0.8f, 0.7f, t);
                float b = Mathf.Lerp(0.9f, 0.75f, t);
                Color c = new Color(r, g, b, alpha);
                if (h.material.color != c)
                    h.material.color = c;
            }
```

注意：这个颜色混合代码应该替换原来的 alpha-only 更新逻辑：
```csharp
            if (h.material != null && !Mathf.Approximately(h.material.color.a, alpha))
            {
                Color c = h.material.color;
                c.a = alpha;
                h.material.color = c;
            }
```
替换为上面的颜色混合代码（它同时处理了 alpha 和 RGB）。

---

然后修改 VRHandHapticTrigger.cs 添加通知：

在 OnTriggerStay 中，在 `_lastPulseTime = Time.time;` 之后添加：

```csharp
                    // 通知手部模型变色
                    if (VRHandModelManager.Instance != null)
                    {
                        bool triggerIsLeft = false;
                        var leftCtrl = _trackedObject.GetComponentInParent<VRGIN.Controls.LeftController>();
                        triggerIsLeft = (leftCtrl != null);
                        VRHandModelManager.Instance.NotifyTouch(triggerIsLeft);
                    }
```

需要添加 using：
```csharp
using VRGIN.Controls;
```
（VRHandHapticTrigger.cs 文件顶部）
```

---

## Batch C — 双手缩放场景 + IK 平滑插值（1 新文件 + 2 修改）

### Agent Prompt C1: VRTwoHandScale.cs（新文件）

```
创建新文件：E:\KK_VR\KKCharaStudioVRPlugin\KKCharaStudioVR\VRTwoHandScale.cs

背景：Koikatu Chara Studio VR 插件。Unity 5.x，.NET 3.5，LangVersion 11.0。
SDK 风格 csproj 自动包含 .cs。

功能：双手同时按 Grip 时，根据双手距离变化缩放整个 VR 世界（改变用户的
相对大小）。双手拉开 = 世界变小（用户变大），双手合拢 = 世界变大（用户变小）。

关键 API：
- VR.Mode.Left / VR.Mode.Right — 左右控制器（VRGIN.Controls.IController）
- VR.Camera.SteamCam.origin — VR 原点 Transform
- SteamVR_Controller.Input(index).GetPress(EVRButtonId.k_EButton_Grip) — 检测 grip

设计：
- 在 VRLoader 中通过 AddComponent 初始化
- 双手同时 grip 时记录初始距离
- 后续帧计算距离比率，缩放 VR 原点
- 缩放范围限制：0.1x ~ 10x
- 通过修改 origin.localScale 实现缩放

命名空间：KKCharaStudioVR

using：
- UnityEngine
- VRGIN.Core
- VRGIN.Modes
- Valve.VR

```csharp
public class VRTwoHandScale : MonoBehaviour
{
    public static VRTwoHandScale Instance { get; private set; }

    private float _initialDistance;
    private Vector3 _initialScale;
    private Vector3 _initialMidpoint;
    private bool _isScaling;
    private KKCharaStudioVRSettings _settings;

    void Start()
    {
        Instance = this;
        if (VR.Manager != null && VR.Manager.Context != null)
            _settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
    }

    void Update()
    {
        if (_settings != null && !_settings.TwoHandScaleEnabled) return;
        if (VR.Mode == null || VR.Mode.Left == null || VR.Mode.Right == null) return;

        var leftTracked = ((Component)VR.Mode.Left).GetComponent<SteamVR_TrackedObject>();
        var rightTracked = ((Component)VR.Mode.Right).GetComponent<SteamVR_TrackedObject>();
        if (leftTracked == null || rightTracked == null) return;
        if (!leftTracked.isValid || !rightTracked.isValid) return;

        var leftDevice = SteamVR_Controller.Input((int)leftTracked.index);
        var rightDevice = SteamVR_Controller.Input((int)rightTracked.index);
        if (leftDevice == null || rightDevice == null) return;

        bool bothGrip = leftDevice.GetPress(EVRButtonId.k_EButton_Grip)
                     && rightDevice.GetPress(EVRButtonId.k_EButton_Grip);

        Transform origin = VR.Camera.SteamCam.origin;
        if (origin == null) return;

        if (bothGrip)
        {
            Vector3 leftPos = ((Component)VR.Mode.Left).transform.position;
            Vector3 rightPos = ((Component)VR.Mode.Right).transform.position;
            float currentDist = Vector3.Distance(leftPos, rightPos);

            if (!_isScaling)
            {
                // 开始缩放
                _isScaling = true;
                _initialDistance = currentDist;
                _initialScale = origin.localScale;
                _initialMidpoint = (leftPos + rightPos) * 0.5f;

                // 发送触觉反馈
                leftDevice.TriggerHapticPulse(500, EVRButtonId.k_EButton_Axis0);
                rightDevice.TriggerHapticPulse(500, EVRButtonId.k_EButton_Axis0);
            }
            else if (_initialDistance > 0.01f)
            {
                // 计算缩放比率（双手拉开 = ratio > 1 = 世界变小 = 用户变大）
                // 反转：双手拉开应该让用户变大（世界缩小）
                float ratio = _initialDistance / currentDist;
                Vector3 newScale = _initialScale * ratio;

                // 限制缩放范围
                float magnitude = newScale.x;
                magnitude = Mathf.Clamp(magnitude, 0.1f, 10f);
                newScale = Vector3.one * magnitude;

                // 以双手中点为缩放中心
                Vector3 currentMidpoint = (leftPos + rightPos) * 0.5f;
                Vector3 pivotWorld = _initialMidpoint;

                // 应用缩放（以 pivot 为中心）
                Vector3 originToPivot = pivotWorld - origin.position;
                float scaleChange = magnitude / origin.localScale.x;

                origin.localScale = newScale;
                // 调整位置保持 pivot 不动
                origin.position = pivotWorld - originToPivot * scaleChange;
            }
        }
        else
        {
            if (_isScaling)
            {
                _isScaling = false;
            }
        }
    }
}
```

注意：
- 缩放以双手中点为中心，这样缩放时用户关注的区域不会移动
- 双手同时 grip 才触发缩放，单手 grip 仍然是世界移动或物体抓取
- 需要处理与 GripMoveKKCharaStudioTool 的 HandleGripWorldMove 的冲突：
  当双手都在 grip 时，两个手的 GripMoveKKCharaStudioTool 都会尝试做世界移动。
  VRTwoHandScale 的缩放会叠加在世界移动之上，可能导致抖动。

解决冲突的方法：在 GripMoveKKCharaStudioTool.HandleGripWorldMove 中，
检测是否正在双手缩放，如果是则跳过世界移动。
```

### Agent Prompt C2: 缩放冲突解决 + VRLoader 注册

```
修改 2 个文件。项目：E:\KK_VR\KKCharaStudioVRPlugin
Unity 5.x，.NET 3.5，LangVersion 11.0。

### 文件 1：E:\KK_VR\KKCharaStudioVRPlugin\KKCharaStudioVR\GripMoveKKCharaStudioTool.cs

在 HandleGripWorldMove 方法开头添加双手缩放检测：

找到：
```csharp
	private void HandleGripWorldMove()
	{
		if (controller.GetPress(EVRButtonId.k_EButton_Grip) && (Object)(object)grabbingObject == (Object)null)
```

替换为：
```csharp
	private void HandleGripWorldMove()
	{
		// 双手缩放时跳过世界移动，避免冲突
		if (VRTwoHandScale.Instance != null && VRTwoHandScale.Instance.IsScaling)
			return;

		if (controller.GetPress(EVRButtonId.k_EButton_Grip) && (Object)(object)grabbingObject == (Object)null)
```

注意：VRTwoHandScale 需要暴露 IsScaling 属性。

### 文件 2：E:\KK_VR\KKCharaStudioVRPlugin\KKCharaStudioVR\VRLoader.cs

在 `val.AddComponent<VRComfortVignette>();` 之后添加：
```csharp
				val.AddComponent<VRTwoHandScale>();
```

### 文件 3（回到 VRTwoHandScale.cs）

在 VRTwoHandScale 类中添加公共属性：

```csharp
    public bool IsScaling => _isScaling;
```

### 文件 4：KKCharaStudioVRSettings.cs

在 ComfortVignetteRadius 之后添加：

```csharp
	private bool _TwoHandScaleEnabled = true;

	[XmlComment("Enable two-hand world scaling")]
	public bool TwoHandScaleEnabled
	{
		get { return _TwoHandScaleEnabled; }
		set { _TwoHandScaleEnabled = value; TriggerPropertyChanged("TwoHandScaleEnabled"); }
	}
```

### 文件 5：KKCharaStudioVRGUI.cs

在舒适设置部分之后，Save/Reset 之前添加：

```csharp
				GUILayout.Space(5);
				GUILayout.Label("--- 高级设置 ---", headerStyle);
				settings.TwoHandScaleEnabled = GUILayout.Toggle(settings.TwoHandScaleEnabled, "Two-Hand World Scale");
```

在 Reset to Default 中添加：
```csharp
					settings.TwoHandScaleEnabled = true;
```
```

### Agent Prompt C3: IK 平滑插值（让操控更物理）

```
修改 1 个文件：E:\KK_VR\KKCharaStudioVRPlugin\KKCharaStudioVR\GripMoveKKCharaStudioTool.cs

背景：Unity 5.x，.NET 3.5，LangVersion 11.0。
Tool 基类：Owner = VRGIN.Controls.Controller，Controller 属性 = SteamVR_Controller.Device

当前问题：HandleObjectGrab 中抓取 IK 目标时，grabbingObject.transform 
直接跳到 grabHandle.transform 的位置（瞬移）。这感觉不自然。
VAM 的操控有轻微的弹簧阻尼延迟，让 IK 跟随手的动作时有物理感。

实现：添加可选的平滑跟随模式。

添加新字段（在 _grabLine 之后）：
```csharp
	private Vector3 _smoothGrabPos;
	private Quaternion _smoothGrabRot;
	private bool _smoothGrabInitialized;
```

修改 HandleObjectGrab 中的 "连续跟随" 逻辑。

找到：
```csharp
		if (press && (Object)(object)grabbingObject != (Object)null)
		{
			grabbingObject.transform.position = grabHandle.transform.position;
			grabbingObject.transform.rotation = grabHandle.transform.rotation;
			if ((Object)(object)grabbingObject.GetComponent<MoveableGUIObject>() != (Object)null)
			{
				grabbingObject.GetComponent<MoveableGUIObject>().OnMoved();
			}
		}
```

替换为：
```csharp
		if (press && (Object)(object)grabbingObject != (Object)null)
		{
			Vector3 targetPos = grabHandle.transform.position;
			Quaternion targetRot = grabHandle.transform.rotation;

			// IK 平滑插值：让操控有物理弹簧感
			MoveableGUIObject mgo = grabbingObject.GetComponent<MoveableGUIObject>();
			bool isIKTarget = mgo != null && (Object)(object)mgo.guideObject != (Object)null;

			if (isIKTarget && _settings != null)
			{
				if (!_smoothGrabInitialized)
				{
					_smoothGrabPos = targetPos;
					_smoothGrabRot = targetRot;
					_smoothGrabInitialized = true;
				}

				// 弹簧阻尼跟随（smoothness 越高越平滑）
				float smoothness = 15f; // 帧率无关的平滑速度
				_smoothGrabPos = Vector3.Lerp(_smoothGrabPos, targetPos, Time.deltaTime * smoothness);
				_smoothGrabRot = Quaternion.Slerp(_smoothGrabRot, targetRot, Time.deltaTime * smoothness);

				grabbingObject.transform.position = _smoothGrabPos;
				grabbingObject.transform.rotation = _smoothGrabRot;
			}
			else
			{
				// 非 IK 目标（GUI 面板等）直接跟随
				grabbingObject.transform.position = targetPos;
				grabbingObject.transform.rotation = targetRot;
			}

			if (mgo != null)
			{
				mgo.OnMoved();
			}
		}
```

在抓取开始时（pressDown 块）和释放时（pressUp 块）重置平滑状态。

在 pressDown 块的末尾（触觉反馈之后）添加：
```csharp
			_smoothGrabInitialized = false;
```

在 pressUp 块中（grabbingObject = null 之前）添加：
```csharp
			_smoothGrabInitialized = false;
```

这样每次新抓取都从目标的当前位置开始插值，避免跳变。
smoothness = 15 表示约 0.07 秒达到目标的 63%，感觉是轻微弹簧而不是迟钝。
```

---

## 执行顺序建议

这 3 批共 7 个提示词可以这样安排：

**并行组 1**（互不冲突）：
- A1 (VRComfortVignette 新文件)
- B2 (VRHandModelManager + VRHandHapticTrigger 修改)
- C1 (VRTwoHandScale 新文件)

**并行组 2**（依赖组 1 完成）：
- A2 (Settings + GUI 添加舒适选项)
- A3 (VRLoader + GripMoveKKCharaStudioTool 集成 vignette + 高亮脉冲)
- B1 (GripMoveKKCharaStudioTool 添加触摸选中 + 抓取线)
- C2 (缩放冲突解决 + VRLoader + Settings + GUI)

**最后**（依赖全部完成）：
- C3 (IK 平滑插值 - 修改 GripMoveKKCharaStudioTool)

## 注意：多个 Prompt 修改同一文件的冲突风险

以下文件被多个 Prompt 修改：
- GripMoveKKCharaStudioTool.cs: A3, B1, C2, C3（4 个！）
- KKCharaStudioVRSettings.cs: A2, C2
- KKCharaStudioVRGUI.cs: A2, C2  
- VRLoader.cs: A3, C2

**建议：A3 + B1 + C2 + C3 必须串行执行**，或者合并成一个大提示词。

合并策略：
1. 先执行并行组 1（3 个新文件，互不冲突）
2. 然后把 A2 + C2 合并成一个（修改 Settings + GUI + VRLoader）
3. 然后把 A3 + B1 + C3 合并成一个（修改 GripMoveKKCharaStudioTool）
4. 最后执行 B2（修改 VRHandModelManager + VRHandHapticTrigger）
