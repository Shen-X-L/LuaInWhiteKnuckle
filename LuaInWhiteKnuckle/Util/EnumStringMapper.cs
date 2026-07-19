using System;
using System.Collections.Generic;
using System.Text;

namespace LuaInWhiteKnuckle.Util;

/// <summary>
/// 枚举<->字符串自动转换
/// </summary>
/// <typeparam name="T"></typeparam>
public static class EnumStringMapper<T> where T : struct, Enum {
	// Enum -> String 缓存
	private static readonly Dictionary<T, string> _enumToString = new();
	// String -> Enum 缓存 (忽略大小写)
	private static readonly Dictionary<string, T> _stringToEnum = new(StringComparer.OrdinalIgnoreCase);

	static EnumStringMapper() {
		foreach (T val in Enum.GetValues(typeof(T))) {
			string strName = val.ToString();
			_enumToString[val] = strName;
			_stringToEnum[strName] = val;
		}
	}

	// 获取字符串
	public static string GetString(T value) {
		return _enumToString.TryGetValue(value, out var str) ? str : "Unknown";
	}

	// 还原为枚举,失败则返回默认值
	public static T GetEnum(string value, T defaultValue = default) {
		if (string.IsNullOrEmpty(value)) return defaultValue;
		return _stringToEnum.TryGetValue(value, out var result) ? result : defaultValue;
	}
}