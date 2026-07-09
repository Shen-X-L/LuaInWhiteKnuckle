using LuaInWhiteKnuckle.Game;
using MoonSharp.Interpreter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LuaInWhiteKnuckle.Collections;

[MoonSharpUserData]
public class LuaList<T> : IEnumerable<T> {
	private readonly List<T> _list;
	public LuaList() { _list = new List<T>(); }

	public LuaList(List<T> list) { _list = list ?? new List<T>(); }
	// ---------------- IEnumerable 实现 ----------------
	public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	public IEnumerable<T> List => _list;

	// ---------------- 基础属性 ----------------
	public int Count => _list.Count;
	public int Length => _list.Count;
	public bool IsEmpty => _list.Count == 0;

	public T First => _list.Count > 0 ? _list[0] : default;
	public T Last => _list.Count > 0 ? _list[^1] : default;

	// ---------------- 安全索引器 ----------------
	// Lua 化 (1-based) 需要写 index - 1
	public T this[int index] {
		get {
			if (index < 0 || index >= _list.Count) return default; // 越界安全返回 nil
			return _list[index];
		}
		set {
			if (index >= 0 && index < _list.Count) {
				_list[index] = value;
			} else {
				Plugin.Logger.LogWarning($"[LuaList] 尝试向越界索引 {index} 写入数据被拦截");
			}
		}
	}
	// ---------------- 集合操作 ----------------
	public T[] ToArray() => _list.ToArray();
	public void Add(T value) => _list.Add(value);

	public void Insert(int index, T value) {
		if (index < 0 || index > _list.Count) return; // 容错处理
		_list.Insert(index, value);
	}

	public bool Remove(T value) => _list.Remove(value);

	public void RemoveAt(int index) {
		if (index < 0 || index >= _list.Count) return; // 容错处理
		_list.RemoveAt(index);
	}

	public bool Contains(T value) => _list.Contains(value);
	public int IndexOf(T value) => _list.IndexOf(value);
	public void Clear() => _list.Clear();
}