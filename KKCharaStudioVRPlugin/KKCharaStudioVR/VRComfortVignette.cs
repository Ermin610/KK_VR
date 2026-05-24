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
