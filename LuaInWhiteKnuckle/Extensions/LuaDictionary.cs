using MoonSharp.Interpreter;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LuaInWhiteKnuckle.Collections;

[MoonSharpUserData]
public class LuaDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> {
	private readonly Dictionary<TKey, TValue> _dict;

	public LuaDictionary() { _dict = new Dictionary<TKey, TValue>(); }

	public LuaDictionary(Dictionary<TKey, TValue> dictionary) { 
		_dict = dictionary ?? new Dictionary<TKey, TValue>();
	}

	// ---------------- IEnumerable 实现 ----------------
	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dict.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	// ---------------- 基础属性 ----------------
	public int Count => _dict.Count;
	public bool IsEmpty => _dict.Count == 0;

	// 为 Lua 暴露单独的键值数组方便遍历
	public TKey[] Keys => _dict.Keys.ToArray();
	public TValue[] Values => _dict.Values.ToArray();

	// ---------------- 安全索引器 ----------------
	public TValue this[TKey key] {
		get {
			if (key == null) return default;
			// 如果找不到 Key,安全返回 default(TValue),Lua 接收到 nil
			return _dict.TryGetValue(key, out var val) ? val : default;
		}
		set {
			if (key == null) return;
			// Lua 如果执行 dict[key] = nil,代表删除该键
			if (value == null) {
				_dict.Remove(key);
			} else {
				_dict[key] = value;
			}
		}
	}

	// ---------------- 字典操作 ----------------
	public void Add(TKey key, TValue value) {
		if (key != null && value != null) {
			// 使用索引器赋值以替代 _dict.Add(),避免 Key 冲突抛出 ArgumentException
			_dict[key] = value;
		}
	}

	public bool Remove(TKey key) {
		if (key == null) return false;
		return _dict.Remove(key);
	}

	public bool ContainsKey(TKey key) {
		if (key == null) return false;
		return _dict.ContainsKey(key);
	}

	public bool ContainsValue(TValue value) {
		if (value == null) return false;
		return _dict.ContainsValue(value);
	}

	public void Clear() => _dict.Clear();
}