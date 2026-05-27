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
            public float[] fingerTargets = new float[5];
            public VRGIN.Controls.Controller cachedController;
            public bool lastRenderModelHidden;
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

            // Wait two frames for VRGIN tool system OnEnable/OnDisable cycle to settle,
            // then force-reapply correct hand visibility from settings
            yield return null;
            yield return null;

            bool handEnabled = settings != null ? settings.HandModelEnabled : true;
            if (leftHand != null && leftHand.root != null)
                leftHand.root.SetActive(handEnabled);
            if (rightHand != null && rightHand.root != null)
                rightHand.root.SetActive(handEnabled);
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

            if (h.cachedController != null)
            {
                bool shouldHideRenderModel = h.root.activeSelf;
                if (shouldHideRenderModel != h.lastRenderModelHidden)
                {
                    h.lastRenderModelHidden = shouldHideRenderModel;
                    h.cachedController.SetRenderModelVisible(!shouldHideRenderModel);
                }
            }

            // 不在这里控制可见性——由工具的 OnEnable/OnDisable 通过 SetHandVisible 管理
            // 如果手部不可见，跳过动画更新
            if (!h.root.activeSelf) return;

            // Apply custom offset and rotation
            bool isLeft = (h == leftHand);
            float xOffset = settings != null ? settings.HandOffsetX : 0f;
            float yOffset = settings != null ? settings.HandOffsetY : -0.02f;
            float zOffset = settings != null ? settings.HandOffsetZ : -0.05f;
            h.root.transform.localPosition = new Vector3(isLeft ? xOffset : -xOffset, yOffset, zOffset);

            float pitch = settings != null ? settings.HandRotPitch : 30f;
            float yaw = settings != null ? settings.HandRotYaw : 0f;
            float roll = settings != null ? settings.HandRotRoll : 0f;
            float baseRoll = isLeft ? 90f : -90f;
            float appliedYaw = isLeft ? yaw : -yaw;
            float appliedRoll = isLeft ? roll : -roll;
            h.root.transform.localRotation = Quaternion.Euler(pitch, appliedYaw, baseRoll + appliedRoll);

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
            ctx.cachedController = tracked.GetComponent<VRGIN.Controls.Controller>();
            ctx.lastRenderModelHidden = false;
            ctx.root = new GameObject(isLeft ? "VRHandModel_L" : "VRHandModel_R");
            ctx.root.transform.parent = tracked.transform;
            // The position and rotation are now continuously updated in UpdateSingleHand
            // using the user's custom settings offsets.

            ctx.material = new Material(MaterialHelper.GetColorZOrderShader());
            float a = settings != null ? settings.HandModelAlpha : 0.3f;
            ctx.material.color = new Color(0.8f, 0.8f, 0.9f, a);

            // Swap geometry: left controller gets right-hand shape, right gets left-hand shape.
            // Combined with zRoll this gives thumb-up + palm-inward orientation.
            float mirror = isLeft ? 1f : -1f;
            
            // Palm — flat capsule centered below the controller, aligned to SteamVR wand convention
            // (Y-up, Z-forward from the controller grip point)
            GameObject palm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            palm.transform.parent = ctx.root.transform;
            palm.transform.localScale = new Vector3(0.07f, 0.04f, 0.08f);
            palm.transform.localPosition = new Vector3(0f, -0.025f, 0.04f);
            palm.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            SetupMesh(palm, ctx.material);

            // Create fingers — positions relative to front edge of palm
            ctx.fingers = new FingerContext[5];

            // Wrist — small capsule connecting palm back toward controller
            GameObject wrist = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            wrist.transform.parent = ctx.root.transform;
            wrist.transform.localScale = new Vector3(0.045f, 0.025f, 0.045f);
            wrist.transform.localPosition = new Vector3(0f, -0.025f, -0.01f);
            wrist.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            SetupMesh(wrist, ctx.material);

            float m = mirror; // +1 right, -1 left
            // Thumb — sprouts from side of palm, angled more outward for natural pose
            ctx.fingers[0] = CreateFinger(ctx.root.transform,
                new Vector3(0.035f * m, -0.015f, 0.015f),
                Quaternion.Euler(-10, 50 * m, -30 * m),
                ctx.material, 2, 0.024f, 0.013f);
            // Index
            ctx.fingers[1] = CreateFinger(ctx.root.transform,
                new Vector3(0.024f * m, -0.027f, 0.085f),
                Quaternion.Euler(0, 3 * m, 0),
                ctx.material, 3, 0.018f, 0.009f);
            // Middle (longest)
            ctx.fingers[2] = CreateFinger(ctx.root.transform,
                new Vector3(0.007f * m, -0.027f, 0.09f),
                Quaternion.identity,
                ctx.material, 3, 0.019f, 0.010f);
            // Ring
            ctx.fingers[3] = CreateFinger(ctx.root.transform,
                new Vector3(-0.010f * m, -0.027f, 0.083f),
                Quaternion.Euler(0, -3 * m, 0),
                ctx.material, 3, 0.017f, 0.009f);
            // Little (shortest and thinnest)
            ctx.fingers[4] = CreateFinger(ctx.root.transform,
                new Vector3(-0.026f * m, -0.027f, 0.072f),
                Quaternion.Euler(0, -6 * m, 0),
                ctx.material, 3, 0.014f, 0.007f);
            
            bool handEnabled = settings != null ? settings.HandModelEnabled : true;
            ctx.root.SetActive(handEnabled);
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

            // Read analog inputs
            float trigger = controller.GetAxis(EVRButtonId.k_EButton_Axis1).x;
            
            // Grip: try analog axis first, fall back to binary
            float gripAxis = controller.GetAxis(EVRButtonId.k_EButton_Axis2).x;
            if (gripAxis < 0.01f)
                gripAxis = controller.GetPress(EVRButtonId.k_EButton_Grip) ? 1.0f : 0.0f;
            
            // Thumb: capacitive touch detection
            bool thumbOnStick = controller.GetTouch(EVRButtonId.k_EButton_Axis0);
            bool thumbOnButton = controller.GetTouch(EVRButtonId.k_EButton_A);
            float thumbTarget;
            if (thumbOnStick || thumbOnButton)
                thumbTarget = 0.3f;  // Thumb resting on surface — slightly curled
            else if (gripAxis > 0.7f)
                thumbTarget = 1.0f;  // Fist — thumb fully curled
            else
                thumbTarget = 0.0f;  // Thumb extended
            
            // Smooth interpolation
            float dt = Time.deltaTime;
            ctx.currentTriggerVal = Mathf.Lerp(ctx.currentTriggerVal, trigger, dt * 15f);
            ctx.currentGripVal = Mathf.Lerp(ctx.currentGripVal, gripAxis, dt * 15f);
            
            // Per-finger targets with slight stagger for natural look
            float smoothThumb = Mathf.Lerp(ctx.fingerTargets[0], thumbTarget, dt * 10f);
            ctx.fingerTargets[0] = smoothThumb;
            
            float restCurl = 0.08f; // ~7° at full curl of 85° — subtle natural rest pose

            ctx.fingerTargets[1] = Mathf.Max(ctx.currentTriggerVal, restCurl);   // Index: trigger + rest
            ctx.fingerTargets[2] = Mathf.Max(ctx.currentGripVal, restCurl);       // Middle: grip + rest
            ctx.fingerTargets[3] = Mathf.Lerp(ctx.fingerTargets[3], Mathf.Max(gripAxis, restCurl), dt * 11f); // Ring
            ctx.fingerTargets[4] = Mathf.Lerp(ctx.fingerTargets[4], Mathf.Max(gripAxis, restCurl * 1.2f), dt * 9f); // Little: slightly more curled

            // Apply with realistic curl angles per finger
            AnimateFinger(ctx.fingers[0], ctx.fingerTargets[0] * 70f, true);    // Thumb — isThumb=true
            AnimateFinger(ctx.fingers[1], ctx.fingerTargets[1] * 90f);          // Index — increased from 85
            AnimateFinger(ctx.fingers[2], ctx.fingerTargets[2] * 95f);          // Middle — increased from 90
            AnimateFinger(ctx.fingers[3], ctx.fingerTargets[3] * 92f);          // Ring — increased from 88
            AnimateFinger(ctx.fingers[4], ctx.fingerTargets[4] * 88f);          // Little — increased from 85
        }
        
        void AnimateFinger(FingerContext finger, float totalAngle, bool isThumb = false)
        {
            for (int i = 0; i < finger.joints.Count; i++)
            {
                float factor = 1f - i * 0.275f;
                float curAngle = totalAngle * factor;
                
                if (isThumb)
                {
                    // Thumb curls inward (Z rotation) as well as forward (X rotation)
                    float inwardAngle = curAngle * 0.5f;
                    finger.joints[i].localRotation = Quaternion.Euler(curAngle * 0.7f, 0, inwardAngle);
                }
                else
                {
                    finger.joints[i].localRotation = Quaternion.Euler(curAngle, 0, 0);
                }
            }
        }
    }
}
