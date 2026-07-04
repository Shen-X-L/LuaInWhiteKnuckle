using System;
using System.Collections.Generic;
using System.Text;

namespace LuaInWhiteKnuckle.Core;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class LuaApiAttribute : Attribute {
	public string Name { get; }

	public LuaApiAttribute(string name) {
		Name = name;
	}
}