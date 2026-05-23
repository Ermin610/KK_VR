using System;

namespace Leap.Unity;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ExecuteBeforeAttribute : Attribute
{
	public readonly Type beforeType;

	public ExecuteBeforeAttribute(Type beforeType)
	{
		this.beforeType = beforeType;
	}
}
