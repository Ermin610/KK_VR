using System;

namespace Leap.Unity;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ExecuteAfterAttribute : Attribute
{
	public readonly Type afterType;

	public ExecuteAfterAttribute(Type afterType)
	{
		this.afterType = afterType;
	}
}
