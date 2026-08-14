using System.IO;
using System.Reflection;
using UnityEngine;
using VRGIN.Core;
using Object = UnityEngine.Object;

namespace KKCharaStudioVR.Mirror
{
	// Replaces MirrorReflection with VRReflection so mirrors render per eye.
	internal class Manager
	{
		private const string ShaderResourceName = "KKCharaStudioVR.Mirror.mirror-shader";

		private Material _material;
		private bool _shaderFailed;

		// False when the mirror was left alone, so the caller can keep the stock reflection.
		public bool Fix(MirrorReflection refl)
		{
			if (refl.GetComponent<VRReflection>() != null) return true;

			var material = Material();
			if (material == null) return false;

			var mirror = refl.gameObject;
			Object.Destroy(refl);

			mirror.AddComponent<VRReflection>();
			mirror.GetComponent<Renderer>().material = material;
			return true;
		}

		private Material Material()
		{
			if (_material != null || _shaderFailed) return _material;

			_shaderFailed = true;
			var bundleData = ReadShaderBundle();
			if (bundleData == null)
			{
				VRLog.Error("Failed to read the mirror shader bundle");
				return null;
			}

			var shader = VRGIN.Helpers.UnityHelper.LoadFromAssetBundle<Shader>(bundleData, "Assets/MirrorReflection.shader");
			if (shader == null)
			{
				VRLog.Error("Failed to load the mirror shader");
				return null;
			}

			_material = new Material(shader);
			_shaderFailed = false;
			return _material;
		}

		private static byte[] ReadShaderBundle()
		{
			using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ShaderResourceName))
			{
				if (stream == null) return null;

				var data = new byte[stream.Length];
				var read = 0;
				while (read < data.Length)
				{
					var count = stream.Read(data, read, data.Length - read);
					if (count <= 0) return null;
					read += count;
				}

				return data;
			}
		}
	}
}
