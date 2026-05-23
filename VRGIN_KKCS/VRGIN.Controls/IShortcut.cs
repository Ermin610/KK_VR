using System;

namespace VRGIN.Controls;

public interface IShortcut : IDisposable
{
	void Evaluate();
}
