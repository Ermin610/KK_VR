using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace VRGIN;

[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
[DebuggerNonUserCode]
[CompilerGenerated]
internal class Resource
{
	private static ResourceManager resourceMan;

	private static CultureInfo resourceCulture;

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static ResourceManager ResourceManager
	{
		get
		{
			if (resourceMan == null)
			{
				resourceMan = new ResourceManager("VRGIN.Resource", typeof(Resource).Assembly);
			}
			return resourceMan;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static CultureInfo Culture
	{
		get
		{
			return resourceCulture;
		}
		set
		{
			resourceCulture = value;
		}
	}

	internal static byte[] hands_5_3 => (byte[])ResourceManager.GetObject("hands_5_3", resourceCulture);

	internal static byte[] vrgin_5_0 => (byte[])ResourceManager.GetObject("vrgin_5_0", resourceCulture);

	internal static byte[] vrgin_5_2 => (byte[])ResourceManager.GetObject("vrgin_5_2", resourceCulture);

	internal static byte[] vrgin_5_3 => (byte[])ResourceManager.GetObject("vrgin_5_3", resourceCulture);

	internal static byte[] vrgin_5_4 => (byte[])ResourceManager.GetObject("vrgin_5_4", resourceCulture);

	internal static byte[] vrgin_5_5 => (byte[])ResourceManager.GetObject("vrgin_5_5", resourceCulture);

	internal static byte[] vrgin_5_6 => (byte[])ResourceManager.GetObject("vrgin_5_6", resourceCulture);

	internal Resource()
	{
	}
}
