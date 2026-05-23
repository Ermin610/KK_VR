using System.Collections.Generic;

namespace VRGIN.Visuals;

public static class GUIQuadRegistry
{
	private static HashSet<GUIQuad> _Quads = new HashSet<GUIQuad>();

	public static IEnumerable<GUIQuad> Quads => _Quads;

	internal static void Register(GUIQuad quad)
	{
		_Quads.Add(quad);
	}

	internal static void Unregister(GUIQuad quad)
	{
		_Quads.Remove(quad);
	}
}
