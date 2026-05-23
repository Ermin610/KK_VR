using System.Collections.Generic;
using UnityEngine;

namespace VRGIN.Core;

public interface IScreenGrabber
{
	bool Check(Camera camera);

	IEnumerable<RenderTexture> GetTextures();

	void OnAssign(Camera camera);
}
