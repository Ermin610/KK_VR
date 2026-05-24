using System;
using System.Collections;
using System.Collections.Generic;
using Manager;
using UnityEngine;
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
        private HashSet<DynamicBone> _registeredBones = new HashSet<DynamicBone>();

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

                obj.AddComponent<VRHandHapticTrigger>();
            }
        }

        private void OnLevelWasLoaded(int level)
        {
            // Characters are destroyed on level change; clear stale references
            _registeredBones.Clear();
        }

        private void OnDestroy()
        {
            // Unregister our colliders from any surviving DynamicBones
            foreach (var bone in _registeredBones)
            {
                if (bone == null || bone.m_Colliders == null) continue;
                foreach (var hc in _handColliders)
                {
                    bone.m_Colliders.Remove(hc);
                }
            }
            _registeredBones.Clear();
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

                if (Singleton<Character>.Instance != null)
                {
                    _registeredBones.RemoveWhere(b => b == null);

                    foreach (var kvp in Singleton<Character>.Instance.dictEntryChara)
                    {
                        var chaCtrl = kvp.Value;
                        if (chaCtrl != null && chaCtrl.objBodyBone != null)
                        {
                            var bones = chaCtrl.GetComponentsInChildren<DynamicBone>(true);
                            foreach (var bone in bones)
                            {
                                if (bone != null && !_registeredBones.Contains(bone))
                                {
                                    if (bone.m_Colliders == null)
                                    {
                                        bone.m_Colliders = new List<DynamicBoneColliderBase>();
                                    }
                                    foreach (var hc in _handColliders)
                                    {
                                        if (!bone.m_Colliders.Contains(hc))
                                        {
                                            bone.m_Colliders.Add(hc);
                                        }
                                    }
                                    _registeredBones.Add(bone);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
