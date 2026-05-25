using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VRGIN.Core;
using VRGIN.Modes;
using Valve.VR;

namespace KKCharaStudioVR
{
    internal class VRHandModelManager : MonoBehaviour
    {
        private class FingerContext
        {
            public List<Transform> joints = new List<Transform>();
        }

        private class HandContext
        {
            public GameObject root;
            public SteamVR_TrackedObject trackedObj;
            public FingerContext[] fingers;
            public Material material;
            public float currentGripVal;
            public float currentTriggerVal;
            public float touchFeedback; // 0 = not touching, 1 = touching
        }

        private HandContext leftHand;
        private HandContext rightHand;
        private KKCharaStudioVRSettings settings;
        private bool initialized;

        public static VRHandModelManager Instance { get; private set; }

        void Start()
        {
            Instance = this;
            if (VR.Manager != null && VR.Manager.Context != null)
            {
                settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
            }
            StartCoroutine(InitHandsCo());
        }

        private IEnumerator InitHandsCo()
        {
            // Wait until controllers are available — same pattern as DynamicBoneColliderManager
            while (VR.Mode == null || !(VR.Mode is StandingMode)
                || VR.Mode.Left == null || VR.Mode.Right == null)
            {
                yield return new WaitForSeconds(0.5f);
            }

            var leftTracked = ((Component)VR.Mode.Left).GetComponent<SteamVR_TrackedObject>();
            var rightTracked = ((Component)VR.Mode.Right).GetComponent<SteamVR_TrackedObject>();

            if (leftTracked != null) leftHand = CreateHand(leftTracked, true);
            if (rightTracked != null) rightHand = CreateHand(rightTracked, false);
            initialized = true;
        }

        void Update()
        {
            if (!initialized) return;

            float alpha = settings != null ? settings.HandModelAlpha : 0.3f;
            float scale = settings != null ? settings.HandModelScale : 1.0f;
            UpdateSingleHand(leftHand, alpha, scale);
            UpdateSingleHand(rightHand, alpha, scale);
        }

        public void SetHandVisible(bool isLeft, bool visible)
        {
            HandContext h = isLeft ? leftHand : rightHand;
            if (h != null && h.root != null)
                h.root.SetActive(visible);
        }

        public void SetVisible(bool visible)
        {
            SetHandVisible(true, visible);
            SetHandVisible(false, visible);
        }

        public Transform GetFingerTipTransform(bool isLeft, int fingerIndex)
        {
            if (!initialized) return null;
            HandContext h = isLeft ? leftHand : rightHand;
            if (h == null || h.fingers == null || fingerIndex < 0 || fingerIndex >= h.fingers.Length) return null;
            var joints = h.fingers[fingerIndex].joints;
            if (joints.Count == 0) return null;
            return joints[joints.Count - 1];
        }

        /// <summary>
        /// 由 VRHandHapticTrigger 调用，通知手部正在触碰角色。
        /// </summary>
        public void NotifyTouch(bool isLeft)
        {
            HandContext h = isLeft ? leftHand : rightHand;
            if (h != null) h.touchFeedback = 1f;
        }

        private void UpdateSingleHand(HandContext h, float alpha, float scale)
        {
            if (h == null || h.root == null) return;

            // 不在这里控制可见性——由工具的 OnEnable/OnDisable 通过 SetHandVisible 管理
            // 如果手部不可见，跳过动画更新
            if (!h.root.activeSelf) return;
            h.root.transform.localScale = Vector3.one * scale;
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
            UpdateHandAnimation(h);
        }
        
        HandContext CreateHand(SteamVR_TrackedObject tracked, bool isLeft)
        {
            HandContext ctx = new HandContext();
            ctx.trackedObj = tracked;
            ctx.root = new GameObject(isLeft ? "VRHandModel_L" : "VRHandModel_R");
            ctx.root.transform.parent = tracked.transform;
            ctx.root.transform.localPosition = Vector3.zero;
            ctx.root.transform.localRotation = Quaternion.identity;

            ctx.material = new Material(MaterialHelper.GetColorZOrderShader());
            float a = settings != null ? settings.HandModelAlpha : 0.3f;
            ctx.material.color = new Color(0.8f, 0.8f, 0.9f, a);

            float mirror = isLeft ? -1f : 1f;
            
            // Palm — flat capsule centered below the controller, aligned to SteamVR wand convention
            // (Y-up, Z-forward from the controller grip point)
            GameObject palm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            palm.transform.parent = ctx.root.transform;
            palm.transform.localScale = new Vector3(0.08f, 0.015f, 0.09f);
            palm.transform.localPosition = new Vector3(0f, -0.03f, 0.04f);
            palm.transform.localRotation = Quaternion.identity;
            SetupMesh(palm, ctx.material);

            // Create fingers — positions relative to front edge of palm
            ctx.fingers = new FingerContext[5];

            float m = mirror; // +1 right, -1 left
            // Thumb — sprouts from side of palm, angled outward
            ctx.fingers[0] = CreateFinger(ctx.root.transform,
                new Vector3(0.04f * m, -0.025f, 0.02f),
                Quaternion.Euler(0, 30 * m, -20 * m),
                ctx.material, 2, 0.022f, 0.012f);
            // Index
            ctx.fingers[1] = CreateFinger(ctx.root.transform,
                new Vector3(0.025f * m, -0.03f, 0.085f),
                Quaternion.identity,
                ctx.material, 3, 0.020f, 0.010f);
            // Middle
            ctx.fingers[2] = CreateFinger(ctx.root.transform,
                new Vector3(0.008f * m, -0.03f, 0.09f),
                Quaternion.identity,
                ctx.material, 3, 0.022f, 0.010f);
            // Ring
            ctx.fingers[3] = CreateFinger(ctx.root.transform,
                new Vector3(-0.010f * m, -0.03f, 0.085f),
                Quaternion.identity,
                ctx.material, 3, 0.020f, 0.010f);
            // Little
            ctx.fingers[4] = CreateFinger(ctx.root.transform,
                new Vector3(-0.028f * m, -0.03f, 0.075f),
                Quaternion.identity,
                ctx.material, 3, 0.017f, 0.008f);
            
            ctx.root.SetActive(false);
            return ctx;
        }
        
        FingerContext CreateFinger(Transform parent, Vector3 pos, Quaternion rot, Material mat, int segments, float segLen, float thickness)
        {
            FingerContext finger = new FingerContext();
            GameObject fingerRoot = new GameObject("Finger");
            fingerRoot.transform.parent = parent;
            fingerRoot.transform.localPosition = pos;
            fingerRoot.transform.localRotation = rot;

            Transform currentParent = fingerRoot.transform;
            for (int i = 0; i < segments; i++)
            {
                GameObject joint = new GameObject("Joint_" + i);
                joint.transform.parent = currentParent;
                joint.transform.localPosition = i == 0 ? Vector3.zero : new Vector3(0, 0, segLen);
                joint.transform.localRotation = Quaternion.identity;

                finger.joints.Add(joint.transform);

                // Capsule oriented along Z — rotated 90° on X so the capsule's Y axis aligns with Z
                GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                mesh.transform.parent = joint.transform;
                float t = thickness * (1f - i * 0.1f); // slightly taper toward fingertip
                mesh.transform.localScale = new Vector3(t, segLen * 0.5f, t);
                mesh.transform.localPosition = new Vector3(0, 0, segLen * 0.5f);
                mesh.transform.localRotation = Quaternion.Euler(90, 0, 0);
                SetupMesh(mesh, mat);

                currentParent = joint.transform;
            }
            return finger;
        }
        
        void SetupMesh(GameObject obj, Material mat)
        {
            Destroy(obj.GetComponent<Collider>());
            Renderer r = obj.GetComponent<Renderer>();
            r.material = mat;
            r.material.renderQueue = 3000;
        }
        
        void UpdateHandAnimation(HandContext ctx)
        {
            if (ctx.trackedObj == null || ctx.trackedObj.index == SteamVR_TrackedObject.EIndex.None) return;
            var controller = SteamVR_Controller.Input((int)ctx.trackedObj.index);
            if (controller == null) return;

            float trigger = controller.GetAxis(EVRButtonId.k_EButton_Axis1).x;
            bool grip = controller.GetPress(EVRButtonId.k_EButton_Grip);

            // Smoothly interpolate both trigger and grip for natural motion
            float smoothRate = Time.deltaTime * 12f;
            ctx.currentTriggerVal = Mathf.Lerp(ctx.currentTriggerVal, trigger, smoothRate);
            float targetGrip = grip ? 1.0f : 0.0f;
            ctx.currentGripVal = Mathf.Lerp(ctx.currentGripVal, targetGrip, smoothRate);

            // Index follows trigger
            AnimateFinger(ctx.fingers[1], ctx.currentTriggerVal * 50f);

            // Middle/Ring/Little follow grip
            AnimateFinger(ctx.fingers[2], ctx.currentGripVal * 65f);
            AnimateFinger(ctx.fingers[3], ctx.currentGripVal * 65f);
            AnimateFinger(ctx.fingers[4], ctx.currentGripVal * 65f);

            // Thumb follows whichever is larger — grip or trigger
            float thumbInput = Mathf.Max(ctx.currentGripVal, ctx.currentTriggerVal * 0.5f);
            AnimateFinger(ctx.fingers[0], thumbInput * 25f);
        }
        
        void AnimateFinger(FingerContext finger, float angle)
        {
            foreach (Transform joint in finger.joints)
            {
                joint.localRotation = Quaternion.Euler(angle, 0, 0);
            }
        }
    }
}
