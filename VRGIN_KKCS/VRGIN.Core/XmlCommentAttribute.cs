using System;

namespace VRGIN.Core;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class XmlCommentAttribute : Attribute
{
	public string Value { get; set; }

	public XmlCommentAttribute(string value)
	{
		Value = value;
	}
}
