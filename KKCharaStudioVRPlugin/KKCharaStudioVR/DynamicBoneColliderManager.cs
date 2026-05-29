using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Manager;
using UnityEngine;
using Object = UnityEngine.Object;
using VRGIN.Core;

namespace KKCharaStudioVR
{
    public class DynamicBoneColliderManager : MonoBehaviour
    {
        private class ColliderContext
        {
            public DynamicBoneCollider collider;
            public Transform controllerRoot;
            public bool isLeft;
            public int fingerIndex;
            public Vector3 defaultLocalPos;
        }

        private KKCharaStudioVRSettings _settings;
        private List<DynamicBoneCollider> _handColliders = new List<DynamicBoneCollider>();
        private List<ColliderContext> _colliderContexts = new List<ColliderContext>();
        private int _updateCounter = 0;
        
        // Use a generic HashSet<MonoBehaviour> to store registered bones to avoid static compile-time type dependency
        private HashSet<MonoBehaviour> _registeredBonesReflection = new HashSet<MonoBehaviour>();

        public void Start()
        {
            if (VR.Manager != null && VR.Manager.Context != null)
            {
                _settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
            }
            StartCoroutine(InitCollidersCo());
        }

        private IEnumerator InitCollidersCo()
        {
            while (VR.Mode == null || VR.Mode.Left == null || VR.Mode.Right == null)
            {
                yield return new WaitForSeconds(0.5f);
            }

            while (VRHandModelManager.Instance == null)
            {
                yield return new WaitForSeconds(0.5f);
            }

            CreateHandColliders(VR.Mode.Left.transform, true);
            CreateHandColliders(VR.Mode.Right.transform, false);

            StartCoroutine(ScanDynamicBonesCo());
        }

        private void CreateHandColliders(Transform controllerRoot, bool isLeft)
        {
            string prefix = isLeft ? "L_" : "R_";
            string[] parts = { "Palm", "Thumb", "Index", "Middle", "Ring", "Little" };

            // Aligned with VRHandModelManager finger tip positions (mirror = isLeft? -1:1)
            float m = isLeft ? -1f : 1f;
            Vector3[] positions = new Vector3[]
            {
                new Vector3(0f, -0.03f, 0.04f),                        // Palm center
                new Vector3(0.04f * m, -0.025f, 0.065f),               // Thumb tip
                new Vector3(0.025f * m, -0.03f, 0.145f),               // Index tip (3 seg × 0.020)
                new Vector3(0.008f * m, -0.03f, 0.156f),               // Middle tip (3 seg × 0.022)
                new Vector3(-0.010f * m, -0.03f, 0.145f),              // Ring tip
                new Vector3(-0.028f * m, -0.03f, 0.126f),              // Little tip
            };

            for (int i = 0; i < parts.Length; i++)
            {
                GameObject obj = new GameObject(prefix + parts[i] + "_Col");
                
                int fingerIndex = i - 1;
                Transform targetParent = controllerRoot;
                Vector3 targetLocalPos = positions[i];

                if (fingerIndex >= 0 && VRHandModelManager.Instance != null)
                {
                    Transform tip = VRHandModelManager.Instance.GetFingerTipTransform(isLeft, fingerIndex);
                    if (tip != null && tip.gameObject.activeInHierarchy)
                    {
                        targetParent = tip;
                        targetLocalPos = Vector3.zero;
                    }
                }

                obj.transform.parent = targetParent;
                obj.transform.localPosition = targetLocalPos;
                obj.transform.localRotation = Quaternion.identity;

                var dbc = obj.AddComponent<DynamicBoneCollider>();
                dbc.m_Radius = _settings != null ? _settings.ColliderRadius : 0.02f;
                _handColliders.Add(dbc);

                _colliderContexts.Add(new ColliderContext
                {
                    collider = dbc,
                    controllerRoot = controllerRoot,
                    isLeft = isLeft,
                    fingerIndex = fingerIndex,
                    defaultLocalPos = positions[i]
                });

                var sphere = obj.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = dbc.m_Radius;

                var rb = obj.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;

                var trigger = obj.AddComponent<VRHandHapticTrigger>();
                var tracked = controllerRoot.GetComponent<SteamVR_TrackedObject>();
                trigger.trackedObject = tracked;
                trigger.isLeftHand = isLeft;
            }
        }

        private void OnLevelWasLoaded(int level)
        {
            lock (_registeredBonesReflection)
            {
                _registeredBonesReflection.Clear();
            }
        }

        private void OnDestroy()
        {
            lock (_registeredBonesReflection)
            {
                foreach (var bone in _registeredBonesReflection)
                {
                    if (bone == null) continue;
                    try
                    {
                        Type type = bone.GetType();
                        var field = type.GetField("m_Colliders", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                 ?? type.GetField("Colliders", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                        if (field != null)
                        {
                            var list = field.GetValue(bone) as IList;
                            if (list != null)
                            {
                                foreach (var hc in _handColliders)
                                {
                                    list.Remove(hc);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        VRLog.Warn("Failed to unregister collider via reflection: " + ex.Message);
                    }
                }
                _registeredBonesReflection.Clear();
            }
        }

        private void Update()
        {
            if (_settings != null)
            {
                float r = _settings.ColliderRadius;
                bool enabled = _settings.DynamicBoneCollisionEnabled;
                foreach (var dbc in _handColliders)
                {
                    if (dbc != null)
                    {
                        dbc.m_Radius = r;
                        dbc.enabled = enabled;
                        
                        var sphere = dbc.GetComponent<SphereCollider>();
                        if (sphere != null) sphere.radius = r;
                        
                        var trigger = dbc.GetComponent<VRHandHapticTrigger>();
                        if (trigger != null) trigger.enabled = enabled;
                    }
                }
            }

            _updateCounter++;
            if (_updateCounter >= 30)
            {
                _updateCounter = 0;
                if (VRHandModelManager.Instance != null)
                {
                    foreach (var ctx in _colliderContexts)
                    {
                        if (ctx.fingerIndex >= 0 && ctx.collider != null)
                        {
                            Transform tip = VRHandModelManager.Instance.GetFingerTipTransform(ctx.isLeft, ctx.fingerIndex);
                            bool hasTip = (tip != null && tip.gameObject.activeInHierarchy);
                            
                            if (hasTip)
                            {
                                if (ctx.collider.transform.parent != tip)
                                {
                                    ctx.collider.transform.parent = tip;
                                    ctx.collider.transform.localPosition = Vector3.zero;
                                    ctx.collider.transform.localRotation = Quaternion.identity;
                                }
                            }
                            else
                            {
                                if (ctx.collider.transform.parent != ctx.controllerRoot)
                                {
                                    ctx.collider.transform.parent = ctx.controllerRoot;
                                    ctx.collider.transform.localPosition = ctx.defaultLocalPos;
                                    ctx.collider.transform.localRotation = Quaternion.identity;
                                }
                            }
                        }
                    }
                }
            }
        }

        private IEnumerator ScanDynamicBonesCo()
        {
            while (true)
            {
                yield return new WaitForSeconds(1.5f);

                if (_settings != null && !_settings.DynamicBoneCollisionEnabled)
                {
                    continue;
                }

                lock (_registeredBonesReflection)
                {
                    // Clean up null entries
                    var toRemove = new List<MonoBehaviour>();
                    foreach (var b in _registeredBonesReflection)
                    {
                        if (b == null) toRemove.Add(b);
                    }
                    foreach (var b in toRemove)
                    {
                        _registeredBonesReflection.Remove(b);
                    }
                }

                var allChas = FindObjectsOfType<ChaControl>();
                foreach (var chaCtrl in allChas)
                {
                    if (chaCtrl != null && chaCtrl.objBodyBone != null)
                    {
                        // Retrieve all MonoBehaviour scripts attached to character children
                        var behaviours = chaCtrl.GetComponentsInChildren<MonoBehaviour>(true);
                        foreach (var b in behaviours)
                        {
                            if (b == null) continue;
                            
                            string typeName = b.GetType().Name;
                            if (typeName.Contains("DynamicBone") && !typeName.Contains("Collider"))
                            {
                                bool alreadyRegistered = false;
                                lock (_registeredBonesReflection)
                                {
                                    alreadyRegistered = _registeredBonesReflection.Contains(b);
                                }

                                if (!alreadyRegistered)
                                {
                                    RegisterCollidersReflection(b);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void RegisterCollidersReflection(MonoBehaviour bone)
        {
            try
            {
                Type type = bone.GetType();
                FieldInfo field = null;
                Type t = type;
                while (t != null && field == null)
                {
                    field = t.GetField("m_Colliders", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                         ?? t.GetField("Colliders", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    t = t.BaseType;
                }

                if (field != null)
                {
                    var list = field.GetValue(bone) as IList;
                    if (list == null)
                    {
                        // If null, create a new List<DynamicBoneCollider>
                        var listType = typeof(List<>).MakeGenericType(typeof(DynamicBoneCollider));
                        list = Activator.CreateInstance(listType) as IList;
                        field.SetValue(bone, list);
                    }

                    if (list != null)
                    {
                        foreach (var hc in _handColliders)
                        {
                            if (!list.Contains(hc))
                            {
                                list.Add(hc);
                            }
                        }

                        lock (_registeredBonesReflection)
                        {
                            _registeredBonesReflection.Add(bone);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                VRLog.Error("Reflection error in RegisterCollidersReflection: " + ex.Message);
            }
        }
    }
}
