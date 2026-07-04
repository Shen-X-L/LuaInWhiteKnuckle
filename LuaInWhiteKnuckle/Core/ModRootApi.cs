using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace LuaInWhiteKnuckle.Core;

public static class PluginRegistry {
	// API注册表
	private static readonly Dictionary<string, object> _apis = new();
	private static readonly HashSet<Assembly> _assemblies = new();
	public static void Initialize() {
		RegisterAssembly(typeof(PluginRegistry).Assembly);
	}

	/// <summary>
	/// 扫描程序集LuaApiAttribute特性
	/// </summary>
	/// <param name="assembly"></param>
	public static void RegisterAssembly(Assembly assembly) {
		if (!_assemblies.Add(assembly))
			return;

		Type[] types;
		try {
			types = assembly.GetTypes();
		} catch (ReflectionTypeLoadException ex) {
			Plugin.LogError($"[LuaInWK] 扫描程序集 {assembly.FullName} 失败");

			foreach (var loader in ex.LoaderExceptions) {
				Plugin.LogError(loader.ToString());
			}
			throw;
		}

		foreach (Type type in types) {
			var attr = type.GetCustomAttribute<LuaApiAttribute>();
			if (attr == null) continue;
			if (type.IsAbstract) continue;
			if (type.IsInterface) continue;
			if (type.GetConstructor(Type.EmptyTypes) == null) {
				Plugin.LogError($"[LuaInWK] {type.Name} 缺少无参构造函数");
				continue;
			}
			UserData.RegisterType(type);

			object instance = Activator.CreateInstance(type);

			_apis[attr.Name] = instance;
		}
	}

	// 基于路径注册 API 到 Lua 环境中,例如 "Game.Events" 会在 Lua 中创建 Game 表,并在其下创建 Events 表
	private static void RegisterPath(Script script,string path,object api) {
		string[] parts = path.Split('.');

		Table current = script.Globals;

		for (int i = 0; i < parts.Length - 1; i++) {
			DynValue value = current.Get(parts[i]);

			if (value.Type != DataType.Table) {
				Table table = new Table(script);

				current[parts[i]] = table;

				current = table;
			} else {
				current = value.Table;
			}
		}

		current[parts[^1]] = api;
	}

	public static void Register(string name, object api) {
		_apis[name] = api;
	}
	public static void Unregister(string name) {
		_apis.Remove(name);
	}
	public static void Build(Script script) {
		foreach (var kv in _apis)
			RegisterPath(script, kv.Key, kv.Value);
	}
}

[MoonSharpUserData]
public class ModRootApi {
	// 注入事件总线
	public ModEventBus Events { get; } = new ModEventBus();

	public ModRootApi(Script script) {}
}

[MoonSharpUserData]
public class ModEventBus {
	internal class LuaEventListener {
		public string DebugName;
		public Closure Callback;
	}
	// 存储事件名和对应的 Lua 回调函数列表
	private Dictionary<string, List<LuaEventListener>> _listeners = new();

	/// <summary>
	/// 暴露给 Lua 的注册接口<br/>
	/// Lua调用<br/>
	/// Game.Events.On("OnPlayerDamage","debug_print", function(amount, type, tags)<br/>
	/// com_print("受到了伤害: " .. amount .. " 类型: " .. type .. " 标签: " .. table.concat(tags, ", "))<br/>
	/// end)<br/>
	/// </summary>
	/// <param name="eventName"></param>
	/// <param name="listenerId"></param>
	/// <param name="callback"></param>
	public void On(string eventName, string listenerId, Closure callback) {
		Plugin.LogDebug("ModEventBus.On");
		if (callback == null) return;
		if (!_listeners.ContainsKey(eventName)) {
			_listeners[eventName] = new List<LuaEventListener>();
		}
		_listeners[eventName].Add(new LuaEventListener {
			Callback = callback,
			DebugName = listenerId
		});
	}

	/// <summary>
	/// 退订指定事件的一个监听
	/// Lua:
	/// Game.Events.Off("OnPlayerDamage", "debug_print")
	/// </summary>
	/// <param name="eventName"></param>
	/// <param name="debugName"></param>
	/// <returns>是否成功移除</returns>
	public bool Off(string eventName, string debugName) {
		if (!_listeners.TryGetValue(eventName, out var listeners))
			return false;

		for (int i = listeners.Count - 1; i >= 0; i--) {
			if (listeners[i].DebugName == debugName) {
				listeners.RemoveAt(i);

				// 没有监听器了，顺便删除事件
				if (listeners.Count == 0)
					_listeners.Remove(eventName);

				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// 获取订阅指定事件的所有监听器
	/// </summary>
	/// <param name="eventName"></param>
	/// <param name="listeners"></param>
	/// <returns></returns>
	public bool TryGetListeners(string eventName, out List<string> listeners) {
		if (!_listeners.TryGetValue(eventName, out var listenerList)) {
			listeners = null;
			return false;
		}
		listeners = new List<string>(listenerList.Count);
		foreach (var listener in listenerList) {
			listeners.Add(listener.DebugName);
		}
		return true;
	}

	/// <summary>
	/// 提供给 C# 游戏钩子(Harmony Patch等)调用的触发接口
	/// </summary>
	/// <param name="eventName">事件名</param>
	/// <param name="args">传递给 Lua 回调的参数</param>
	public void Trigger(string eventName, params object[] args) {
		if (!_listeners.TryGetValue(eventName, out var listeners))
			return;

		// 倒序遍历，防止执行过程中注销监听
		for (int i = listeners.Count - 1; i >= 0; i--) {
			var listener = listeners[i];

			if (listener.Callback == null) {
				listeners.RemoveAt(i);
				continue;
			}
			Plugin.luaTaskManager.Execute(listener.Callback, listener.DebugName, args);
		}

		// 清理空事件
		if (listeners.Count == 0)
			_listeners.Remove(eventName);
	}

	public static void TriggerLuaEvent(string eventName, params object[] args) {
		if (Plugin.safeLuaSandbox != null && Plugin.safeLuaSandbox.Api != null) {
			Plugin.safeLuaSandbox.Api.Events.Trigger(eventName, args);
		}
	}

	/// <summary>
	/// 热重载 Lua 脚本前，必须调用此方法彻底释放引用
	/// </summary>
	public void ClearAllListeners() {
		_listeners.Clear();
	}

	/// <summary>
	/// 清空某事件的全部监听
	/// </summary>
	/// <param name="eventName"></param>
	public void Clear(string eventName) {
		_listeners.Remove(eventName);
	}
}

