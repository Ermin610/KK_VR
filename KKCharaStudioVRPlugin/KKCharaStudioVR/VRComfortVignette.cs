using System.Collections.Generic;
using UnityEngine;
using VRGIN.Core;

namespace KKCharaStudioVR
{
    public class VRComfortVignette : MonoBehaviour
    {
        public static VRComfortVignette Instance { get; private set; }

        private GameObject _vignetteObj;
        private MeshRenderer _renderer;
        private Material _material;
        private Mesh _mesh;
        private Color[] _colors;
        private float[] _normalizedRadii;
        private float _currentIntensity;
        private float _targetIntensity;
        private float _lastRenderedRadius = float.NaN;
        private float _lastRenderedIntensity = float.NaN;
        private bool _leftMoving;
        private bool _rightMoving;
        private bool _legacyMoving;

        private const int Segments = 32;
        private const int Rings = 8;
        private const float MeshRadius = 0.6f;
        private const float MeshDistance = 0.2f;

        // Kept for binary compatibility with callers built against older releases.
        public void SetMoving(bool isMoving)
        {
            _legacyMoving = isMoving;
            RefreshTargetIntensity();
        }

        public void SetMoving(bool isLeftHand, bool isMoving)
        {
            if (isLeftHand)
                _leftMoving = isMoving;
            else
                _rightMoving = isMoving;
            RefreshTargetIntensity();
        }

        private void RefreshTargetIntensity()
        {
            _targetIntensity = _leftMoving || _rightMoving || _legacyMoving ? 1f : 0f;
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
                _lastRenderedIntensity = float.NaN;
                return;
            }

            _currentIntensity = Mathf.Lerp(_currentIntensity, _targetIntensity, Time.deltaTime * 8f);
            if (_currentIntensity < 0.01f)
            {
                if (_vignetteObj != null) _vignetteObj.SetActive(false);
                _currentIntensity = 0f;
                return;
            }

            if (_vignetteObj == null)
                return;

            if (VR.Camera == null || VR.Camera.Head == null)
            {
                _vignetteObj.SetActive(false);
                return;
            }

            _vignetteObj.SetActive(true);
            Transform head = VR.Camera.Head;
            _vignetteObj.transform.position = head.position;
            _vignetteObj.transform.rotation = head.rotation;
            UpdateVignetteColors(settings.ComfortVignetteRadius, _currentIntensity);
        }

        private void CreateVignetteMesh()
        {
            _vignetteObj = new GameObject("VRComfortVignette");
            Object.DontDestroyOnLoad(_vignetteObj);

            MeshFilter meshFilter = _vignetteObj.AddComponent<MeshFilter>();
            _renderer = _vignetteObj.AddComponent<MeshRenderer>();

            int vertCount = 1 + Segments * Rings;
            Vector3[] vertices = new Vector3[vertCount];
            _colors = new Color[vertCount];
            _normalizedRadii = new float[vertCount];
            vertices[0] = new Vector3(0f, 0f, MeshDistance);

            for (int ring = 1; ring <= Rings; ring++)
            {
                float normalizedRadius = (float)ring / Rings;
                float ringRadius = normalizedRadius * MeshRadius;
                for (int segment = 0; segment < Segments; segment++)
                {
                    float angle = (float)segment / Segments * Mathf.PI * 2f;
                    int index = 1 + (ring - 1) * Segments + segment;
                    vertices[index] = new Vector3(
                        Mathf.Cos(angle) * ringRadius,
                        Mathf.Sin(angle) * ringRadius,
                        MeshDistance);
                    _normalizedRadii[index] = normalizedRadius;
                }
            }

            List<int> triangles = new List<int>();
            for (int segment = 0; segment < Segments; segment++)
            {
                int next = (segment + 1) % Segments;
                triangles.Add(0);
                triangles.Add(1 + segment);
                triangles.Add(1 + next);
            }

            for (int ring = 0; ring < Rings - 1; ring++)
            {
                int ringStart = 1 + ring * Segments;
                int nextRingStart = 1 + (ring + 1) * Segments;
                for (int segment = 0; segment < Segments; segment++)
                {
                    int next = (segment + 1) % Segments;
                    triangles.Add(ringStart + segment);
                    triangles.Add(nextRingStart + segment);
                    triangles.Add(nextRingStart + next);
                    triangles.Add(ringStart + segment);
                    triangles.Add(nextRingStart + next);
                    triangles.Add(ringStart + next);
                }
            }

            _mesh = new Mesh();
            _mesh.name = "VRComfortVignetteMesh";
            _mesh.MarkDynamic();
            _mesh.vertices = vertices;
            _mesh.triangles = triangles.ToArray();
            _mesh.colors = _colors;
            _mesh.RecalculateBounds();
            meshFilter.sharedMesh = _mesh;

            _material = new Material(Shader.Find("Sprites/Default"));
            _material.renderQueue = 5000;
            _renderer.sharedMaterial = _material;
            _renderer.shadowCastingMode = (UnityEngine.Rendering.ShadowCastingMode)0;
            _renderer.receiveShadows = false;
            _vignetteObj.SetActive(false);
        }

        private void UpdateVignetteColors(float radius, float intensity)
        {
            if (_mesh == null || _colors == null || _normalizedRadii == null) return;

            radius = Mathf.Clamp(radius, 0.01f, 0.99f);
            intensity = Mathf.Clamp01(intensity);
            if (Mathf.Abs(radius - _lastRenderedRadius) < 0.0005f &&
                Mathf.Abs(intensity - _lastRenderedIntensity) < 0.0005f)
                return;

            float fadeWidth = Mathf.Max(0.001f, 1f - radius);
            for (int i = 0; i < _colors.Length; i++)
            {
                float alpha = 0f;
                if (_normalizedRadii[i] > radius)
                {
                    alpha = Mathf.Clamp01((_normalizedRadii[i] - radius) / fadeWidth);
                    alpha = alpha * alpha * intensity;
                }
                _colors[i] = new Color(0f, 0f, 0f, alpha);
            }

            _mesh.colors = _colors;
            _lastRenderedRadius = radius;
            _lastRenderedIntensity = intensity;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_vignetteObj != null) Destroy(_vignetteObj);
            if (_material != null) Destroy(_material);
            if (_mesh != null) Destroy(_mesh);
        }
    }
}
