using System.Collections.Generic;
using System.Linq;
using VRGIN.Controls;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.Modes;

namespace KKCharaStudioVR;

internal class GenericSeatedMode : SeatedMode
{
	protected override IEnumerable<IShortcut> CreateShortcuts()
	{
		return base.CreateShortcuts().Concat(new IShortcut[1]
		{
			new MultiKeyboardShortcut(new KeyStroke("Ctrl+C"), new KeyStroke("Ctrl+C"), delegate
			{
				VR.Manager.SetMode<GenericStandingMode>();
			})
		});
	}

	protected override void CreateControllers()
	{
	}
}
