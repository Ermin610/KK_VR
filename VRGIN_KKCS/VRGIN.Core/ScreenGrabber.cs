using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRGIN.Core;

public class ScreenGrabber : IScreenGrabber
{
	public delegate bool JudgingMethod(Camera camera);

	private IList<Camera> _Cameras = new List<Camera>();

	private HashSet<Camera> _CheckedCameras = new HashSet<Camera>();

	private JudgingMethod _Judge;

	public RenderTexture Texture { get; private set; }

	public int Height { get; private set; }

	public int Width { get; private set; }

	public static JudgingMethod FromList(IEnumerable<Camera> allowedCameras)
	{
		return (Camera camera) => allowedCameras.Contains(camera);
	}

	public static JudgingMethod FromList(params string[] allowedCameraNames)
	{
		return (Camera camera) => allowedCameraNames.Contains(((Object)camera).name);
	}

	public ScreenGrabber(int width, int height, JudgingMethod method)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		Texture = new RenderTexture(width, height, 24, (RenderTextureFormat)7);
		Width = width;
		Height = height;
		_Judge = method;
	}

	public bool Check(Camera camera)
	{
		return _Judge(camera);
	}

	public IEnumerable<RenderTexture> GetTextures()
	{
		yield return Texture;
	}

	public void OnAssign(Camera camera)
	{
	}
}
