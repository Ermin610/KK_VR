# VAM-Level VR — Consolidated Agent Prompts (v2)

上一版 (05) 存在以下问题，已全部修复：
1. GripMoveKKCharaStudioTool.cs 被 4 个 prompt 修改 → 合并为 1 个
2. VRComfortVignette 有两段互相矛盾的 shader 方案 → 统一为顶点色方案
3. VRTwoHandScale 缩放方向反了 + 世界空间距离会随缩放变化导致抖动 → 已修正
4. 抓取线用 Particles/Alpha Blended shader 没有贴图会不可见 → 改用项目自带 ColorZOrder
5. 使用了 `?.` 语法但代码库风格不用 → 改为显式 null 检查

## 执行策略

共 5 个 Prompt，每个文件只被 1 个 Prompt 修改，零冲突：

**并行执行（互不依赖）：**
- Prompt 1: VRComfortVignette.cs（新文件）
- Prompt 2: VRTwoHandScale.cs（新文件）

**串行执行（按顺序，依赖并行组完成）：**
- Prompt 3: KKCharaStudioVRSettings.cs + KKCharaStudioVRGUI.cs + VRLoader.cs
- Prompt 4: GripMoveKKCharaStudioTool.cs（所有行为修改合并）
- Prompt 5: VRHandModelManager.cs + VRHandHapticTrigger.cs

---

## Prompt 1: VRComfortVignette.cs（新文件）

```
创建新文件：E:\KK_VR\KKCharaStudioVRPlugin\KKCharaStudioVR\VRComfortVignette.cs

背景：Koikatu Chara Studio VR 插件，Unity 5.x，.NET 3.5，LangVersion 11.0。
SDK 风格 csproj 自动包含所有 .cs 文件，不需要修改 csproj。

这是一个 VR 舒适暗角（vignette）系统：用户进行摇杆移动/转向时，屏幕
边缘渐变出现黑色遮罩以减少周边视野，防止 VR 晕动症。

技术方案：
- 使用程序化网格创建径向渐变遮罩（中心透明，边缘不透明）
- 通过顶点颜色实现渐变（不依赖 shader 属性）
- 网格跟随 VR 头部相机
- 使用 Sprites/Default shader（已在项目 SteamVR_PlayArea 中验证可用）
- 强度由外部调用 SetMoving(true/false) 控制，自动平滑淡入淡出

请创建以下完整文件内容：

```csharp
using System.Collections.Generic;
using UnityEngine;
using VRGIN.Core;

namespace KKCharaStudioVR
{
    public class VRComfortVignette : MonoBehaviour
    {
        public static VRComfortVignette Instance { get; private set; }

        private GameObject _vignetteObj;
        private MeshFilter _meshFilter;
        private MeshRenderer _renderer;
        private Material _material;
        private float _currentIntensity;
        private float _targetIntensity;

        private const int Segments = 32;
        private const int Rings = 8;
        private const float MeshRadius = 0.6f;
        private const float MeshDistance = 0.2f;

        /// <summary>
        /// 由外部调用通知是否正在移动。true 时暗角渐入，false 时渐出。
        /// </summary>
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
            KKCharaStudioVRSettings settings = null;
            if (VR.Manager != null && VR.Manager.Context != null)
                settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;

            if (settings == null || !settings.ComfortVignetteEnabled)
            {
                if (_vignetteObj != null) _vignetteObj.SetActive(false);
                _currentIntensity = 0f;
                return;
            }

            // 平滑淡入淡出
            _currentIntensity = Mathf.Lerp(_currentIntensity, _targetIntensity, Time.deltaTime * 8f);

            if (_currentIntensity < 0.01f)
            {
                if (_vignetteObj != null) _vignetteObj.SetActive(false);
                _currentIntensity = 0f;
                return;
            }

            if (_vignetteObj != null)
            {
                _vignetteObj.SetActive(true);

                // 跟随 VR 头部相机
                Transform head = VR.Camera.Head;
                if (head != null)
                {
                    _vignetteObj.transform.position = head.position;
                    _vignetteObj.transform.rotation = head.rotation;
                }

                // 动态更新顶点颜色控制遮罩范围和强度
                UpdateVignetteColors(settings.ComfortVignetteRadius, _currentIntensity);
            }
        }

        private void CreateVignetteMesh()
        {
            _vignetteObj = new GameObject("VRComfortVignette");
            Object.DontDestroyOnLoad(_vignetteObj);

            _meshFilter = _vignetteObj.AddComponent<MeshFilter>();
            _renderer = _vignetteObj.AddComponent<MeshRenderer>();

            // 构建径向渐变网格：中心透明，边缘不透明
            int vertCount = 1 + Segments * Rings;
            Vector3[] verts = new Vector3[vertCount];
            Color[] colors = new Color[vertCount];

            // 中心顶点（完全透明）
            verts[0] = new Vector3(0, 0, MeshDistance);
            colors[0] = new Color(0, 0, 0, 0);

            for (int r = 1; r <= Rings; r++)
            {
                float ringRadius = (float)r / Rings * MeshRadius;
                float ringAlpha = Mathf.Clamp01(((float)r / Rings - 0.4f) / 0.6f);
                ringAlpha = ringAlpha * ringAlpha; // 二次曲线，更自然的渐变

                for (int s = 0; s < Segments; s++)
                {
                    float angle = (float)s / Segments * Mathf.PI * 2f;
                    int idx = 1 + (r - 1) * Segments + s;
                    verts[idx] = new Vector3(
                        Mathf.Cos(angle) * ringRadius,
                        Mathf.Sin(angle) * ringRadius,
                        MeshDistance);
                    colors[idx] = new Color(0, 0, 0, ringAlpha);
                }
            }

            // 三角形索引
            List<int> triList = new List<int>();

            // 中心到第一环
            for (int s = 0; s < Segments; s++)
            {
                int next = (s + 1) % Segments;
                triList.Add(0);
                triList.Add(1 + s);
                triList.Add(1 + next);
            }

            // 环与环之间
            for (int r = 0; r < Rings - 1; r++)
            {
                int ringStart = 1 + r * Segments;
                int nextRingStart = 1 + (r + 1) * Segments;
                for (int s = 0; s < Segments; s++)
                {
                    int next = (s + 1) % Segments;
                    triList.Add(ringStart + s);
                    triList.Add(nextRingStart + s);
                    triList.Add(nextRingStart + next);

                    triList.Add(ringStart + s);
                    triList.Add(nextRingStart + next);
                    triList.Add(ringStart + next);
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = verts;
            mesh.triangles = triList.ToArray();
            mesh.colors = colors;
            mesh.RecalculateBounds();
            _meshFilter.mesh = mesh;

            // Sprites/Default：支持顶点颜色，无光照，Alpha 混合
            _material = new Material(Shader.Find("Sprites/Default"));
            _material.renderQueue = 5000;
            _renderer.material = _material;
            _renderer.shadowCastingMode = (UnityEngine.Rendering.ShadowCastingMode)0;
            _renderer.receiveShadows = false;

            _vignetteObj.SetActive(false);
        }

        /// <summary>
        /// 通过修改顶点颜色动态控制遮罩范围和强度。
        /// radius 控制透明区域大小（0.3=只有中心30%透明，0.8=80%透明）
        /// intensity 控制边缘黑度（0=无遮罩，1=全黑边缘）
        /// </summary>
        private void UpdateVignetteColors(float radius, float intensity)
        {
            if (_meshFilter == null || _meshFilter.mesh == null) return;
            Mesh mesh = _meshFilter.mesh;
            Vector3[] vertices = mesh.vertices;
            Color[] newColors = new Color[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];
                float r = Mathf.Sqrt(v.x * v.x + v.y * v.y);
                float normalizedR = r / MeshRadius;

                float alpha;
                if (normalizedR <= radius)
                {
                    alpha = 0f;
                }
                else
                {
                    alpha = Mathf.Clamp01((normalizedR - radius) / (1f - radius));
                    alpha = alpha * alpha * intensity;
                }
                newColors[i] = new Color(0, 0, 0, alpha);
            }
            mesh.colors = newColors;
        }

        void OnDestroy()
        {
            if (_vignetteObj != null) Destroy(_vignetteObj);
        }
    }
}
```

注意事项：
- 不要使用 `?.` null 条件运算符，使用显式 null 检查
- mesh.vertices 和 mesh.colors 使用数组赋值（不是 SetVertices/SetColors 方法）
- ShadowCastingMode 使用 (UnityEngine.Rendering.ShadowCastingMode)0 而非枚举名
- List<int> 需要 using System.Collections.Generic
```

---

## Prompt 2: VRTwoHandScale.cs（新文件）

```
创建新文件：E:\KK_VR\KKCharaStudioVRPlugin\KKCharaStudioVR\VRTwoHandScale.cs

背景：Koikatu Chara Studio VR 插件，Unity 5.x，.NET 3.5，LangVersion 11.0。
SDK 风格 csproj 自动包含所有 .cs 文件，不需要修改 csproj。
使用 SteamVR 旧 API（不是 SteamVR 2.x Input System）。

功能：双手同时按 Grip 时，根据双手距离变化缩放 VR 世界。
双手拉开 = 用户变大（世界相对变小），双手合拢 = 用户变小（世界相对变大）。
缩放以双手中点为中心，使关注区域保持稳定。

关键技术要点：
1. 双手距离必须在 origin 的局部空间中测量（除以 origin.localScale.x），
   否则缩放会改变世界空间距离，导致反馈振荡！
2. 缩放中心在双手中点，通过调整 origin.position 保持中点世界坐标不变
3. 公开 IsScaling 属性供 GripMoveKKCharaStudioTool 检查，避免世界移动冲突

请创建以下完整文件内容：

```csharp
using UnityEngine;
using VRGIN.Core;
using Valve.VR;

namespace KKCharaStudioVR
{
    public class VRTwoHandScale : MonoBehaviour
    {
        public static VRTwoHandScale Instance { get; private set; }

        /// <summary>
        /// 是否正在双手缩放。GripMoveKKCharaStudioTool 检查此属性以跳过世界移动。
        /// </summary>
        public bool IsScaling
        {
            get { return _isScaling; }
        }

        private float _initialDistance;
        private Vector3 _initialScale;
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
            if (leftTracked.index == SteamVR_TrackedObject.EIndex.None ||
                rightTracked.index == SteamVR_TrackedObject.EIndex.None) return;

            var leftDevice = SteamVR_Controller.Input((int)leftTracked.index);
            var rightDevice = SteamVR_Controller.Input((int)rightTracked.index);
            if (leftDevice == null || rightDevice == null) return;

            bool bothGrip = leftDevice.GetPress(EVRButtonId.k_EButton_Grip)
                         && rightDevice.GetPress(EVRButtonId.k_EButton_Grip);

            Transform origin = VR.Camera.SteamCam.origin;
            if (origin == null) return;

            Vector3 leftPos = ((Component)VR.Mode.Left).transform.position;
            Vector3 rightPos = ((Component)VR.Mode.Right).transform.position;

            if (bothGrip)
            {
                // 关键：用 scale-invariant 距离避免振荡
                // 世界空间距离会随 origin.localScale 变化，必须除以当前 scale
                float worldDist = Vector3.Distance(leftPos, rightPos);
                float unscaledDist = worldDist / origin.localScale.x;

                if (!_isScaling)
                {
                    _isScaling = true;
                    _initialDistance = unscaledDist;
                    _initialScale = origin.localScale;

                    // 触觉反馈通知用户进入缩放模式
                    leftDevice.TriggerHapticPulse(500, EVRButtonId.k_EButton_Axis0);
                    rightDevice.TriggerHapticPulse(500, EVRButtonId.k_EButton_Axis0);
                }
                else if (_initialDistance > 0.01f)
                {
                    // 双手拉开 → ratio > 1 → scale 增大 → 用户变大（世界变小）
                    float ratio = unscaledDist / _initialDistance;
                    float newMagnitude = Mathf.Clamp(_initialScale.x * ratio, 0.1f, 10f);

                    // 以双手中点为缩放中心，保持中点世界坐标不变
                    Vector3 midpoint = (leftPos + rightPos) * 0.5f;
                    Vector3 originToMid = midpoint - origin.position;
                    float scaleChange = newMagnitude / origin.localScale.x;

                    origin.localScale = Vector3.one * newMagnitude;
                    origin.position = midpoint - originToMid * scaleChange;
                }
            }
            else
            {
                _isScaling = false;
            }
        }
    }
}
```

注意事项：
- 不要 using VRGIN.Modes（不需要）
- IsScaling 使用完整属性写法 `get { return _isScaling; }`，不用表达式体
- SteamVR_TrackedObject.EIndex.None 检查代替 isValid（与 VRHandModelManager 风格一致）
- TriggerHapticPulse 的第二个参数固定为 EVRButtonId.k_EButton_Axis0
```

---

## Prompt 3: 设置 + GUI + VRLoader 注册

```
修改 3 个文件。项目：E:\KK_VR\KKCharaStudioVRPlugin
Unity 5.x，.NET 3.5，LangVersion 11.0，SDK 风格 csproj 自动包含 .cs。

### 文件 1：E:\KK_VR\KKCharaStudioVRPlugin\KKCharaStudioVR\KKCharaStudioVRSettings.cs

在 ProximityGrabRadius 属性之后、`public static KKCharaStudioVRSettings Load` 
方法之前，添加以下内容：

```csharp
	private bool _ComfortVignetteEnabled = true;
	private float _ComfortVignetteRadius = 0.5f;
	private bool _TwoHandScaleEnabled = true;

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

	[XmlComment("Enable two-hand world scaling")]
	public bool TwoHandScaleEnabled
	{
		get { return _TwoHandScaleEnabled; }
		set { _TwoHandScaleEnabled = value; TriggerPropertyChanged("TwoHandScaleEnabled"); }
	}
```

### 文件 2：E:\KK_VR\KKCharaStudioVRPlugin\KKCharaStudioVR\KKCharaStudioVRGUI.cs

在 FuncWindowGUI 方法中，找到 Proximity Grab 设置部分之后的 `GUILayout.Space(10);`，
在其前面添加两个新设置节：

```csharp
				GUILayout.Space(5);
				GUILayout.Label("--- 舒适设置 ---", headerStyle);
				settings.ComfortVignetteEnabled = GUILayout.Toggle(settings.ComfortVignetteEnabled, "Movement Vignette");
				if (settings.ComfortVignetteEnabled)
				{
					GUILayout.Label($"Vignette Radius: {settings.ComfortVignetteRadius:F2}");
					settings.ComfortVignetteRadius = GUILayout.HorizontalSlider(settings.ComfortVignetteRadius, 0.3f, 0.8f);
				}

				GUILayout.Space(5);
				GUILayout.Label("--- 高级设置 ---", headerStyle);
				settings.TwoHandScaleEnabled = GUILayout.Toggle(settings.TwoHandScaleEnabled, "Two-Hand World Scale");
```

然后找到 "Reset to Default" 按钮的处理代码块，在 `settings.ProximityGrabRadius = 0.12f;`
之后添加：

```csharp
					settings.ComfortVignetteEnabled = true;
					settings.ComfortVignetteRadius = 0.5f;
					settings.TwoHandScaleEnabled = true;
```

### 文件 3：E:\KK_VR\KKCharaStudioVRPlugin\KKCharaStudioVR\VRLoader.cs

在 LoadDevice 方法中，找到 `val.AddComponent<VRQuickActions>();` 行，
在其后面添加两行：

```csharp
				val.AddComponent<VRComfortVignette>();
				val.AddComponent<VRTwoHandScale>();
```

注意事项：
- KKCharaStudioVRSettings.cs 中属性风格必须与现有属性一致（完整 get/set，TriggerPropertyChanged）
- VRLoader.cs 中 AddComponent 的顺序：VRQuickActions → VRComfortVignette → VRTwoHandScale
- GUI 中新设置节放在物理设置之后、Save/Reset 按钮之前
```

---

## Prompt 4: GripMoveKKCharaStudioTool.cs（所有行为修改合并）

```
修改 1 个文件：E:\KK_VR\KKCharaStudioVRPlugin\KKCharaStudioVR\GripMoveKKCharaStudioTool.cs

背景：Unity 5.x，.NET 3.5，LangVersion 11.0，SDK 风格 csproj。
这个文件是 VR 工具的核心，继承自 VRGIN 的 Tool 基类。
- Owner = VRGIN.Controls.Controller（有 SetRenderModelVisible）
- Controller 属性 = SteamVR_Controller.Device（有 GetAxis, GetPress 等）
- LineRenderer API 使用 Unity 5.x 版本：SetVertexCount, SetWidth, SetColors（不是 positionCount, startWidth 等）

此 Prompt 包含 10 处修改，按顺序执行：

### 修改 1：添加新字段

找到：
```csharp
	private int _proximityCheckCounter;
```

替换为：
```csharp
	private int _proximityCheckCounter;
	private LineRenderer _grabLine;
	private Vector3 _smoothGrabPos;
	private Quaternion _smoothGrabRot;
	private bool _smoothGrabInitialized;
```

### 修改 2：OnDestroy 添加 grabLine 清理

找到：
```csharp
	protected override void OnDestroy()
	{
		if ((Object)(object)_proximityHighlight != (Object)null)
			Object.Destroy((Object)(object)_proximityHighlight);
```

替换为：
```csharp
	protected override void OnDestroy()
	{
		if ((Object)(object)_proximityHighlight != (Object)null)
			Object.Destroy((Object)(object)_proximityHighlight);
		if (_grabLine != null)
			Object.Destroy(((Component)_grabLine).gameObject);
```

### 修改 3：OnDisable 添加 grabLine 隐藏

找到：
```csharp
		ClearProximityHighlight();
		if ((Object)(object)gripMenuHandler != (Object)null)
```

替换为：
```csharp
		ClearProximityHighlight();
		if (_grabLine != null) ((Component)_grabLine).gameObject.SetActive(false);
		if ((Object)(object)gripMenuHandler != (Object)null)
```

### 修改 4：OnUpdate 添加 UpdateGrabLine 调用

找到：
```csharp
		HandleObjectGrab();
		HandleGripWorldMove();
```

替换为：
```csharp
		HandleObjectGrab();
		UpdateGrabLine();
		HandleGripWorldMove();
```

### 修改 5：替换整个 HandleThumbstickLocomotion 方法

找到整个 HandleThumbstickLocomotion 方法（从 `private void HandleThumbstickLocomotion()` 
到其闭合花括号），替换为：

```csharp
	private void HandleThumbstickLocomotion()
	{
		Vector2 axis = controller.GetAxis(EVRButtonId.k_EButton_SteamVR_Touchpad);
		bool isLeft = _isLeftHand;

		if (Mathf.Abs(axis.y) > 0.1f || Mathf.Abs(axis.x) > 0.1f)
		{
			Transform head = VR.Camera.Head;
			Transform origin = VR.Camera.SteamCam.origin;

			if ((Object)(object)origin != (Object)null && (Object)(object)head != (Object)null)
			{
				if (isLeft)
				{
					Vector3 forward = head.forward;
					forward.y = 0f;
					((Vector3)(ref forward)).Normalize();
					Vector3 right = head.right;
					right.y = 0f;
					((Vector3)(ref right)).Normalize();
					
					float speed = _settings != null ? _settings.LocomotionSpeed : 2.0f;
					origin.position += (forward * axis.y + right * axis.x) * speed * Time.deltaTime;
				}
				else
				{
					bool smoothTurn = _settings != null && _settings.SmoothTurnEnabled;
					if (smoothTurn)
					{
						float turnSpeed = _settings != null ? _settings.SmoothTurnSpeed : 90f;
						origin.RotateAround(head.position, Vector3.up, axis.x * turnSpeed * Time.deltaTime);
					}
					else if (Mathf.Abs(axis.x) > 0.5f)
					{
						float cooldown = _settings != null ? _settings.SnapTurnCooldown : 0.3f;
						if (Time.time - lastSnapTurnTime > cooldown)
						{
							float angle = _settings != null ? _settings.SnapTurnAngle : 45f;
							origin.RotateAround(head.position, Vector3.up, Mathf.Sign(axis.x) * angle);
							lastSnapTurnTime = Time.time;
						}
					}
				}

				// 通知舒适暗角：正在移动
				if (VRComfortVignette.Instance != null)
					VRComfortVignette.Instance.SetMoving(true);
			}
		}
		else
		{
			// 通知舒适暗角：停止移动
			if (VRComfortVignette.Instance != null)
				VRComfortVignette.Instance.SetMoving(false);
		}
	}
```

### 修改 6：替换整个 HandleButtonEvents 方法

找到整个 HandleButtonEvents 方法，替换为：

```csharp
	private void HandleButtonEvents()
	{
		if (controller.GetPressDown(EVRButtonId.k_EButton_ApplicationMenu))
		{
			menuDownTime = Time.time;
		}

		if (controller.GetPress(EVRButtonId.k_EButton_ApplicationMenu) && Time.time - menuDownTime > 1.5f)
		{
			resetGUIPosition();
			menuDownTime = Time.time;
		}

		if (controller.GetPress(EVRButtonId.k_EButton_Axis1) && controller.GetPress(EVRButtonId.k_EButton_Grip) && controller.GetPress(EVRButtonId.k_EButton_ApplicationMenu) && Time.time - menuDownTime > 0.5f)
		{
			lockRotXZ = !lockRotXZ;
			if (lockRotXZ)
			{
				ResetRotation();
			}
			menuDownTime = Time.time;
		}

		bool flag = false;

		// 近距离高亮目标：Trigger 点击自动在工作区树中选中
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

		// 直接接触目标：Trigger 点击选中（与上面互斥）
		if (!flag && controller.GetPressDown(EVRButtonId.k_EButton_Axis1) && (Object)(object)lastGrabbedObject != (Object)null && (Object)(object)lastGrabbedObject.GetComponent<MoveableGUIObject>() != (Object)null)
		{
			GuideObject guideObject = lastGrabbedObject.GetComponent<MoveableGUIObject>().guideObject;
			if ((Object)(object)guideObject != (Object)null)
			{
				if ((Object)(object)guideObject.guideSelect != (Object)null && (Object)(object)guideObject.guideSelect.treeNodeObject != (Object)null)
				{
					guideObject.guideSelect.treeNodeObject.OnClickSelect();
				}
				else
				{
					Singleton<GuideObjectManager>.Instance.selectObject = guideObject;
				}
				flag = true;
			}
		}

		if (controller.GetPressDown(EVRButtonId.k_EButton_Axis1) && !flag)
		{
			VRLog.Info("Called on Select VRToggle");
			if (Object.op_Implicit((Object)(object)gripMenuHandler) && gripMenuHandler.LaserVisible)
			{
				VRItemObjMoveHelper.Instance.VRToggleObjectSelectOnCursor();
			}
		}
	}
```

关键改动说明：
- 新增近距离高亮目标的 Trigger 点击选中（在 `bool flag = false;` 之后）
- 原有的直接接触选中改为 `if (!flag && ...)` 以确保与近距离选中互斥
- 其余逻辑完全不变

### 修改 7：替换整个 HandleObjectGrab 方法

找到整个 HandleObjectGrab 方法，替换为：

```csharp
	private void HandleObjectGrab()
	{
		if ((Object)(object)grabHandle == (Object)null)
		{
			grabHandle = new GameObject("__GripMoveGrabHandle__");
			grabHandle.transform.parent = ((Component)this).transform;
			grabHandle.transform.position = ((Component)this).transform.position;
			grabHandle.transform.rotation = ((Component)this).transform.rotation;
		}

		// Proximity grab: when grip pressed near character IK target but not directly touching
		if (controller.GetPressDown(EVRButtonId.k_EButton_Grip) && !screenGrabbed && _proximityTarget != null)
		{
			screenGrabbed = true;
			lastGrabbedObject = ((Component)_proximityTarget).gameObject;
			ClearProximityHighlight();
		}

		bool pressDown = controller.GetPressDown(EVRButtonId.k_EButton_Grip);
		bool press = controller.GetPress(EVRButtonId.k_EButton_Grip);
		bool pressUp = controller.GetPressUp(EVRButtonId.k_EButton_Grip);

		if (pressDown && screenGrabbed && (Object)(object)lastGrabbedObject != (Object)null && (Object)(object)grabHandle != (Object)null)
		{
			grabbingObject = lastGrabbedObject;
			grabHandle.transform.position = lastGrabbedObject.transform.position;
			grabHandle.transform.rotation = lastGrabbedObject.transform.rotation;
			if ((Object)(object)lastGrabbedObject.GetComponent<MoveableGUIObject>() != (Object)null)
			{
				MoveableGUIObject component = lastGrabbedObject.GetComponent<MoveableGUIObject>();
				if ((Object)(object)component.guideObject != (Object)null)
				{
					ApplyFingerFKIfNeeded(component.guideObject);
					grabHandle.transform.rotation = component.guideObject.transformTarget.rotation;
					grabbingObject.transform.rotation = component.guideObject.transformTarget.rotation;
					component.OnMoveStart();
				}
			}
			// 抓取开始触觉反馈
			if (controller != null)
				controller.TriggerHapticPulse(1500, EVRButtonId.k_EButton_Axis0);
			_smoothGrabInitialized = false;
		}

		if (press && (Object)(object)grabbingObject != (Object)null)
		{
			Vector3 targetPos = grabHandle.transform.position;
			Quaternion targetRot = grabHandle.transform.rotation;

			// 检测是否为 IK 目标，决定是否使用平滑插值
			MoveableGUIObject mgo = grabbingObject.GetComponent<MoveableGUIObject>();
			bool isIKTarget = mgo != null && (Object)(object)mgo.guideObject != (Object)null;

			if (isIKTarget)
			{
				// IK 目标使用弹簧阻尼跟随，让操控有物理感
				if (!_smoothGrabInitialized)
				{
					_smoothGrabPos = targetPos;
					_smoothGrabRot = targetRot;
					_smoothGrabInitialized = true;
				}

				float smoothness = 15f; // 约 0.07 秒达到 63%，轻微弹簧感
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

		if (screenGrabbed && (Object)(object)grabbingObject != (Object)null && pressUp)
		{
			if ((Object)(object)grabbingObject.GetComponent<MoveableGUIObject>() != (Object)null)
			{
				grabbingObject.GetComponent<MoveableGUIObject>().OnReleased();
			}
			// 释放触觉反馈
			if (controller != null)
				controller.TriggerHapticPulse(800, EVRButtonId.k_EButton_Axis0);
			_smoothGrabInitialized = false;
			grabbingObject = null;
		}
	}
```

关键改动说明：
- 抓取开始时（pressDown 块末尾）添加 `_smoothGrabInitialized = false;`
- 连续跟随（press 块）改为区分 IK 目标（弹簧插值）和非 IK 目标（直接跟随）
- 释放时（pressUp 块）添加 `_smoothGrabInitialized = false;`
- 前半部分（grabHandle 创建、proximity grab、pressDown 初始化）几乎不变

### 修改 8：HandleGripWorldMove 添加双手缩放检测

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

### 修改 9：UpdateProximityDetection 添加高亮脉冲动画

找到（在 UpdateProximityDetection 方法末尾）：
```csharp
		else if (_proximityTarget != null && _proximityHighlight != null && _proximityHighlight.activeSelf)
		{
			// Update position for existing highlight
			_proximityHighlight.transform.position = _proximityTarget.transform.position;
		}
	}
```

替换为：
```csharp
		else if (_proximityTarget != null && _proximityHighlight != null && _proximityHighlight.activeSelf)
		{
			// Update position for existing highlight
			_proximityHighlight.transform.position = _proximityTarget.transform.position;
		}

		// 高亮脉冲动画（缓慢呼吸效果）
		if (_proximityHighlight != null && _proximityHighlight.activeSelf)
		{
			float pulse = 0.035f * (1f + 0.2f * Mathf.Sin(Time.time * 5f));
			_proximityHighlight.transform.localScale = Vector3.one * pulse;
		}
	}
```

### 修改 10：添加新方法 UpdateGrabLine

在 ClearProximityHighlight 方法之后、OnEnable 方法之前，添加新方法：

```csharp
	private void UpdateGrabLine()
	{
		if (grabbingObject == null)
		{
			if (_grabLine != null)
				((Component)_grabLine).gameObject.SetActive(false);
			return;
		}

		// 只对有 guideObject 的 IK 目标显示连线
		MoveableGUIObject mgo = grabbingObject.GetComponent<MoveableGUIObject>();
		if (mgo == null || (Object)(object)mgo.guideObject == (Object)null)
		{
			if (_grabLine != null)
				((Component)_grabLine).gameObject.SetActive(false);
			return;
		}

		// 延迟创建 LineRenderer
		if (_grabLine == null)
		{
			GameObject lineObj = new GameObject("_VRGrabLine");
			Object.DontDestroyOnLoad(lineObj);
			_grabLine = lineObj.AddComponent<LineRenderer>();
			_grabLine.material = new Material(MaterialHelper.GetColorZOrderShader());
			_grabLine.material.renderQueue = 3600;
			_grabLine.SetVertexCount(2);
			_grabLine.useWorldSpace = true;
			_grabLine.SetWidth(0.005f, 0.002f);
		}

		((Component)_grabLine).gameObject.SetActive(true);

		Vector3 handPos = ((Component)this).transform.position;
		Vector3 targetPos = mgo.guideObject.transformTarget.position;
		float dist = Vector3.Distance(handPos, targetPos);

		// 颜色根据距离变化：近=绿色，中=黄色，远=橙色
		Color lineColor;
		if (dist < 0.1f)
			lineColor = new Color(0f, 1f, 0.5f, 0.4f);
		else if (dist < 0.3f)
			lineColor = new Color(1f, 1f, 0f, 0.5f);
		else
			lineColor = new Color(1f, 0.5f, 0f, 0.7f);

		_grabLine.material.color = lineColor;
		_grabLine.SetPosition(0, handPos);
		_grabLine.SetPosition(1, targetPos);
	}
```

注意事项：
- LineRenderer 使用 Unity 5.x API：SetVertexCount（不是 positionCount），
  SetWidth（不是 startWidth/endWidth），SetPosition
- 使用 MaterialHelper.GetColorZOrderShader()（项目自带 shader，已验证可用）
- DontDestroyOnLoad 因为 grabbingObject 可能跨场景
- 不需要新的 using（所有依赖已在文件顶部）
```

---

## Prompt 5: 手部触摸变色反馈

```
修改 2 个文件。项目：E:\KK_VR\KKCharaStudioVRPlugin
Unity 5.x，.NET 3.5，LangVersion 11.0，SDK 风格 csproj。

### 文件 1：E:\KK_VR\KKCharaStudioVRPlugin\KKCharaStudioVR\VRHandModelManager.cs

做 3 处修改：

**修改 A：在 HandContext 类中添加字段**

找到：
```csharp
            public float currentTriggerVal;
```

在其后添加：
```csharp
            public float touchFeedback; // 0 = not touching, 1 = touching
```

**修改 B：添加公共方法 NotifyTouch**

在 GetFingerTipTransform 方法之后、UpdateSingleHand 方法之前，添加：

```csharp
        /// <summary>
        /// 由 VRHandHapticTrigger 调用，通知手部正在触碰角色。
        /// </summary>
        public void NotifyTouch(bool isLeft)
        {
            HandContext h = isLeft ? leftHand : rightHand;
            if (h != null) h.touchFeedback = 1f;
        }
```

**修改 C：在 UpdateSingleHand 中替换颜色更新逻辑**

找到：
```csharp
            if (h.material != null && !Mathf.Approximately(h.material.color.a, alpha))
            {
                Color c = h.material.color;
                c.a = alpha;
                h.material.color = c;
            }
```

替换为：
```csharp
            // 触摸反馈：接触角色时渐变为暖色
            h.touchFeedback = Mathf.Lerp(h.touchFeedback, 0f, Time.deltaTime * 4f);
            if (h.material != null)
            {
                float t = h.touchFeedback;
                // 默认色 (0.8, 0.8, 0.9) → 触摸色 (1.0, 0.7, 0.75)
                float cr = Mathf.Lerp(0.8f, 1.0f, t);
                float cg = Mathf.Lerp(0.8f, 0.7f, t);
                float cb = Mathf.Lerp(0.9f, 0.75f, t);
                Color newCol = new Color(cr, cg, cb, alpha);
                if (h.material.color != newCol)
                    h.material.color = newCol;
            }
```

注意：这段代码同时处理了 alpha（来自 settings.HandModelAlpha）和 RGB（触摸反馈），
替换了原来只处理 alpha 的代码。变量名用 cr/cg/cb 而不是 r/g/b 以避免与 Renderer 变量冲突。

### 文件 2：E:\KK_VR\KKCharaStudioVRPlugin\KKCharaStudioVR\VRHandHapticTrigger.cs

做 2 处修改：

**修改 A：添加缓存字段**

找到：
```csharp
        private float _lastPulseTime;
```

在其后添加：
```csharp
        private bool _isLeftHand;
```

**修改 B：在 Start 中初始化 _isLeftHand**

找到：
```csharp
            _trackedObject = GetComponentInParent<SteamVR_TrackedObject>();
```

在其后添加：
```csharp
            _isLeftHand = GetComponentInParent<VRGIN.Controls.LeftController>() != null;
```

**修改 C：在 OnTriggerStay 中通知手部模型变色**

找到：
```csharp
                    _lastPulseTime = Time.time;
                }
            }
        }
    }
}
```

替换为：
```csharp
                    _lastPulseTime = Time.time;

                    // 通知手部模型变色
                    if (VRHandModelManager.Instance != null)
                        VRHandModelManager.Instance.NotifyTouch(_isLeftHand);
                }
            }
        }
    }
}
```

注意事项：
- _isLeftHand 在 Start 中缓存，而不是在 OnTriggerStay 中每帧 GetComponentInParent
- 使用完全限定名 VRGIN.Controls.LeftController，不需要额外的 using
- NotifyTouch 设置 touchFeedback=1，然后在 UpdateSingleHand 中每帧 Lerp 回 0（自动衰减）
```

---

## 验证清单

执行完所有 5 个 Prompt 后，检查：

1. **新文件存在**：
   - `VRComfortVignette.cs` — 含 SetMoving, CreateVignetteMesh, UpdateVignetteColors
   - `VRTwoHandScale.cs` — 含 IsScaling 属性, scale-invariant 距离计算

2. **KKCharaStudioVRSettings.cs** — 新增 3 个属性：
   ComfortVignetteEnabled, ComfortVignetteRadius, TwoHandScaleEnabled

3. **KKCharaStudioVRGUI.cs** — 新增 "舒适设置" 和 "高级设置" 两个 GUI 节

4. **VRLoader.cs** — AddComponent 顺序应为：
   DynamicBoneColliderManager → KKCharaStudioVRGUI → VRHandModelManager → 
   VRQuickActions → VRComfortVignette → VRTwoHandScale

5. **GripMoveKKCharaStudioTool.cs** — 检查：
   - 新字段：_grabLine, _smoothGrabPos, _smoothGrabRot, _smoothGrabInitialized
   - HandleThumbstickLocomotion 有 else 分支调用 SetMoving(false)
   - HandleButtonEvents 有 proximity target 选中逻辑（在 flag=false 之后）
   - HandleObjectGrab 有 IK 平滑插值（Lerp/Slerp with smoothness=15）
   - HandleGripWorldMove 开头有 VRTwoHandScale.IsScaling 检查
   - UpdateProximityDetection 末尾有脉冲动画
   - UpdateGrabLine 新方法存在

6. **VRHandModelManager.cs** — HandContext 有 touchFeedback 字段，
   有 NotifyTouch 方法，UpdateSingleHand 有 RGB+Alpha 颜色混合

7. **VRHandHapticTrigger.cs** — 有 _isLeftHand 缓存字段，
   OnTriggerStay 调用 VRHandModelManager.Instance.NotifyTouch

8. **编译测试**：确保没有命名冲突、缺少 using、API 版本错误
