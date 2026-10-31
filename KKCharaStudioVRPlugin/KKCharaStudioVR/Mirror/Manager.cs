using System.IO;
using System.Reflection;
using System;
using System.Collections.Generic;
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
			if (!refl) return false;
			if (refl.GetComponent<VRReflection>() != null) return true;

			var mirror = refl.gameObject;
			var renderer = mirror.GetComponent<Renderer>();
			if (!renderer) return false;
			var original = renderer.sharedMaterials;
			var hasReflection = false;
			foreach (var source in original)
				if (source && source.HasProperty("_ReflectionTex")) hasReflection = true;
			if (!hasReflection) return false;
			var material = Material();
			if (material == null) return false;

			var converted = (Material[])original.Clone();
			var owned = new List<Material>();
			VRReflection reflection = null;
			var assigningMaterials = false;
			try
			{
				for (var i = 0; i < original.Length; i++)
				{
					var source = original[i];
					if (!source || !source.HasProperty("_ReflectionTex")) continue;
					// Clone before changing the shader to retain per-mirror properties
					// without modifying shared source assets or non-mirror slots.
					var replacement = new Material(source);
					owned.Add(replacement);
					replacement.shader = material.shader;
					replacement.renderQueue = source.renderQueue;
					replacement.shaderKeywords = source.shaderKeywords;
					converted[i] = replacement;
				}
				reflection = mirror.AddComponent<VRReflection>();
				reflection.m_DisablePixelLights = refl.m_DisablePixelLights;
				reflection.m_ClipPlaneOffset = refl.m_ClipPlaneOffset;
				reflection.m_ReflectLayers = refl.m_ReflectLayers;
				reflection.SetOwnedMaterials(owned.ToArray());
				assigningMaterials = true;
				renderer.sharedMaterials = converted;
				// Keep the stock updater until every reflective slot is ready.
				Object.Destroy(refl);
				return true;
			}
			catch (Exception error)
			{
				if (reflection)
				{
					reflection.enabled = false;
					reflection.SetOwnedMaterials(null);
					Object.Destroy(reflection);
				}
				try
				{
					if (assigningMaterials) renderer.sharedMaterials = original;
				}
				finally
				{
					foreach (var replacement in owned) Object.Destroy(replacement);
				}
				VRLog.Error("Failed to convert mirror materials; keeping the stock reflection: {0}", error);
				return false;
			}
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
