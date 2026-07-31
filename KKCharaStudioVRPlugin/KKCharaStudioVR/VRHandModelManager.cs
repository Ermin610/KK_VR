using System;
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
        private class JointContext
        {
            public Transform transform;
            public Quaternion initialLocalRotation;
        }

        private class FingerContext
        {
            public List<JointContext> joints = new List<JointContext>();
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
            public Rigidbody cachedRigidbody;
            public bool isOfficialModel;
            public bool requestedVisible = true;
            public bool trackingStateInitialized;
            public bool wasTracked;
            public bool nativeAccessoriesStateInitialized;
            public bool nativeAccessoriesVisible;
        }

        private HandContext leftHand;
        private HandContext rightHand;
        private KKCharaStudioVRSettings settings;
        private bool initialized;
        private bool _lastPhysicsHandsEnabled;
        private static PhysicMaterial _sharedFrictionless;

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
            _lastPhysicsHandsEnabled = IsPhysicsHandsEnabled();
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

            bool physicsEnabled = IsPhysicsHandsEnabled();
            if (physicsEnabled != _lastPhysicsHandsEnabled)
            {
                ConfigurePhysicsMode(leftHand, true, physicsEnabled);
                ConfigurePhysicsMode(rightHand, false, physicsEnabled);
                _lastPhysicsHandsEnabled = physicsEnabled;
                VRLog.Info("Physics hands mode changed to: " + physicsEnabled);
            }

            float alpha = settings != null ? settings.HandModelAlpha : 0.3f;
            float scale = settings != null ? settings.HandModelScale : 1.0f;
            UpdateSingleHand(leftHand, alpha, scale);
            UpdateSingleHand(rightHand, alpha, scale);
        }

        public void SetHandVisible(bool isLeft, bool visible)
        {
            HandContext h = isLeft ? leftHand : rightHand;
            if (h == null)
                return;

            h.requestedVisible = visible;
            if (h.root != null && !visible)
                h.root.SetActive(false);
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
            return joints[joints.Count - 1].transform;
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

            bool isTracked = HasUsableTracking(h);
            ReportTrackingTransition(h, isTracked);

            bool handModelEnabled = settings != null ? settings.HandModelEnabled : true;
            bool shouldShowCustomHand = isTracked && handModelEnabled && h.requestedVisible;
            if (h.root.activeSelf != shouldShowCustomHand)
                h.root.SetActive(shouldShowCustomHand);

            // Never reveal the native controller at an invalid pose. SteamVR commonly
            // leaves that model at the tracking origin, which can put a large cyan mesh
            // directly inside the HMD view when a controller disconnects.
            bool shouldShowNativeController = isTracked && !shouldShowCustomHand;
            if (isTracked)
            {
                SetNativeControllerVisible(h, shouldShowNativeController);
                SetNativeAccessoriesVisible(h, shouldShowNativeController, !shouldShowNativeController);
            }
            else
            {
                ForceHideNativeController(h);
                SetNativeAccessoriesVisible(h, false, true);
            }

            if (!shouldShowCustomHand)
                return;

            // Apply custom offset and rotation
            bool isLeft = (h == leftHand);
            h.root.transform.localScale = Vector3.one * scale;

            if (!IsPhysicsHandsEnabled())
            {
                h.root.transform.localPosition = GetTargetLocalPosition(isLeft);
                h.root.transform.localRotation = GetTargetLocalRotation(isLeft);
            }

            // 触摸反馈：接触角色时渐变为暖色
            h.touchFeedback = Mathf.Lerp(h.touchFeedback, 0f, Time.deltaTime * 4f);
            if (h.material != null)
            {
                float t = h.touchFeedback;
                // 默认温暖肤色 (0.97, 0.88, 0.85) → 接触粉红肤色 (1.0, 0.65, 0.70)
                float cr = Mathf.Lerp(0.97f, 1.0f, t);
                float cg = Mathf.Lerp(0.88f, 0.65f, t);
                float cb = Mathf.Lerp(0.85f, 0.70f, t);
                Color newCol = new Color(cr, cg, cb, alpha);
                if (h.material.color != newCol)
                    h.material.color = newCol;
            }
            UpdateHandAnimation(h);
        }

        private static bool HasUsableTracking(HandContext h)
        {
            if (h == null || h.trackedObj == null || h.trackedObj.transform == null
                || h.trackedObj.index == SteamVR_TrackedObject.EIndex.None)
                return false;

            try
            {
                SteamVR_Controller.Device device = SteamVR_Controller.Input((int)h.trackedObj.index);
                if (device == null || !device.connected)
                    return false;

                CVRSystem system = OpenVR.System;
                if (system == null
                    || system.GetTrackedDeviceClass((uint)h.trackedObj.index)
                        != ETrackedDeviceClass.Controller)
                    return false;

                if (h.trackedObj.isValid || device.hasTracking)
                    return true;

                // Some Virtual Desktop/OpenVR combinations report bPoseIsValid late.
                // Retain their non-zero pose fallback, but reject explicit loss states.
                bool hasFallbackPose = h.trackedObj.transform.localPosition.sqrMagnitude >= 0.000001f;
                return hasFallbackPose
                    && !device.outOfRange
                    && !device.calibrating
                    && !device.uninitialized;
            }
            catch (Exception ex)
            {
                VRLog.Error("Error checking controller tracking: " + ex.Message);
                return false;
            }
        }

        private static void SetNativeControllerVisible(HandContext h, bool visible)
        {
            if (h == null || h.cachedController == null)
                return;

            bool hidden = !visible;
            if (h.lastRenderModelHidden == hidden)
                return;

            h.lastRenderModelHidden = hidden;
            h.cachedController.SetRenderModelVisible(visible);
        }

        private static void ForceHideNativeController(HandContext h)
        {
            if (h == null || h.cachedController == null)
                return;

            // Other VRGIN tools also toggle this renderer. Reassert the hidden
            // state while tracking is invalid instead of trusting a stale cache.
            h.lastRenderModelHidden = true;
            h.cachedController.SetRenderModelVisible(false);
        }

        private static void SetNativeAccessoriesVisible(
            HandContext h,
            bool visible,
            bool force)
        {
            if (h == null || h.cachedController == null)
                return;
            if (!force
                && h.nativeAccessoriesStateInitialized
                && h.nativeAccessoriesVisible == visible)
                return;

            h.nativeAccessoriesStateInitialized = true;
            h.nativeAccessoriesVisible = visible;
            Transform controllerTransform = ((Component)h.cachedController).transform;
            foreach (Canvas canvas in ((Component)h.cachedController)
                .GetComponentsInChildren<Canvas>(true))
            {
                ((Component)canvas).gameObject.SetActive(visible);
            }

            for (int i = 0; i < controllerTransform.childCount; i++)
            {
                Transform child = controllerTransform.GetChild(i);
                MeshFilter meshFilter = ((Component)child).GetComponent<MeshFilter>();
                bool namedConcealer = child.name == "VRGIN_AlphaConcealer";
                bool legacyConcealer = meshFilter != null
                    && meshFilter.sharedMesh != null
                    && meshFilter.sharedMesh.name.Contains("Sphere")
                    && Mathf.Abs(child.localScale.y) < 0.001f
                    && child.localScale.x >= 0.04f;
                if (namedConcealer || legacyConcealer)
                {
                    ((Component)child).gameObject.SetActive(visible);
                }
            }
        }

        private static void ReportTrackingTransition(HandContext h, bool isTracked)
        {
            if (h.trackingStateInitialized && h.wasTracked == isTracked)
                return;

            h.trackingStateInitialized = true;
            h.wasTracked = isTracked;
            string side = h == null || h.root == null ? "Unknown" : h.root.name;
            if (isTracked)
                VRLog.Info(side + " tracking restored.");
            else
                VRLog.Warn(side + " tracking lost; hiding all controller meshes.");
        }
        
        private Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                Transform found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        HandContext CreateHand(SteamVR_TrackedObject tracked, bool isLeft)
        {
            HandContext ctx = new HandContext();
            ctx.trackedObj = tracked;
            ctx.cachedController = tracked.GetComponent<VRGIN.Controls.Controller>();
            ctx.lastRenderModelHidden = false;
            ctx.requestedVisible = settings != null ? settings.HandModelEnabled : true;
            ctx.root = new GameObject(isLeft ? "VRHandModel_L" : "VRHandModel_R");
            ctx.root.transform.SetParent(tracked.transform, false);

            ctx.material = new Material(MaterialHelper.GetColorZOrderShader());
            float a = settings != null ? settings.HandModelAlpha : 0.3f;
            ctx.material.color = new Color(0.97f, 0.88f, 0.85f, a);

            bool loadedOfficial = false;
            try
            {
                string assetBundleName = "h/common/00_00.unity3d";
                string assetName = isLeft ? "p_handL" : "p_handR";
                GameObject handPrefab = CommonLib.LoadAsset<GameObject>(assetBundleName, assetName, true, null);
                
                if (handPrefab != null)
                {
                    handPrefab.transform.parent = ctx.root.transform;
                    handPrefab.transform.localPosition = Vector3.zero;
                    handPrefab.transform.localRotation = Quaternion.identity;
                    handPrefab.transform.localScale = Vector3.one;

                    // Clean/Disable Animator to allow manual joint control
                    var animator = handPrefab.GetComponent<Animator>();
                    if (animator != null)
                    {
                        DestroyImmediate(animator);
                    }

                    // Clean existing colliders
                    foreach (var col in handPrefab.GetComponentsInChildren<Collider>(true))
                    {
                        DestroyImmediate(col);
                    }

                    // Apply custom hand material (use sharedMaterial so touch color feedback works)
                    ctx.material.renderQueue = 3000;
                    foreach (var r in handPrefab.GetComponentsInChildren<Renderer>(true))
                    {
                        r.sharedMaterial = ctx.material;
                    }

                    // Map finger joints — dynamically detect segment count
                    string handSuffix = isLeft ? "_L" : "_R";
                    string[] fingerNames = { "thumb", "index", "middle", "ring", "little" };
                    ctx.fingers = new FingerContext[5];

                    for (int f = 0; f < 5; f++)
                    {
                        ctx.fingers[f] = new FingerContext();

                        // Probe up to 5 segments; stop at first missing bone
                        for (int s = 1; s <= 5; s++)
                        {
                            string boneName = "cf_j_" + fingerNames[f] + "0" + s + handSuffix;
                            Transform bone = FindDeepChild(handPrefab.transform, boneName);
                            if (bone == null) break;

                            var jc = new JointContext
                            {
                                transform = bone,
                                initialLocalRotation = bone.localRotation
                            };
                            ctx.fingers[f].joints.Add(jc);
                        }

                        int foundCount = ctx.fingers[f].joints.Count;
                        VRLog.Info("Hand {0} finger {1}: found {2} bone segments", isLeft ? "L" : "R", fingerNames[f], foundCount);

                        // Log bone directions for curl axis diagnosis
                        if (foundCount >= 2)
                        {
                            var j0 = ctx.fingers[f].joints[0].transform;
                            var j1 = ctx.fingers[f].joints[1].transform;
                            Vector3 localDir = j0.InverseTransformPoint(j1.position);
                            VRLog.Info("  bone01->02 local dir: ({0:F3}, {1:F3}, {2:F3})", localDir.x, localDir.y, localDir.z);
                            VRLog.Info("  bone01 localRot: {0}", j0.localRotation.eulerAngles);
                        }

                        // Add fingertip collider + haptic to the LAST found joint
                        if (foundCount > 0)
                        {
                            var lastJoint = ctx.fingers[f].joints[foundCount - 1];
                            var tipCol = lastJoint.transform.gameObject.AddComponent<SphereCollider>();
                            tipCol.isTrigger = true;
                            tipCol.radius = settings != null ? settings.ColliderRadius : 0.02f;

                            if (_sharedFrictionless == null)
                            {
                                _sharedFrictionless = new PhysicMaterial("VRHandFrictionless");
                                _sharedFrictionless.staticFriction = 0f;
                                _sharedFrictionless.dynamicFriction = 0f;
                                _sharedFrictionless.frictionCombine = PhysicMaterialCombine.Minimum;
                                _sharedFrictionless.bounciness = 0f;
                                _sharedFrictionless.bounceCombine = PhysicMaterialCombine.Minimum;
                            }
                            tipCol.sharedMaterial = _sharedFrictionless;

                            var trigger = lastJoint.transform.gameObject.AddComponent<VRHandHapticTrigger>();
                            trigger.trackedObject = ctx.trackedObj;
                            trigger.isLeftHand = isLeft;
                        }
                    }

                    // Log renderer types for skinning diagnosis
                    foreach (var r in handPrefab.GetComponentsInChildren<Renderer>(true))
                    {
                        var smr = r as SkinnedMeshRenderer;
                        if (smr != null)
                            VRLog.Info("Hand renderer: SkinnedMeshRenderer '{0}', bones={1}", ((UnityEngine.Object)r).name, smr.bones != null ? smr.bones.Length : 0);
                        else
                            VRLog.Info("Hand renderer: {0} '{1}' (NOT skinned — bone rotation will not deform mesh!)", r.GetType().Name, ((UnityEngine.Object)r).name);
                    }

                    // Add palm BoxCollider
                    Transform palmBone = FindDeepChild(handPrefab.transform, isLeft ? "cf_j_hand_L" : "cf_j_hand_R");
                    if (palmBone != null)
                    {
                        var palmBox = palmBone.gameObject.AddComponent<BoxCollider>();
                        palmBox.size = new Vector3(0.06f, 0.03f, 0.06f);
                        palmBox.center = new Vector3(0f, -0.015f, 0.02f);
                        palmBox.isTrigger = false; // palm blocks solid objects!

                        if (_sharedFrictionless == null)
                        {
                            _sharedFrictionless = new PhysicMaterial("VRHandFrictionless");
                            _sharedFrictionless.staticFriction = 0f;
                            _sharedFrictionless.dynamicFriction = 0f;
                            _sharedFrictionless.frictionCombine = PhysicMaterialCombine.Minimum;
                            _sharedFrictionless.bounciness = 0f;
                            _sharedFrictionless.bounceCombine = PhysicMaterialCombine.Minimum;
                        }
                        palmBox.sharedMaterial = _sharedFrictionless;

                        var trigger = palmBone.gameObject.AddComponent<VRHandHapticTrigger>();
                        trigger.trackedObject = ctx.trackedObj;
                        trigger.isLeftHand = isLeft;
                    }

                    loadedOfficial = true;
                    ctx.isOfficialModel = true;
                    VRLog.Info("Successfully loaded official VR hand for {0}", isLeft ? "Left" : "Right");
                }
            }
            catch (Exception e)
            {
                VRLog.Error($"Error loading official hand: {e.Message}. Falling back to procedural capsule hands.");
            }

            if (!loadedOfficial)
            {
                // Procedural capsule hand fallback!
                float mirror = isLeft ? 1f : -1f;
                GameObject palm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                palm.transform.parent = ctx.root.transform;
                palm.transform.localScale = new Vector3(0.075f, 0.036f, 0.015f);
                palm.transform.localPosition = new Vector3(0f, -0.024f, 0.05f);
                palm.transform.localRotation = Quaternion.Euler(90f, 0f, 90f);

                Collider defaultCol = palm.GetComponent<Collider>();
                if (defaultCol != null) DestroyImmediate(defaultCol);

                BoxCollider boxCol = palm.AddComponent<BoxCollider>();
                boxCol.size = new Vector3(0.85f, 1.8f, 0.7f);

                SetupMesh(palm, ctx.material, false);

                ctx.fingers = new FingerContext[5];
                float m = mirror;
                ctx.fingers[0] = CreateFinger(ctx.root.transform,
                    new Vector3(0.035f * m, -0.015f, 0.015f),
                    Quaternion.Euler(-10, 50 * m, -30 * m),
                    ctx.material, 2, 0.024f, 0.013f);
                ctx.fingers[1] = CreateFinger(ctx.root.transform,
                    new Vector3(0.024f * m, -0.027f, 0.085f),
                    Quaternion.Euler(0, 3 * m, 0),
                    ctx.material, 3, 0.018f, 0.009f);
                ctx.fingers[2] = CreateFinger(ctx.root.transform,
                    new Vector3(0.007f * m, -0.027f, 0.09f),
                    Quaternion.identity,
                    ctx.material, 3, 0.019f, 0.010f);
                ctx.fingers[3] = CreateFinger(ctx.root.transform,
                    new Vector3(-0.010f * m, -0.027f, 0.083f),
                    Quaternion.Euler(0, -3 * m, 0),
                    ctx.material, 3, 0.017f, 0.009f);
                ctx.fingers[4] = CreateFinger(ctx.root.transform,
                    new Vector3(-0.026f * m, -0.027f, 0.072f),
                    Quaternion.Euler(0, -6 * m, 0),
                    ctx.material, 3, 0.014f, 0.007f);
            }

            ConfigurePhysicsMode(ctx, isLeft, IsPhysicsHandsEnabled());
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

                var jc = new JointContext
                {
                    transform = joint.transform,
                    initialLocalRotation = joint.transform.localRotation
                };
                finger.joints.Add(jc);

                float t = thickness * (1f - i * 0.1f); // slightly taper toward fingertip

                // Add a Sphere at each joint knuckle to act as a smooth visual bridge!
                GameObject knuckle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                knuckle.transform.parent = joint.transform;
                knuckle.transform.localPosition = Vector3.zero;
                knuckle.transform.localRotation = Quaternion.identity;
                float ks = t * 1.25f; // slightly larger than thickness for a nice smooth knuckle look
                knuckle.transform.localScale = new Vector3(ks, ks, ks);
                SetupMesh(knuckle, mat, true); // Fingers are triggers (can penetrate!)

                // Capsule oriented along Z — rotated 90° on X so the capsule's Y axis aligns with Z
                GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                mesh.transform.parent = joint.transform;
                mesh.transform.localScale = new Vector3(t, segLen * 0.5f, t);
                mesh.transform.localPosition = new Vector3(0, 0, segLen * 0.5f);
                mesh.transform.localRotation = Quaternion.Euler(90, 0, 0);
                SetupMesh(mesh, mat, true); // Fingers are triggers (can penetrate!)

                currentParent = joint.transform;
            }
            return finger;
        }
        
        void SetupMesh(GameObject obj, Material mat, bool isFinger = false)
        {
            Collider col = obj.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = isFinger;
                if (_sharedFrictionless == null)
                {
                    _sharedFrictionless = new PhysicMaterial("VRHandFrictionless");
                    _sharedFrictionless.staticFriction = 0.0f;
                    _sharedFrictionless.dynamicFriction = 0.0f;
                    _sharedFrictionless.frictionCombine = PhysicMaterialCombine.Minimum;
                    _sharedFrictionless.bounciness = 0.0f;
                    _sharedFrictionless.bounceCombine = PhysicMaterialCombine.Minimum;
                }
                col.sharedMaterial = _sharedFrictionless;
            }
            Renderer r = obj.GetComponent<Renderer>();
            mat.renderQueue = 3000;
            r.sharedMaterial = mat;
        }
        
        private bool IsPhysicsHandsEnabled()
        {
            return settings != null ? settings.PhysicsHandsEnabled : true;
        }

        private Vector3 GetTargetLocalPosition(bool isLeft)
        {
            float xOffset = settings != null ? settings.HandOffsetX : 0f;
            float yOffset = settings != null ? settings.HandOffsetY : -0.02f;
            float zOffset = settings != null ? settings.HandOffsetZ : -0.05f;
            return new Vector3(isLeft ? xOffset : -xOffset, yOffset, zOffset);
        }

        private Quaternion GetTargetLocalRotation(bool isLeft)
        {
            float pitch = settings != null ? settings.HandRotPitch : 30f;
            float yaw = settings != null ? settings.HandRotYaw : 0f;
            float roll = settings != null ? settings.HandRotRoll : 0f;
            float baseRoll = isLeft ? 90f : -90f;
            return Quaternion.Euler(pitch, isLeft ? yaw : -yaw, baseRoll + (isLeft ? roll : -roll));
        }

        private void ConfigurePhysicsMode(HandContext h, bool isLeft, bool enabled)
        {
            if (h == null || h.root == null || h.trackedObj == null || h.trackedObj.transform == null) return;

            SetHandCollisionState(h, false);
            Transform tracker = h.trackedObj.transform;
            Vector3 localPosition = GetTargetLocalPosition(isLeft);
            Quaternion localRotation = GetTargetLocalRotation(isLeft);

            if (enabled)
            {
                if (h.cachedRigidbody == null)
                    h.cachedRigidbody = h.root.AddComponent<Rigidbody>();

                Rigidbody rb = h.cachedRigidbody;
                rb.detectCollisions = false;
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                h.root.transform.SetParent(tracker.parent, true);
                h.root.transform.position = tracker.TransformPoint(localPosition);
                h.root.transform.rotation = tracker.rotation * localRotation;
                rb.position = h.root.transform.position;
                rb.rotation = h.root.transform.rotation;
                rb.useGravity = false;
                rb.mass = 1.0f;
                rb.drag = 0.5f;
                rb.angularDrag = 0.5f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.maxAngularVelocity = 20.0f;
                rb.isKinematic = false;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.detectCollisions = true;
                SetHandCollisionState(h, true);
            }
            else
            {
                if (h.cachedRigidbody != null)
                {
                    h.cachedRigidbody.detectCollisions = false;
                    h.cachedRigidbody.velocity = Vector3.zero;
                    h.cachedRigidbody.angularVelocity = Vector3.zero;
                    h.cachedRigidbody.isKinematic = true;
                }

                h.root.transform.SetParent(tracker, false);
                h.root.transform.localPosition = localPosition;
                h.root.transform.localRotation = localRotation;
            }
        }

        private void SetHandCollisionState(HandContext h, bool enabled)
        {
            foreach (Collider collider in h.root.GetComponentsInChildren<Collider>(true))
                collider.enabled = enabled;
            foreach (VRHandHapticTrigger trigger in h.root.GetComponentsInChildren<VRHandHapticTrigger>(true))
                trigger.enabled = enabled;
        }

        void UpdateHandAnimation(HandContext ctx)
        {
            if (ctx.trackedObj == null || ctx.trackedObj.index == SteamVR_TrackedObject.EIndex.None) return;
            var controller = SteamVR_Controller.Input((int)ctx.trackedObj.index);
            if (controller == null) return;

            // Read analog inputs
            float trigger = controller.GetAxis(EVRButtonId.k_EButton_Axis1).x;
            if (trigger < 0.01f)
                trigger = controller.GetPress(EVRButtonId.k_EButton_Axis1) ? 1.0f : 0.0f;
            
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
            bool official = ctx.isOfficialModel;
            AnimateFinger(ctx.fingers[0], ctx.fingerTargets[0] * 70f, true, official);    // Thumb
            AnimateFinger(ctx.fingers[1], ctx.fingerTargets[1] * 90f, false, official);   // Index
            AnimateFinger(ctx.fingers[2], ctx.fingerTargets[2] * 95f, false, official);   // Middle
            AnimateFinger(ctx.fingers[3], ctx.fingerTargets[3] * 92f, false, official);   // Ring
            AnimateFinger(ctx.fingers[4], ctx.fingerTargets[4] * 88f, false, official);   // Little
        }
        
        void AnimateFinger(FingerContext finger, float totalAngle, bool isThumb = false, bool isOfficialModel = false)
        {
            if (finger == null || finger.joints.Count == 0) return;
            int count = finger.joints.Count;

            for (int i = 0; i < count; i++)
            {
                var joint = finger.joints[i];
                // For 3-bone chains (Koikatu official): MCP=40%, PIP=35%, DIP=25%
                // For 4-bone chains (procedural): gradual decrease
                float factor;
                if (count == 3)
                {
                    float[] dist3 = { 0.40f, 0.35f, 0.25f };
                    factor = dist3[i];
                }
                else if (count == 2)
                {
                    float[] dist2 = { 0.55f, 0.45f };
                    factor = dist2[i];
                }
                else
                {
                    factor = (1f - i * 0.275f) / count * 1.5f; // Original approach, normalized
                    if (factor < 0.1f) factor = 0.1f;
                }
                float curAngle = totalAngle * factor;

                if (isOfficialModel)
                {
                    // Official Koikatu hand model: fingers curl toward palm via positive Z rotation.
                    // Tested: negative Z causes outward hyperextension (wrong direction).
                    if (isThumb)
                    {
                        float inward = curAngle * 0.4f;
                        joint.transform.localRotation = joint.initialLocalRotation
                            * Quaternion.Euler(0, 0, curAngle * 0.7f)
                            * Quaternion.Euler(inward, 0, 0);
                    }
                    else
                    {
                        joint.transform.localRotation = joint.initialLocalRotation
                            * Quaternion.Euler(0, 0, curAngle);
                    }
                }
                else
                {
                    // Procedural capsule hand — bones extend along Z, joints created at identity
                    if (isThumb)
                    {
                        float inwardAngle = curAngle * 0.5f;
                        joint.transform.localRotation = joint.initialLocalRotation * Quaternion.Euler(curAngle * 0.7f, 0, inwardAngle);
                    }
                    else
                    {
                        joint.transform.localRotation = joint.initialLocalRotation * Quaternion.Euler(curAngle, 0, 0);
                    }
                }
            }
        }

        void FixedUpdate()
        {
            if (!initialized) return;

            if (!IsPhysicsHandsEnabled()) return;

            UpdateHandPhysics(leftHand, true);
            UpdateHandPhysics(rightHand, false);
        }

        private void UpdateHandPhysics(HandContext h, bool isLeft)
        {
            if (h == null || h.root == null || h.trackedObj == null || h.trackedObj.transform == null) return;
            if (!h.root.activeSelf) return;

            var rb = h.cachedRigidbody;
            if (rb == null) return;

            Vector3 targetLocalPos = GetTargetLocalPosition(isLeft);
            Quaternion targetLocalRot = GetTargetLocalRotation(isLeft);

            // Convert to global space targets
            Transform tracker = h.trackedObj.transform;
            Vector3 targetPos = tracker.TransformPoint(targetLocalPos);
            Quaternion targetRot = tracker.rotation * targetLocalRot;

            // 1. Position tracking (Velocity-based)
            Vector3 deltaPos = targetPos - rb.position;
            if (float.IsNaN(deltaPos.x) || float.IsNaN(deltaPos.y) || float.IsNaN(deltaPos.z) ||
                float.IsInfinity(deltaPos.x) || float.IsInfinity(deltaPos.y) || float.IsInfinity(deltaPos.z))
            {
                rb.velocity = Vector3.zero;
            }
            else
            {
                // Soft spring tracking (20.0f spring factor) for smooth compliant touch
                Vector3 desiredVelocity = deltaPos * 20.0f;
                float maxVelocity = 5.0f;
                if (desiredVelocity.magnitude > maxVelocity)
                {
                    desiredVelocity = desiredVelocity.normalized * maxVelocity;
                }
                rb.velocity = desiredVelocity;
            }

            // 2. Rotation tracking (Angular velocity-based)
            Quaternion deltaRot = targetRot * Quaternion.Inverse(rb.rotation);
            deltaRot.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            
            if (Mathf.Abs(angle) > 0.1f && axis.sqrMagnitude > 0.0001f)
            {
                if (!float.IsNaN(axis.x) && !float.IsNaN(axis.y) && !float.IsNaN(axis.z) &&
                    !float.IsInfinity(axis.x) && !float.IsInfinity(axis.y) && !float.IsInfinity(axis.z))
                {
                    // Soft spring tracking (20.0f spring factor) for smooth compliant rotation
                    Vector3 desiredAngularVelocity = axis.normalized * (angle * Mathf.Deg2Rad * 20.0f);
                    if (!float.IsNaN(desiredAngularVelocity.x) && !float.IsInfinity(desiredAngularVelocity.x))
                    {
                        float maxAngularVelocity = 20.0f;
                        if (desiredAngularVelocity.magnitude > maxAngularVelocity)
                        {
                            desiredAngularVelocity = desiredAngularVelocity.normalized * maxAngularVelocity;
                        }
                        rb.angularVelocity = desiredAngularVelocity;
                    }
                    else
                    {
                        rb.angularVelocity = Vector3.zero;
                    }
                }
                else
                {
                    rb.angularVelocity = Vector3.zero;
                }
            }
            else
            {
                rb.angularVelocity = Vector3.zero;
            }

            // 3. Teleport fallback if too far (e.g., stuck in geometry)
            if (deltaPos.magnitude > 0.4f)
            {
                rb.position = targetPos;
                rb.rotation = targetRot;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        private void OnDestroy()
        {
            DestroyHand(leftHand);
            DestroyHand(rightHand);
            if (Instance == this) Instance = null;
        }

        private void DestroyHand(HandContext hand)
        {
            if (hand == null) return;
            if (hand.cachedController != null && hand.lastRenderModelHidden)
                hand.cachedController.SetRenderModelVisible(true);
            if (hand.root != null) Destroy(hand.root);
            if (hand.material != null) Destroy(hand.material);
        }
    }
}
