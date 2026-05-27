using System;
using System.Collections.Generic;
using UnityEngine;

namespace KKCharaStudioVR
{
    public class VRBreastSquasher : MonoBehaviour
    {
        private Vector3 _originalScale = Vector3.one;
        private float _contactThreshold = 0.08f; // 8cm contact threshold
        private float _restoreSpeed = 5.0f; // Bounces back smoothly in ~0.2s

        private void Start()
        {
            _originalScale = transform.localScale;
        }

        private void LateUpdate()
        {
            if (VRHandModelManager.Instance == null) return;

            List<Transform> activePalms = VRHandModelManager.Instance.GetActivePalms();
            if (activePalms == null || activePalms.Count == 0)
            {
                // Smoothly restore original scale if no hands are active
                transform.localScale = Vector3.MoveTowards(transform.localScale, _originalScale, Time.deltaTime * _restoreSpeed);
                return;
            }

            // Find the closest active palm
            Transform closestPalm = null;
            float minDistance = float.MaxValue;
            Vector3 myPos = transform.position;

            foreach (var palm in activePalms)
            {
                if (palm != null)
                {
                    float dist = Vector3.Distance(myPos, palm.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestPalm = palm;
                    }
                }
            }

            if (closestPalm != null && minDistance < _contactThreshold)
            {
                // Calculate press depth ratio (0.0 = just touching, 1.0 = fully pressed at center)
                float pressDepth = Mathf.Clamp01((_contactThreshold - minDistance) / _contactThreshold);

                // Modulate scale: Z compresses (breast flattens), X and Y expand (breast bulges sideways/vertically)
                // Compress Z up to 35% for maximum juicy softness, expand X/Y up to 15%
                float targetZ = _originalScale.z * (1.0f - pressDepth * 0.35f);
                float targetX = _originalScale.x * (1.0f + pressDepth * 0.15f);
                float targetY = _originalScale.y * (1.0f + pressDepth * 0.15f);

                Vector3 targetScale = new Vector3(targetX, targetY, targetZ);

                // Smoothly interpolate scales for soft-body elastic muscle feel
                transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale, Time.deltaTime * 8.0f);
            }
            else
            {
                // Smoothly restore original scale
                transform.localScale = Vector3.MoveTowards(transform.localScale, _originalScale, Time.deltaTime * _restoreSpeed);
            }
        }
    }
}
