// Managed test doubles only. These tests do not exercise Unity, GPU rendering,
// shader compatibility, projection geometry, or a headset.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine
{
    public class Object
    {
        public bool Destroyed;
        public string name;
        public HideFlags hideFlags;
        public static readonly List<Object> Created = new List<Object>();
        public Object() { Created.Add(this); }
        public int GetInstanceID() { return Created.IndexOf(this); }
        public static implicit operator bool(Object value) { return !ReferenceEquals(value, null) && !value.Destroyed; }
        public static bool operator ==(Object a, Object b)
        {
            bool an = ReferenceEquals(a, null) || a.Destroyed, bn = ReferenceEquals(b, null) || b.Destroyed;
            return an || bn ? an == bn : ReferenceEquals(a, b);
        }
        public static bool operator !=(Object a, Object b) { return !(a == b); }
        public override bool Equals(object obj) { return ReferenceEquals(this, obj); }
        public override int GetHashCode() { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this); }
        public static void Destroy(Object value)
        {
            if (ReferenceEquals(value, null) || value.Destroyed) return;
            if (value is Behaviour behaviour && behaviour.enabled) Invoke(value, "OnDisable");
            Invoke(value, "OnDestroy");
            value.Destroyed = true;
        }
        public static void DestroyImmediate(Object value) { Destroy(value); }
        public static void Invoke(Object value, string method)
        {
            value.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(value, null);
        }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform { get { return gameObject.transform; } }
        public T GetComponent<T>() where T : Component { return gameObject.GetComponent<T>(); }
        public Component GetComponent(Type type) { return gameObject.GetComponent(type); }
    }
    public class Behaviour : Component { public bool enabled = true; }
    public class MonoBehaviour : Behaviour { }
    public class GameObject : Object
    {
        private readonly List<Component> components = new List<Component>();
        public static Type FailAddType;
        public readonly Transform transform = new Transform();
        public GameObject(string objectName = "test", params Type[] types)
        {
            name = objectName;
            foreach (var type in types) Add(type);
        }
        private Component Add(Type type)
        {
            if (type == FailAddType) throw new InvalidOperationException("Injected AddComponent failure");
            var value = (Component)Activator.CreateInstance(type);
            value.gameObject = this; components.Add(value); Invoke(value, "Awake"); return value;
        }
        public T AddComponent<T>() where T : Component { return (T)Add(typeof(T)); }
        public T GetComponent<T>() where T : Component { return (T)GetComponent(typeof(T)); }
        public Component GetComponent(Type type) { return components.Find(c => c && type.IsInstanceOfType(c)); }
    }
    public class Transform
    {
        public Vector3 position, up = new Vector3();
        public Quaternion rotation;
        public Vector3 TransformPoint(Vector3 value) { return value; }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3 normalized { get { return this; } }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return a; }
        public static Vector3 operator *(Vector3 a, float b) { return a; }
        public static float Dot(Vector3 a, Vector3 b) { return 0; }
    }
    public struct Quaternion { public static Quaternion operator *(Quaternion a, Quaternion b) { return a; } }
    public struct Vector4
    {
        private float x, y, z, w;
        public Vector4(float a, float b, float c, float d) { x = a; y = b; z = c; w = d; }
        public float this[int i] { get { return i == 0 ? x : i == 1 ? y : i == 2 ? z : w; } }
    }
    public struct Matrix4x4
    {
        public float m00,m01,m02,m03,m10,m11,m12,m13,m20,m21,m22,m23,m30,m31,m32,m33;
        public static Matrix4x4 identity { get { return new Matrix4x4(); } }
        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b) { return a; }
        public Vector3 MultiplyPoint(Vector3 p) { return p; }
        public Vector3 MultiplyVector(Vector3 p) { return p; }
    }
    public struct LayerMask { public int value; public static implicit operator LayerMask(int value) { return new LayerMask { value = value }; } }
    public enum HideFlags { DontSave, HideAndDontSave }
    public enum StereoTargetEyeMask { Both, Left, Right, None }
    public enum CameraClearFlags { Skybox, SolidColor }
    public struct Color { }
    public class Shader : Object
    {
        public readonly HashSet<string> Properties = new HashSet<string>();
        public static readonly Shader Stereo = new Shader("_LeftReflectionTex", "_RightReflectionTex");
        public Shader(params string[] properties) { foreach (var p in properties) Properties.Add(p); }
        private static readonly Dictionary<string,int> ids = new Dictionary<string,int>();
        public static int PropertyToID(string value) { if (!ids.ContainsKey(value)) ids[value] = ids.Count + 1; return ids[value]; }
    }
    public class Material : Object
    {
        private Shader shaderValue;
        public Shader shader { get { return shaderValue; } set { shaderValue = value; renderQueue = 2000; shaderKeywords = new string[0]; } }
        public int renderQueue = 2000;
        public string[] shaderKeywords = new string[0];
        public readonly Dictionary<string,object> Properties = new Dictionary<string,object>();
        public static int CloneCalls, FailCloneAt;
        public static readonly List<Material> SuccessfulClones = new List<Material>();
        public Material(Shader source) { shaderValue = source; }
        public Material(Material source)
        {
            if (++CloneCalls == FailCloneAt) throw new InvalidOperationException("Injected material allocation failure");
            shaderValue = source.shader; renderQueue = source.renderQueue; shaderKeywords = (string[])source.shaderKeywords.Clone();
            foreach (var pair in source.Properties) Properties.Add(pair.Key, pair.Value);
            SuccessfulClones.Add(this);
        }
        public bool HasProperty(string key) { return shader.Properties.Contains(key); }
    }
    public class MaterialPropertyBlock
    {
        public readonly Dictionary<int,object> Properties = new Dictionary<int,object>();
        public void SetTexture(int id, RenderTexture texture) { Properties[id] = texture; }
        public void CopyFrom(MaterialPropertyBlock source) { Properties.Clear(); foreach (var p in source.Properties) Properties.Add(p.Key,p.Value); }
    }
    public class Renderer : Component
    {
        private Material[] materials = new Material[0];
        public bool enabled = true, FailNextAssignment, FailPropertyBlock;
        public Material[] sharedMaterials
        {
            get { return (Material[])materials.Clone(); }
            set { materials = (Material[])value.Clone(); if (FailNextAssignment) { FailNextAssignment = false; throw new InvalidOperationException("Injected material assignment failure"); } }
        }
        public Material sharedMaterial { get { return materials.Length > 0 ? materials[0] : null; } }
        public readonly MaterialPropertyBlock Block = new MaterialPropertyBlock();
        public void GetPropertyBlock(MaterialPropertyBlock block) { block.CopyFrom(Block); }
        public void SetPropertyBlock(MaterialPropertyBlock block) { if (FailPropertyBlock) throw new InvalidOperationException("Injected property block failure"); Block.CopyFrom(block); }
    }
    public class RenderTexture : Object
    {
        public int width, height;
        public static int Allocations, FailAllocationAt;
        public RenderTexture(int w,int h,int depth)
        {
            if (++Allocations == FailAllocationAt) throw new InvalidOperationException("Injected texture allocation failure");
            width = w; height = h;
        }
    }
    public class Camera : Behaviour
    {
        public static Camera current;
        public static int Renders, FailRenderAt;
        public static Action DuringRender;
        public static Camera LastRendered;
        public bool stereoEnabled = true, orthographic;
        public StereoTargetEyeMask stereoTargetEye = StereoTargetEyeMask.Both;
        public int pixelWidth = 1000, pixelHeight = 800, cullingMask = -1;
        public float nearClipPlane = 0.1f, farClipPlane = 100f, fieldOfView = 60, aspect = 1.25f, orthographicSize = 1;
        public CameraClearFlags clearFlags = CameraClearFlags.SolidColor;
        public Color backgroundColor;
        public Matrix4x4 worldToCameraMatrix, cameraToWorldMatrix, projectionMatrix;
        public RenderTexture targetTexture;
        public void ResetWorldToCameraMatrix() { }
        public Matrix4x4 CalculateObliqueMatrix(Vector4 v) { return projectionMatrix; }
        public void Render()
        {
            Renders++; LastRendered = this; DuringRender?.Invoke();
            if (Renders == FailRenderAt) throw new InvalidOperationException("Injected render failure");
        }
    }
    public class Skybox : Behaviour { public Material material; }
    public class FlareLayer : Component { }
    public static class QualitySettings { public static int pixelLightCount = 4; }
    public static class GL { public static bool invertCulling; }
    public static class Mathf
    {
        public static float Clamp(float v,float min,float max) { return Math.Min(max,Math.Max(min,v)); }
        public static int Max(int a,int b) { return Math.Max(a,b); }
        public static int RoundToInt(float v) { return (int)Math.Round(v); }
    }
}
namespace Valve.VR
{
    public enum EVREye { Eye_Left, Eye_Right }
    public struct HmdMatrix44_t { public float m0,m1,m2,m3,m4,m5,m6,m7,m8,m9,m10,m11,m12,m13,m14,m15; }
    public class CVRSystem
    {
        public bool FailProjection;
        public HmdMatrix44_t GetProjectionMatrix(EVREye eye, float near, float far)
        {
            if (FailProjection) throw new InvalidOperationException("Injected projection failure");
            return new HmdMatrix44_t();
        }
    }
}
public class SteamVR
{
    public static bool active = true;
    public static SteamVR Value = new SteamVR();
    public static int InstanceReads;
    public static SteamVR instance { get { InstanceReads++; return Value; } }
    public Valve.VR.CVRSystem hmd = new Valve.VR.CVRSystem();
    public Eye[] eyes = { new Eye(), new Eye() };
    public class Eye { public UnityEngine.Vector3 pos; public UnityEngine.Quaternion rot; }
}
public class MirrorReflection : UnityEngine.Behaviour
{
    public bool m_DisablePixelLights = true;
    public float m_ClipPlaneOffset = 0.07f;
    public UnityEngine.LayerMask m_ReflectLayers = -1;
}
public static class MatrixExtensions
{
    public static UnityEngine.Vector3 GetPosition(this UnityEngine.Matrix4x4 m) { return new UnityEngine.Vector3(); }
    public static UnityEngine.Quaternion GetRotation(this UnityEngine.Matrix4x4 m) { return new UnityEngine.Quaternion(); }
}
namespace VRGIN.Core
{
    public static class VR
    {
        public static bool Active = true;
        public static ManagerState Manager = new ManagerState();
        public class ManagerState { public ContextState Context = new ContextState(); }
        public class ContextState { public object Settings = new KKCharaStudioVR.KKCharaStudioVRSettings(); }
    }
    public static class VRLog { public static void Error(string message, params object[] args) { } }
}
namespace VRGIN.Helpers
{
    public static class UnityHelper
    {
        public static bool MissingShader;
        public static T LoadFromAssetBundle<T>(byte[] bytes,string name) where T : class
        { return MissingShader ? null : UnityEngine.Shader.Stereo as T; }
    }
}
namespace KKCharaStudioVR { public class KKCharaStudioVRSettings { public float MirrorResolutionScale = 0.75f; } }
