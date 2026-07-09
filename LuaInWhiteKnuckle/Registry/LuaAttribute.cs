using System;
using System.Collections.Generic;
using System.Text;

namespace LuaInWhiteKnuckle.Registry;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class LuaApiAttribute : Attribute {
	public string Name { get; }

	public LuaApiAttribute(string name) {
		Name = name;
	}
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class LuaDataAttribute : Attribute {
	public Type Type { get; }

	public LuaDataAttribute(Type type) {
		Type = type;
	}
}