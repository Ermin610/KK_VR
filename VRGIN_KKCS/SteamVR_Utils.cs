using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Valve.VR;

public static class SteamVR_Utils
{
	[Serializable]
	public struct RigidTransform
	{
		public Vector3 pos;

		public Quaternion rot;

		public static RigidTransform identity => new RigidTransform(Vector3.zero, Quaternion.identity);

		public static RigidTransform FromLocal(Transform t)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			return new RigidTransform(t.localPosition, t.localRotation);
		}

		public RigidTransform(Vector3 pos, Quaternion rot)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			this.pos = pos;
			this.rot = rot;
		}

		public RigidTransform(Transform t)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			pos = t.position;
			rot = t.rotation;
		}

		public RigidTransform(Transform from, Transform to)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			Quaternion val = Quaternion.Inverse(from.rotation);
			rot = val * to.rotation;
			pos = val * (to.position - from.position);
		}

		public RigidTransform(HmdMatrix34_t pose)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
			Matrix4x4 matrix = Matrix4x4.identity;
			((Matrix4x4)(ref matrix))[0, 0] = pose.m0;
			((Matrix4x4)(ref matrix))[0, 1] = pose.m1;
			((Matrix4x4)(ref matrix))[0, 2] = 0f - pose.m2;
			((Matrix4x4)(ref matrix))[0, 3] = pose.m3;
			((Matrix4x4)(ref matrix))[1, 0] = pose.m4;
			((Matrix4x4)(ref matrix))[1, 1] = pose.m5;
			((Matrix4x4)(ref matrix))[1, 2] = 0f - pose.m6;
			((Matrix4x4)(ref matrix))[1, 3] = pose.m7;
			((Matrix4x4)(ref matrix))[2, 0] = 0f - pose.m8;
			((Matrix4x4)(ref matrix))[2, 1] = 0f - pose.m9;
			((Matrix4x4)(ref matrix))[2, 2] = pose.m10;
			((Matrix4x4)(ref matrix))[2, 3] = 0f - pose.m11;
			pos = matrix.GetPosition();
			rot = matrix.GetRotation();
		}

		public RigidTransform(HmdMatrix44_t pose)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0103: Unknown result type (might be due to invalid IL or missing references)
			//IL_0109: Unknown result type (might be due to invalid IL or missing references)
			//IL_010a: Unknown result type (might be due to invalid IL or missing references)
			//IL_010f: Unknown result type (might be due to invalid IL or missing references)
			Matrix4x4 matrix = Matrix4x4.identity;
			((Matrix4x4)(ref matrix))[0, 0] = pose.m0;
			((Matrix4x4)(ref matrix))[0, 1] = pose.m1;
			((Matrix4x4)(ref matrix))[0, 2] = 0f - pose.m2;
			((Matrix4x4)(ref matrix))[0, 3] = pose.m3;
			((Matrix4x4)(ref matrix))[1, 0] = pose.m4;
			((Matrix4x4)(ref matrix))[1, 1] = pose.m5;
			((Matrix4x4)(ref matrix))[1, 2] = 0f - pose.m6;
			((Matrix4x4)(ref matrix))[1, 3] = pose.m7;
			((Matrix4x4)(ref matrix))[2, 0] = 0f - pose.m8;
			((Matrix4x4)(ref matrix))[2, 1] = 0f - pose.m9;
			((Matrix4x4)(ref matrix))[2, 2] = pose.m10;
			((Matrix4x4)(ref matrix))[2, 3] = 0f - pose.m11;
			((Matrix4x4)(ref matrix))[3, 0] = pose.m12;
			((Matrix4x4)(ref matrix))[3, 1] = pose.m13;
			((Matrix4x4)(ref matrix))[3, 2] = 0f - pose.m14;
			((Matrix4x4)(ref matrix))[3, 3] = pose.m15;
			pos = matrix.GetPosition();
			rot = matrix.GetRotation();
		}

		public HmdMatrix44_t ToHmdMatrix44()
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			Matrix4x4 val = Matrix4x4.TRS(pos, rot, Vector3.one);
			HmdMatrix44_t result = default(HmdMatrix44_t);
			result.m0 = ((Matrix4x4)(ref val))[0, 0];
			result.m1 = ((Matrix4x4)(ref val))[0, 1];
			result.m2 = 0f - ((Matrix4x4)(ref val))[0, 2];
			result.m3 = ((Matrix4x4)(ref val))[0, 3];
			result.m4 = ((Matrix4x4)(ref val))[1, 0];
			result.m5 = ((Matrix4x4)(ref val))[1, 1];
			result.m6 = 0f - ((Matrix4x4)(ref val))[1, 2];
			result.m7 = ((Matrix4x4)(ref val))[1, 3];
			result.m8 = 0f - ((Matrix4x4)(ref val))[2, 0];
			result.m9 = 0f - ((Matrix4x4)(ref val))[2, 1];
			result.m10 = ((Matrix4x4)(ref val))[2, 2];
			result.m11 = 0f - ((Matrix4x4)(ref val))[2, 3];
			result.m12 = ((Matrix4x4)(ref val))[3, 0];
			result.m13 = ((Matrix4x4)(ref val))[3, 1];
			result.m14 = 0f - ((Matrix4x4)(ref val))[3, 2];
			result.m15 = ((Matrix4x4)(ref val))[3, 3];
			return result;
		}

		public HmdMatrix34_t ToHmdMatrix34()
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			Matrix4x4 val = Matrix4x4.TRS(pos, rot, Vector3.one);
			HmdMatrix34_t result = default(HmdMatrix34_t);
			result.m0 = ((Matrix4x4)(ref val))[0, 0];
			result.m1 = ((Matrix4x4)(ref val))[0, 1];
			result.m2 = 0f - ((Matrix4x4)(ref val))[0, 2];
			result.m3 = ((Matrix4x4)(ref val))[0, 3];
			result.m4 = ((Matrix4x4)(ref val))[1, 0];
			result.m5 = ((Matrix4x4)(ref val))[1, 1];
			result.m6 = 0f - ((Matrix4x4)(ref val))[1, 2];
			result.m7 = ((Matrix4x4)(ref val))[1, 3];
			result.m8 = 0f - ((Matrix4x4)(ref val))[2, 0];
			result.m9 = 0f - ((Matrix4x4)(ref val))[2, 1];
			result.m10 = ((Matrix4x4)(ref val))[2, 2];
			result.m11 = 0f - ((Matrix4x4)(ref val))[2, 3];
			return result;
		}

		public override bool Equals(object o)
		{
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			if (o is RigidTransform rigidTransform)
			{
				if (pos == rigidTransform.pos)
				{
					return rot == rigidTransform.rot;
				}
				return false;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return ((object)(Vector3)(ref pos)).GetHashCode() ^ ((object)(Quaternion)(ref rot)).GetHashCode();
		}

		public static bool operator ==(RigidTransform a, RigidTransform b)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			if (a.pos == b.pos)
			{
				return a.rot == b.rot;
			}
			return false;
		}

		public static bool operator !=(RigidTransform a, RigidTransform b)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			if (!(a.pos != b.pos))
			{
				return a.rot != b.rot;
			}
			return true;
		}

		public static RigidTransform operator *(RigidTransform a, RigidTransform b)
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			RigidTransform result = default(RigidTransform);
			result.rot = a.rot * b.rot;
			result.pos = a.pos + a.rot * b.pos;
			return result;
		}

		public void Inverse()
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			rot = Quaternion.Inverse(rot);
			pos = -(rot * pos);
		}

		public RigidTransform GetInverse()
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			RigidTransform result = new RigidTransform(pos, rot);
			result.Inverse();
			return result;
		}

		public void Multiply(RigidTransform a, RigidTransform b)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			rot = a.rot * b.rot;
			pos = a.pos + a.rot * b.pos;
		}

		public Vector3 InverseTransformPoint(Vector3 point)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			return Quaternion.Inverse(rot) * (point - pos);
		}

		public Vector3 TransformPoint(Vector3 point)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			return pos + rot * point;
		}

		public static Vector3 operator *(RigidTransform t, Vector3 v)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			return t.TransformPoint(v);
		}

		public static RigidTransform Interpolate(RigidTransform a, RigidTransform b, float t)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			return new RigidTransform(Vector3.Lerp(a.pos, b.pos, t), Quaternion.Slerp(a.rot, b.rot, t));
		}

		public void Interpolate(RigidTransform to, float t)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			pos = Lerp(pos, to.pos, t);
			rot = Slerp(rot, to.rot, t);
		}
	}

	public delegate object SystemFn(CVRSystem system, params object[] args);

	public static Quaternion Slerp(Quaternion A, Quaternion B, float t)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Clamp(A.x * B.x + A.y * B.y + A.z * B.z + A.w * B.w, -1f, 1f);
		if (num < 0f)
		{
			((Quaternion)(ref B))._002Ector(0f - B.x, 0f - B.y, 0f - B.z, 0f - B.w);
			num = 0f - num;
		}
		float num4;
		float num5;
		if (1f - num > 0.0001f)
		{
			float num2 = Mathf.Acos(num);
			float num3 = Mathf.Sin(num2);
			num4 = Mathf.Sin((1f - t) * num2) / num3;
			num5 = Mathf.Sin(t * num2) / num3;
		}
		else
		{
			num4 = 1f - t;
			num5 = t;
		}
		return new Quaternion(num4 * A.x + num5 * B.x, num4 * A.y + num5 * B.y, num4 * A.z + num5 * B.z, num4 * A.w + num5 * B.w);
	}

	public static Vector3 Lerp(Vector3 A, Vector3 B, float t)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		return new Vector3(Lerp(A.x, B.x, t), Lerp(A.y, B.y, t), Lerp(A.z, B.z, t));
	}

	public static float Lerp(float A, float B, float t)
	{
		return A + (B - A) * t;
	}

	public static double Lerp(double A, double B, double t)
	{
		return A + (B - A) * t;
	}

	public static float InverseLerp(Vector3 A, Vector3 B, Vector3 result)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		return Vector3.Dot(result - A, B - A);
	}

	public static float InverseLerp(float A, float B, float result)
	{
		return (result - A) / (B - A);
	}

	public static double InverseLerp(double A, double B, double result)
	{
		return (result - A) / (B - A);
	}

	public static float Saturate(float A)
	{
		if (!(A < 0f))
		{
			if (!(A > 1f))
			{
				return A;
			}
			return 1f;
		}
		return 0f;
	}

	public static Vector2 Saturate(Vector2 A)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2(Saturate(A.x), Saturate(A.y));
	}

	public static float Abs(float A)
	{
		if (!(A < 0f))
		{
			return A;
		}
		return 0f - A;
	}

	public static Vector2 Abs(Vector2 A)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2(Abs(A.x), Abs(A.y));
	}

	private static float _copysign(float sizeval, float signval)
	{
		if (Mathf.Sign(signval) != 1f)
		{
			return 0f - Mathf.Abs(sizeval);
		}
		return Mathf.Abs(sizeval);
	}

	public static Quaternion GetRotation(this Matrix4x4 matrix)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		Quaternion val = default(Quaternion);
		val.w = Mathf.Sqrt(Mathf.Max(0f, 1f + matrix.m00 + matrix.m11 + matrix.m22)) / 2f;
		val.x = Mathf.Sqrt(Mathf.Max(0f, 1f + matrix.m00 - matrix.m11 - matrix.m22)) / 2f;
		val.y = Mathf.Sqrt(Mathf.Max(0f, 1f - matrix.m00 + matrix.m11 - matrix.m22)) / 2f;
		val.z = Mathf.Sqrt(Mathf.Max(0f, 1f - matrix.m00 - matrix.m11 + matrix.m22)) / 2f;
		val.x = _copysign(val.x, matrix.m21 - matrix.m12);
		val.y = _copysign(val.y, matrix.m02 - matrix.m20);
		val.z = _copysign(val.z, matrix.m10 - matrix.m01);
		return val;
	}

	public static Vector3 GetPosition(this Matrix4x4 matrix)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		float m = matrix.m03;
		float m2 = matrix.m13;
		float m3 = matrix.m23;
		return new Vector3(m, m2, m3);
	}

	public static Vector3 GetScale(this Matrix4x4 m)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Sqrt(m.m00 * m.m00 + m.m01 * m.m01 + m.m02 * m.m02);
		float num2 = Mathf.Sqrt(m.m10 * m.m10 + m.m11 * m.m11 + m.m12 * m.m12);
		float num3 = Mathf.Sqrt(m.m20 * m.m20 + m.m21 * m.m21 + m.m22 * m.m22);
		return new Vector3(num, num2, num3);
	}

	public static object CallSystemFn(SystemFn fn, params object[] args)
	{
		bool flag = !SteamVR.active && !SteamVR.usingNativeSupport;
		if (flag)
		{
			EVRInitError peError = EVRInitError.None;
			OpenVR.Init(ref peError, EVRApplicationType.VRApplication_Utility);
		}
		CVRSystem system = OpenVR.System;
		object result = ((system != null) ? fn(system, args) : null);
		if (flag)
		{
			OpenVR.Shutdown();
		}
		return result;
	}

	public static void TakeStereoScreenshot(uint screenshotHandle, GameObject target, int cellSize, float ipd, ref string previewFilename, ref string VRFilename)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected O, but got Unknown
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected O, but got Unknown
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Expected O, but got Unknown
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_059d: Unknown result type (might be due to invalid IL or missing references)
		//IL_05aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02de: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0351: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0360: Unknown result type (might be due to invalid IL or missing references)
		//IL_0369: Unknown result type (might be due to invalid IL or missing references)
		//IL_036e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0373: Unknown result type (might be due to invalid IL or missing references)
		//IL_0378: Unknown result type (might be due to invalid IL or missing references)
		//IL_0383: Unknown result type (might be due to invalid IL or missing references)
		//IL_0388: Unknown result type (might be due to invalid IL or missing references)
		//IL_038d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
		//IL_039d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03be: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03db: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0401: Unknown result type (might be due to invalid IL or missing references)
		//IL_0429: Unknown result type (might be due to invalid IL or missing references)
		//IL_042d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0432: Unknown result type (might be due to invalid IL or missing references)
		//IL_0434: Unknown result type (might be due to invalid IL or missing references)
		//IL_0438: Unknown result type (might be due to invalid IL or missing references)
		//IL_043d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0441: Unknown result type (might be due to invalid IL or missing references)
		//IL_044b: Unknown result type (might be due to invalid IL or missing references)
		//IL_044d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0451: Unknown result type (might be due to invalid IL or missing references)
		//IL_0453: Unknown result type (might be due to invalid IL or missing references)
		//IL_048f: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = new Texture2D(4096, 4096, (TextureFormat)5, false);
		Stopwatch stopwatch = new Stopwatch();
		Camera val2 = null;
		stopwatch.Start();
		Camera val3 = target.GetComponent<Camera>();
		if ((Object)(object)val3 == (Object)null)
		{
			if ((Object)(object)val2 == (Object)null)
			{
				val2 = new GameObject().AddComponent<Camera>();
			}
			val3 = val2;
		}
		Texture2D val4 = new Texture2D(2048, 2048, (TextureFormat)5, false);
		RenderTexture val5 = new RenderTexture(2048, 2048, 24);
		RenderTexture targetTexture = val3.targetTexture;
		bool orthographic = val3.orthographic;
		float fieldOfView = val3.fieldOfView;
		float aspect = val3.aspect;
		StereoTargetEyeMask stereoTargetEye = val3.stereoTargetEye;
		val3.stereoTargetEye = (StereoTargetEyeMask)0;
		val3.fieldOfView = 60f;
		val3.orthographic = false;
		val3.targetTexture = val5;
		val3.aspect = 1f;
		val3.Render();
		RenderTexture.active = val5;
		val4.ReadPixels(new Rect(0f, 0f, (float)((Texture)val5).width, (float)((Texture)val5).height), 0, 0);
		RenderTexture.active = null;
		val3.targetTexture = null;
		Object.DestroyImmediate((Object)(object)val5);
		SteamVR_SphericalProjection steamVR_SphericalProjection = ((Component)val3).gameObject.AddComponent<SteamVR_SphericalProjection>();
		Vector3 localPosition = target.transform.localPosition;
		Quaternion localRotation = target.transform.localRotation;
		Vector3 position = target.transform.position;
		Quaternion rotation = target.transform.rotation;
		Quaternion val6 = Quaternion.Euler(0f, ((Quaternion)(ref rotation)).eulerAngles.y, 0f);
		Transform transform = ((Component)val3).transform;
		int num = 1024 / cellSize;
		float num2 = 90f / (float)num;
		float num3 = num2 / 2f;
		RenderTexture val7 = new RenderTexture(cellSize, cellSize, 24);
		((Texture)val7).wrapMode = (TextureWrapMode)1;
		val7.antiAliasing = 8;
		val3.fieldOfView = num2;
		val3.orthographic = false;
		val3.targetTexture = val7;
		val3.aspect = aspect;
		val3.stereoTargetEye = (StereoTargetEyeMask)0;
		for (int i = 0; i < num; i++)
		{
			float num4 = 90f - (float)i * num2 - num3;
			int num5 = 4096 / ((Texture)val7).width;
			float num6 = 360f / (float)num5;
			float num7 = num6 / 2f;
			int num8 = i * 1024 / num;
			for (int j = 0; j < 2; j++)
			{
				if (j == 1)
				{
					num4 = 0f - num4;
					num8 = 2048 - num8 - cellSize;
				}
				for (int k = 0; k < num5; k++)
				{
					float num9 = -180f + (float)k * num6 + num7;
					int num10 = k * 4096 / num5;
					int num11 = 0;
					float num12 = (0f - ipd) / 2f * Mathf.Cos(num4 * ((float)Math.PI / 180f));
					for (int l = 0; l < 2; l++)
					{
						if (l == 1)
						{
							num11 = 2048;
							num12 = 0f - num12;
						}
						Vector3 val8 = val6 * Quaternion.Euler(0f, num9, 0f) * new Vector3(num12, 0f, 0f);
						transform.position = position + val8;
						Quaternion val9 = Quaternion.Euler(num4, num9, 0f);
						transform.rotation = val6 * val9;
						Vector3 val10 = val9 * Vector3.forward;
						float num13 = num9 - num6 / 2f;
						float num14 = num13 + num6;
						float num15 = num4 + num2 / 2f;
						float num16 = num15 - num2;
						float num17 = (num13 + num14) / 2f;
						float num18 = ((Mathf.Abs(num15) < Mathf.Abs(num16)) ? num15 : num16);
						Vector3 val11 = Quaternion.Euler(num18, num13, 0f) * Vector3.forward;
						Vector3 val12 = Quaternion.Euler(num18, num14, 0f) * Vector3.forward;
						Vector3 val13 = Quaternion.Euler(num15, num17, 0f) * Vector3.forward;
						Vector3 val14 = Quaternion.Euler(num16, num17, 0f) * Vector3.forward;
						Vector3 val15 = val11 / Vector3.Dot(val11, val10);
						Vector3 val16 = val12 / Vector3.Dot(val12, val10);
						Vector3 val17 = val13 / Vector3.Dot(val13, val10);
						Vector3 val18 = val14 / Vector3.Dot(val14, val10);
						Vector3 val19 = val16 - val15;
						Vector3 val20 = val18 - val17;
						float magnitude = ((Vector3)(ref val19)).magnitude;
						float magnitude2 = ((Vector3)(ref val20)).magnitude;
						float num19 = 1f / magnitude;
						float num20 = 1f / magnitude2;
						Vector3 uAxis = val19 * num19;
						Vector3 vAxis = val20 * num20;
						steamVR_SphericalProjection.Set(val10, num13, num14, num15, num16, uAxis, val15, num19, vAxis, val17, num20);
						val3.aspect = magnitude / magnitude2;
						val3.Render();
						RenderTexture.active = val7;
						val.ReadPixels(new Rect(0f, 0f, (float)((Texture)val7).width, (float)((Texture)val7).height), num10, num8 + num11);
						RenderTexture.active = null;
					}
					float flProgress = ((float)i * ((float)num5 * 2f) + (float)k + (float)(j * num5)) / ((float)num * ((float)num5 * 2f));
					OpenVR.Screenshots.UpdateScreenshotProgress(screenshotHandle, flProgress);
				}
			}
		}
		OpenVR.Screenshots.UpdateScreenshotProgress(screenshotHandle, 1f);
		previewFilename += ".png";
		VRFilename += ".png";
		val4.Apply();
		File.WriteAllBytes(previewFilename, val4.EncodeToPNG());
		val.Apply();
		File.WriteAllBytes(VRFilename, val.EncodeToPNG());
		if ((Object)(object)val3 != (Object)(object)val2)
		{
			val3.targetTexture = targetTexture;
			val3.orthographic = orthographic;
			val3.fieldOfView = fieldOfView;
			val3.aspect = aspect;
			val3.stereoTargetEye = stereoTargetEye;
			target.transform.localPosition = localPosition;
			target.transform.localRotation = localRotation;
		}
		else
		{
			val2.targetTexture = null;
		}
		Object.DestroyImmediate((Object)(object)val7);
		Object.DestroyImmediate((Object)(object)steamVR_SphericalProjection);
		stopwatch.Stop();
		Debug.Log((object)$"Screenshot took {stopwatch.Elapsed} seconds.");
		if ((Object)(object)val2 != (Object)null)
		{
			Object.DestroyImmediate((Object)(object)((Component)val2).gameObject);
		}
		Object.DestroyImmediate((Object)(object)val4);
		Object.DestroyImmediate((Object)(object)val);
	}
}
