using UnityEngine;

public class SteamVR_IK : MonoBehaviour
{
	public Transform target;

	public Transform start;

	public Transform joint;

	public Transform end;

	public Transform poleVector;

	public Transform upVector;

	public float blendPct = 1f;

	[HideInInspector]
	public Transform startXform;

	[HideInInspector]
	public Transform jointXform;

	[HideInInspector]
	public Transform endXform;

	private void LateUpdate()
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0318: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_039a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0331: Unknown result type (might be due to invalid IL or missing references)
		//IL_0339: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_035c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
		//IL_0377: Unknown result type (might be due to invalid IL or missing references)
		//IL_037f: Unknown result type (might be due to invalid IL or missing references)
		//IL_038a: Unknown result type (might be due to invalid IL or missing references)
		if (blendPct < 0.001f)
		{
			return;
		}
		Vector3 val;
		Vector3 val2;
		if (!(upVector != null))
		{
			val = Vector3.Cross(end.position - start.position, joint.position - start.position);
			val2 = val.normalized;
		}
		else
		{
			val2 = upVector.up;
		}
		Vector3 val3 = val2;
		Vector3 position = target.position;
		Quaternion rotation = target.rotation;
		Vector3 result = joint.position;
		Vector3 position2 = start.position;
		Vector3 position3 = poleVector.position;
		val = joint.position - start.position;
		float magnitude = val.magnitude;
		val = end.position - joint.position;
		Solve(position2, position, position3, magnitude, val.magnitude, ref result, out var _, out var up);
		if (!(up == Vector3.zero))
		{
			Vector3 position4 = start.position;
			Vector3 position5 = joint.position;
			Vector3 position6 = end.position;
			Quaternion localRotation = start.localRotation;
			Quaternion localRotation2 = joint.localRotation;
			Quaternion localRotation3 = end.localRotation;
			Transform parent = start.parent;
			Transform parent2 = joint.parent;
			Transform parent3 = end.parent;
			Vector3 localScale = start.localScale;
			Vector3 localScale2 = joint.localScale;
			Vector3 localScale3 = end.localScale;
			if (startXform == null)
			{
				startXform = new GameObject("startXform").transform;
				startXform.parent = ((Component)this).transform;
			}
			startXform.position = position4;
			startXform.LookAt(joint, val3);
			start.parent = startXform;
			if (jointXform == null)
			{
				jointXform = new GameObject("jointXform").transform;
				jointXform.parent = startXform;
			}
			jointXform.position = position5;
			jointXform.LookAt(end, val3);
			joint.parent = jointXform;
			if (endXform == null)
			{
				endXform = new GameObject("endXform").transform;
				endXform.parent = jointXform;
			}
			endXform.position = position6;
			end.parent = endXform;
			startXform.LookAt(result, up);
			jointXform.LookAt(position, up);
			endXform.rotation = rotation;
			start.parent = parent;
			joint.parent = parent2;
			end.parent = parent3;
			end.rotation = rotation;
			if (blendPct < 1f)
			{
				start.localRotation = Quaternion.Slerp(localRotation, start.localRotation, blendPct);
				joint.localRotation = Quaternion.Slerp(localRotation2, joint.localRotation, blendPct);
				end.localRotation = Quaternion.Slerp(localRotation3, end.localRotation, blendPct);
			}
			start.localScale = localScale;
			joint.localScale = localScale2;
			end.localScale = localScale3;
		}
	}

	public static bool Solve(Vector3 start, Vector3 end, Vector3 poleVector, float jointDist, float targetDist, ref Vector3 result, out Vector3 forward, out Vector3 up)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		float num = jointDist + targetDist;
		Vector3 val = end - start;
		Vector3 val2 = poleVector - start;
		Vector3 normalized = val2.normalized;
		float magnitude = val.magnitude;
		result = start;
		if (magnitude < 0.001f)
		{
			result += normalized * jointDist;
			forward = Vector3.Cross(normalized, Vector3.up);
			val2 = Vector3.Cross(forward, normalized);
			up = val2.normalized;
		}
		else
		{
			forward = val * (1f / magnitude);
			val2 = Vector3.Cross(forward, normalized);
			up = val2.normalized;
			if (magnitude + 0.001f < num)
			{
				float num2 = (num + magnitude) * 0.5f;
				if (num2 > jointDist + 0.001f && num2 > targetDist + 0.001f)
				{
					float num3 = Mathf.Sqrt(num2 * (num2 - jointDist) * (num2 - targetDist) * (num2 - magnitude));
					float num4 = 2f * num3 / magnitude;
					float num5 = Mathf.Sqrt(jointDist * jointDist - num4 * num4);
					Vector3 val3 = Vector3.Cross(up, forward);
					result += forward * num5 + val3 * num4;
					return true;
				}
				result += normalized * jointDist;
			}
			else
			{
				result += forward * jointDist;
			}
		}
		return false;
	}
}
