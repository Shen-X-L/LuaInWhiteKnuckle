using MoonSharp.Interpreter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LuaInWhiteKnuckle.Collections;

[MoonSharpUserData]
public class LuaList<T> : IEnumerable<T> {
	private readonly List<T> _list;
	public IEnumerator<T> GetEnumerator()
	=> _list.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();

	public IEnumerable<T> Values() {
		return _list;
	}

	public IEnumerable<T> List => _list;

	public int Count => _list.Count;
	public int Length => _list.Count;
	public bool IsEmpty => _list.Count == 0;

	public T First => _list.Count > 0 ? _list[0] : default;
	public T Last => _list.Count > 0 ? _list[^1] : default;

	public T this[int index] {
		get => _list[index];
		set => _list[index] = value;
	}

	public T[] ToArray() => _list.ToArray();

	public void Add(T value) => _list.Add(value);

	public void Insert(int index, T value)
		=> _list.Insert(index, value);

	public bool Remove(T value)
		=> _list.Remove(value);

	public void RemoveAt(int index)
		=> _list.RemoveAt(index);

	public bool Contains(T value)
		=> _list.Contains(value);

	public int IndexOf(T value)
		=> _list.IndexOf(value);

	public void Clear()
		=> _list.Clear();

	public LuaList(List<T> list) {
		this._list = list;
	}

	public LuaList() {
		_list = new List<T>();
	}
}