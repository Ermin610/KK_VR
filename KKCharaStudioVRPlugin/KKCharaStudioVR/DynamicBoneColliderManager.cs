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

                if (fingerIndex == 1 || fingerIndex == 2)
                {
                    var capsule = obj.AddComponent<CapsuleCollider>();
                    capsule.isTrigger = true;
                    capsule.radius = 0.012f;
                    capsule.height = 0.05f;
                    capsule.direction = 2; // Z-axis forward
                    capsule.center = new Vector3(0, 0, 0.015f);
                }
                else
                {
                    var sphere = obj.AddComponent<SphereCollider>();
                    sphere.isTrigger = true;
                    sphere.radius = dbc.m_Radius;
                }

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

                        var capsule = dbc.GetComponent<CapsuleCollider>();
                        if (capsule != null)
                        {
                            capsule.radius = r;
                            capsule.height = r * 4.0f; // Height is proportional to radius
                        }
                        
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
                                        bone.m_Colliders = new List<DynamicBoneCollider>();
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

                            // 为角色身上的 DynamicBoneCollider 自动生成物理碰撞体（防止物理手穿模）
                            if (chaCtrl.objBodyBone != null)
                            {
                                var dbcs = chaCtrl.objBodyBone.GetComponentsInChildren<DynamicBoneCollider>(true);
                                foreach (var dbc in dbcs)
                                {
                                    if (dbc != null)
                                    {
                                        CreatePhysicalColliderForDynamicBoneCollider(dbc);
                                    }
                                }
                            }

                            // 为乳房骨骼挂载动态物理挤压变形器 (VRBreastSquasher)
                            if (chaCtrl.objBodyBone != null)
                            {
                                Transform[] allTransforms = chaCtrl.objBodyBone.GetComponentsInChildren<Transform>(true);
                                foreach (var t in allTransforms)
                                {
                                    if (t != null && (t.name == "cf_j_breast_L" || t.name == "cf_j_breast_R"))
                                    {
                                        if (t.gameObject.GetComponent<VRBreastSquasher>() == null)
                                        {
                                            t.gameObject.AddComponent<VRBreastSquasher>();
                                            VRLog.Info($"Successfully attached VRBreastSquasher to breast bone: {t.name} on character {chaCtrl.name}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // BetterPenetration 动态食指与中指插入形变注入
                InjectVRFingersToBetterPenetration();
            }
        }

        private void InjectVRFingersToBetterPenetration()
        {
            try
            {
                // 1. 获取 BetterPenetration 的 CoreGame 类型
                Type coreGameType = Type.GetType("Core_BetterPenetration.CoreGame, KK_BetterPenetration");
                if (coreGameType == null) return;

                // 2. 获取静态 of collisionAgents 列表
                System.Reflection.FieldInfo agentsField = coreGameType.GetField("collisionAgents", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                if (agentsField == null) return;

                var agents = agentsField.GetValue(null) as System.Collections.IEnumerable;
                if (agents == null) return;

                // 3. 收集当前左右手食指和中指的碰撞体（index 1 是食指，index 2 是中指）
                List<Collider> ourFingers = new List<Collider>();
                foreach (var ctx in _colliderContexts)
                {
                    if (ctx.collider != null && (ctx.fingerIndex == 1 || ctx.fingerIndex == 2))
                    {
                        var col = ctx.collider.GetComponent<Collider>();
                        if (col != null) ourFingers.Add(col);
                    }
                }

                if (ourFingers.Count == 0) return;

                // 4. 将我们的手指碰撞体注入到每个活跃女角色的 CollisionAgent 中
                foreach (var agent in agents)
                {
                    if (agent == null) continue;
                    foreach (var col in ourFingers)
                    {
                        AddColliderToAgent(agent, col);
                    }
                }
            }
            catch (Exception ex)
            {
                VRLog.Warn("Error in InjectVRFingersToBetterPenetration: " + ex.Message);
            }
        }

        private void AddColliderToAgent(object agent, Collider col)
        {
            if (agent == null || col == null) return;
            try
            {
                System.Reflection.FieldInfo field = agent.GetType().GetField("m_fingerColliders", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (field != null)
                {
                    object val = field.GetValue(agent);
                    if (val is System.Collections.IList list)
                    {
                        if (!list.Contains(col))
                        {
                            list.Add(col);
                            VRLog.Info($"Successfully injected VR finger collider {col.name} to BetterPenetration m_fingerColliders!");
                        }
                    }
                    else if (val is System.Collections.IEnumerable enumerable)
                    {
                        System.Reflection.MethodInfo addMethod = val.GetType().GetMethod("Add", new Type[] { typeof(Collider) }) ?? val.GetType().GetMethod("Add");
                        if (addMethod != null)
                        {
                            System.Reflection.MethodInfo containsMethod = val.GetType().GetMethod("Contains", new Type[] { typeof(Collider) }) ?? val.GetType().GetMethod("Contains");
                            bool alreadyContains = false;
                            if (containsMethod != null)
                            {
                                alreadyContains = (bool)containsMethod.Invoke(val, new object[] { col });
                            }
                            if (!alreadyContains)
                            {
                                addMethod.Invoke(val, new object[] { col });
                                VRLog.Info($"Successfully injected VR finger collider {col.name} via Add method!");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                VRLog.Warn("Failed to add VR finger to BetterPenetration CollisionAgent: " + ex.Message);
            }
        }

        private void CreatePhysicalColliderForDynamicBoneCollider(DynamicBoneCollider dbc)
        {
            if (dbc == null) return;
            
            // Check if we already created a physical collider on this object
            string holderName = "PhysicalColliderHolder_" + dbc.GetInstanceID();
            if (dbc.transform.Find(holderName) != null) return;

            // Create a child object to hold the physical collider (to avoid conflicting with existing colliders)
            GameObject holder = new GameObject(holderName);
            holder.transform.parent = dbc.transform;
            holder.transform.localPosition = Vector3.zero;
            holder.transform.localRotation = Quaternion.identity;
            holder.transform.localScale = Vector3.one;

            // Determine if it's a sphere or capsule
            // In DynamicBoneCollider, if m_Height is 0, it's a sphere. Otherwise, it's a capsule!
            if (dbc.m_Height <= 0f)
            {
                var sphere = holder.AddComponent<SphereCollider>();
                sphere.radius = dbc.m_Radius;
                sphere.center = dbc.m_Center;
                sphere.isTrigger = false;
            }
            else
            {
                var capsule = holder.AddComponent<CapsuleCollider>();
                capsule.radius = dbc.m_Radius;
                capsule.height = dbc.m_Height;
                capsule.center = dbc.m_Center;
                
                // Align direction
                // DynamicBoneCollider.Direction.X = 0, Y = 1, Z = 2
                capsule.direction = (int)dbc.m_Direction;
                capsule.isTrigger = false;
            }
            VRLog.Info($"Created matching standard physical collider for DynamicBoneCollider on bone: {dbc.name}");
        }
    }
}
