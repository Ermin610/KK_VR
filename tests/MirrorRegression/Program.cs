using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using KKCharaStudioVR.Mirror;
using VRGIN.Core;
using MirrorManager = KKCharaStudioVR.Mirror.Manager;
using UObject = UnityEngine.Object;

internal static class Program
{
    private static int cases, assertions;
    private static readonly FieldInfo Guard = typeof(VRReflection).GetField("s_InsideRendering", BindingFlags.NonPublic | BindingFlags.Static);
    private static void Assert(bool condition, string message)
    { assertions++; if (!condition) throw new Exception(message); }
    private static void Run(string name, Action action)
    { Reset(); action(); cases++; Console.WriteLine("PASS " + name); }
    private static void Reset()
    {
        Guard.SetValue(null, false);
        Material.CloneCalls = Material.FailCloneAt = 0;
        Material.SuccessfulClones.Clear();
        RenderTexture.Allocations = RenderTexture.FailAllocationAt = 0;
        Camera.Renders = Camera.FailRenderAt = 0; Camera.DuringRender = null; Camera.LastRendered = null;
        GameObject.FailAddType = null;
        SteamVR.active = true; SteamVR.Value = new SteamVR(); SteamVR.InstanceReads = 0;
        VR.Active = true; VR.Manager = new VR.ManagerState();
        VRGIN.Helpers.UnityHelper.MissingShader = false;
        QualitySettings.pixelLightCount = 6; GL.invertCulling = true;
        Camera.current = new GameObject("source").AddComponent<Camera>();
    }
    private static Material StockMaterial()
    {
        var result = new Material(new Shader("_ReflectionTex", "_Color", "_MainTex"));
        result.Properties["_Color"] = "original tint";
        result.Properties["_MainTex"] = new object();
        result.renderQueue = 2451; result.shaderKeywords = new[] { "CUSTOM_KEYWORD" };
        return result;
    }
    private static MirrorReflection StockMirror(params Material[] materials)
    {
        var obj = new GameObject("mirror");
        obj.AddComponent<Renderer>().sharedMaterials = materials;
        return obj.AddComponent<MirrorReflection>();
    }
    private static VRReflection Reflection(bool emptyFirstSlot = false)
    {
        var obj = new GameObject("VR mirror");
        var material = new Material(Shader.Stereo);
        obj.AddComponent<Renderer>().sharedMaterials = emptyFirstSlot ? new[] { null, material } : new[] { material };
        return obj.AddComponent<VRReflection>();
    }
    private static void StateRestored()
    {
        Assert(!(bool)Guard.GetValue(null), "recursive rendering guard leaked");
        Assert(GL.invertCulling, "original inverted-culling state not restored");
        Assert(QualitySettings.pixelLightCount == 6, "original light count not restored");
    }
    private static void Fault(string name, Action<VRReflection> inject)
    {
        Run(name, () =>
        {
            var mirror = Reflection(); inject(mirror);
            bool threw = false;
            try { mirror.OnWillRenderObject(); } catch (InvalidOperationException) { threw = true; }
            Assert(threw, "injection did not reach production callback");
            StateRestored();
            Camera.FailRenderAt = 0; RenderTexture.FailAllocationAt = 0;
            SteamVR.Value.hmd.FailProjection = false;
            mirror.GetComponent<Renderer>().FailPropertyBlock = false;
            int before = Camera.Renders;
            mirror.OnWillRenderObject();
            Assert(Camera.Renders == before + 2, "same mirror did not recover on retry");
            Reflection().OnWillRenderObject();
            Assert(Camera.Renders == before + 4, "exception blocked another mirror");
            StateRestored();
        });
    }
    private static void Main()
    {
        Run("stereo state restoration, recursion and property-block preservation", () =>
        {
            var mirror = Reflection(); var other = Reflection();
            mirror.GetComponent<Renderer>().Block.Properties[99] = "custom renderer override";
            Camera.DuringRender = () => { Assert(!GL.invertCulling && QualitySettings.pixelLightCount == 0, "reflection state not applied"); other.OnWillRenderObject(); };
            mirror.OnWillRenderObject();
            Assert(Camera.Renders == 2, "recursion guard did not suppress nested mirrors");
            Assert((string)mirror.GetComponent<Renderer>().Block.Properties[99] == "custom renderer override", "renderer overrides lost");
            StateRestored();
        });
        Fault("left texture allocation failure", mirror => RenderTexture.FailAllocationAt = 1);
        Fault("right texture allocation failure", mirror => RenderTexture.FailAllocationAt = 2);
        Fault("left-eye render failure", mirror => Camera.FailRenderAt = 1);
        Fault("right-eye render failure", mirror => Camera.FailRenderAt = 2);
        Fault("projection failure", mirror => SteamVR.Value.hmd.FailProjection = true);
        Fault("property-block assignment failure", mirror => mirror.GetComponent<Renderer>().FailPropertyBlock = true);
        Fault("shared right texture allocation failure", mirror => { mirror.m_UseSharedRenderTexture = true; RenderTexture.FailAllocationAt = 2; });
        Run("partial reflection-camera allocation can be disabled and retried", () =>
        {
            var mirror = Reflection(); GameObject.FailAddType = typeof(Camera);
            bool threw = false;
            try { mirror.OnWillRenderObject(); } catch (InvalidOperationException) { threw = true; }
            Assert(threw, "camera allocation failure not injected"); StateRestored();
            UObject.Invoke(mirror, "OnDisable");
            GameObject.FailAddType = null; mirror.OnWillRenderObject();
            Assert(Camera.Renders == 2, "camera allocation failure prevented recovery"); StateRestored();
        });
        Run("runtime inactive: no SteamVR initialization", () =>
        {
            SteamVR.active = false; Reflection().OnWillRenderObject();
            Assert(SteamVR.InstanceReads == 0 && Camera.Renders == 0 && RenderTexture.Allocations == 0, "inactive runtime accessed"); StateRestored();
        });
        Run("VR context inactive: no SteamVR access", () =>
        { VR.Active = false; Reflection().OnWillRenderObject(); Assert(SteamVR.InstanceReads == 0 && Camera.Renders == 0, "inactive VR accessed"); StateRestored(); });
        Run("runtime shuts down between active check and instance access", () =>
        { SteamVR.Value = null; Reflection().OnWillRenderObject(); Assert(Camera.Renders == 0 && RenderTexture.Allocations == 0, "null runtime rendered"); StateRestored(); });
        Run("missing HMD", () =>
        { SteamVR.Value.hmd = null; Reflection().OnWillRenderObject(); Assert(Camera.Renders == 0, "missing HMD rendered"); StateRestored(); });
        Run("missing eye data", () =>
        { SteamVR.Value.eyes = null; Reflection().OnWillRenderObject(); Assert(Camera.Renders == 0, "missing eyes rendered"); StateRestored(); });
        Run("incomplete eye data", () =>
        { SteamVR.Value.eyes = new SteamVR.Eye[1]; Reflection().OnWillRenderObject(); Assert(Camera.Renders == 0, "incomplete eyes rendered"); StateRestored(); });
        Run("mono camera without SteamVR and with empty first material slot", () =>
        {
            Camera.current.stereoEnabled = false; SteamVR.active = false;
            Reflection(true).OnWillRenderObject();
            Assert(Camera.Renders == 1 && SteamVR.InstanceReads == 0, "mono mirror did not render independently"); StateRestored();
        });
        Run("left-only eye and disabled light override", () =>
        {
            Camera.current.stereoTargetEye = StereoTargetEyeMask.Left;
            var mirror = Reflection(); mirror.m_DisablePixelLights = false;
            Camera.DuringRender = () => Assert(QualitySettings.pixelLightCount == 6, "lights unexpectedly changed");
            mirror.OnWillRenderObject(); Assert(Camera.Renders == 1, "wrong left-eye render count"); StateRestored();
        });
        Run("right-only eye, resolution and culling masks", () =>
        {
            Camera.current.stereoTargetEye = StereoTargetEyeMask.Right; Camera.current.cullingMask = 0x3ff;
            var mirror = Reflection(); mirror.m_ReflectLayers = 0xff;
            ((KKCharaStudioVR.KKCharaStudioVRSettings)VR.Manager.Context.Settings).MirrorResolutionScale = 0.5f;
            mirror.OnWillRenderObject();
            Assert(Camera.Renders == 1, "wrong right-eye render count");
            Assert(Camera.LastRendered.cullingMask == (0xff & 0x3ff & ~(1 << 4)), "source/mirror masks not intersected");
            Assert(Camera.LastRendered.targetTexture.width == 500 && Camera.LastRendered.targetTexture.height == 400, "resolution scaling changed");
            StateRestored();
        });
        Run("frame plus two reflective slots: properties, settings and ownership", () =>
        {
            var frame = new Material(new Shader("_MainTex")); var surface = StockMaterial(); var second = StockMaterial();
            var stock = StockMirror(frame, surface, null, second);
            var obj = stock.gameObject;
            var renderer = stock.GetComponent<Renderer>();
            stock.m_DisablePixelLights = false; stock.m_ClipPlaneOffset = 0.125f; stock.m_ReflectLayers = 0x7f;
            Assert(new MirrorManager().Fix(stock), "conversion failed");
            var converted = renderer.sharedMaterials;
            Assert(ReferenceEquals(converted[0], frame) && converted[2] == null, "frame or empty slot replaced");
            foreach (int index in new[] { 1, 3 })
            {
                var source = index == 1 ? surface : second;
                Assert(!ReferenceEquals(converted[index], source) && converted[index].shader == Shader.Stereo, "reflective slot not cloned");
                Assert((string)converted[index].Properties["_Color"] == "original tint", "tint lost");
                Assert(ReferenceEquals(converted[index].Properties["_MainTex"], source.Properties["_MainTex"]), "texture lost");
                Assert(converted[index].renderQueue == 2451 && converted[index].shaderKeywords.Single() == "CUSTOM_KEYWORD", "queue or keywords reset by shader switch");
                Assert(source.HasProperty("_ReflectionTex") && !source.Destroyed, "shared source material modified");
            }
            var vr = obj.GetComponent<VRReflection>();
            Assert(!vr.m_DisablePixelLights && vr.m_ClipPlaneOffset == 0.125f && vr.m_ReflectLayers.value == 0x7f, "stock reflection settings lost");
            Assert(stock.Destroyed, "stock updater retained after successful swap");
            UObject.Invoke(vr, "OnDisable");
            Assert(!converted[1].Destroyed && !converted[3].Destroyed, "temporary disable destroyed materials");
            UObject.Destroy(vr);
            Assert(converted[1].Destroyed && converted[3].Destroyed, "owned materials leaked on destroy");
            Assert(!frame.Destroyed && !surface.Destroyed && !second.Destroyed, "original shared materials destroyed");
        });
        Run("no renderer fallback", () =>
        { var stock = new GameObject().AddComponent<MirrorReflection>(); Assert(!new MirrorManager().Fix(stock) && !stock.Destroyed, "missing-renderer fallback failed"); });
        Run("no reflective slots fallback", () =>
        { var stock = StockMirror(new Material(new Shader("_MainTex")), null); Assert(!new MirrorManager().Fix(stock) && !stock.Destroyed, "non-mirror converted"); });
        Run("missing shader fallback", () =>
        { var stock = StockMirror(StockMaterial()); VRGIN.Helpers.UnityHelper.MissingShader = true; Assert(!new MirrorManager().Fix(stock) && !stock.Destroyed, "shader failure removed stock mirror"); });
        foreach (string failure in new[] { "second allocation", "component creation", "material assignment" })
        {
            Run("conversion rollback: " + failure, () =>
            {
                var first = StockMaterial(); var second = StockMaterial(); var stock = StockMirror(first, second);
                var renderer = stock.GetComponent<Renderer>();
                if (failure == "second allocation") Material.FailCloneAt = 2;
                if (failure == "component creation") GameObject.FailAddType = typeof(VRReflection);
                if (failure == "material assignment") renderer.FailNextAssignment = true;
                Assert(!new MirrorManager().Fix(stock), "failed conversion reported success");
                Assert(!stock.Destroyed, "stock updater removed on failure");
                Assert(ReferenceEquals(renderer.sharedMaterials[0], first) && ReferenceEquals(renderer.sharedMaterials[1], second), "original slots not restored");
                Assert(stock.GetComponent<VRReflection>() == null, "partial VR component retained");
                Assert(Material.SuccessfulClones.All(m => m.Destroyed), "partial material allocation leaked");
                Assert(!first.Destroyed && !second.Destroyed, "original shared materials destroyed on failure");
            });
        }
        Console.WriteLine("PASS " + cases + " mirror regression cases / " + assertions + " assertions; managed doubles only, no engine/headset test.");
    }
}
