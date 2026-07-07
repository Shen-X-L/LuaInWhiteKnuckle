using LuaInWhiteKnuckle.Api;
using LuaInWhiteKnuckle.Collections;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LuaInWhiteKnuckle.Core;

[MoonSharpUserData]
public class ModRootApi {
	// 注入事件总线
	public ModEventBus Events { get; } = new ModEventBus();
	public ModHookBus Hooks { get; } = new ModHookBus();

	public ModRootApi(Script script) {
		script.Globals["Game"] = this;
		PluginRegistry.Build(script);
	}
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
		Plugin.LogTest("ModEventBus.On");
		if (callback == null) return;
		if (!_listeners.ContainsKey(eventName)) {
			_listeners[eventName] = new List<LuaEventListener>();
		}
		_listeners[eventName].Add(new LuaEventListener {
			Callback = callback,
			DebugName = listenerId
		});
		Plugin.gameWatcherManager.Enable(eventName, true);
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
				Plugin.gameWatcherManager.Enable(eventName, false);
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// 提供给 C# 游戏钩子(Harmony Patch等)调用的触发接口
	/// </summary>
	/// <param name="eventName">事件名</param>
	/// <param name="args">传递给 Lua 回调的参数</param>
	[MoonSharpHidden]
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

	[MoonSharpHidden]
	public static void TriggerEvent(string eventName, params object[] args) {
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

[MoonSharpUserData]
public class ModHookBus {
	internal class LuaHook {
		public string Id;
		public Closure Callback;
	}
	private Dictionary<string, LuaHook> _hooks = new ();

	public void Register(string hookName,string debugName ,Closure callback) {
		if (callback == null) return;
		_hooks[hookName] = new LuaHook { Id = debugName, Callback = callback };
	}

	public void Unregister(string hookName) {
		_hooks.Remove(hookName);
	}

	[MoonSharpHidden]
	public bool Contains(string hookName) {
		return _hooks.ContainsKey(hookName);
	}

	/// <summary>
	/// 提供给 C# 游戏钩子(Harmony Patch等)调用的触发接口
	/// </summary>
	/// <param name="hookName">钩子名</param>
	/// <param name="args">传递给 Lua 回调的参数</param>
	[MoonSharpHidden]
	public T Invoke<T>(string hookName, params object[] args) {
		if (!_hooks.TryGetValue(hookName, out var hook))
			return default;

		// 调用 TaskManager 的同步安全执行方法，而不是直接 Call
		DynValue result = Plugin.luaTaskManager.InvokeSync(hook.Callback, hook.Id, args);

		// 如果执行失败、被熔断，或者 Lua 显式返回了 nil，则返回默认值
		if (result == null || result.IsNil()) {
			return default;
		}

		try {
			// 将 Lua 的返回值安全地转换为 C# 期望的类型
			return result.ToObject<T>();
		} catch (Exception ex) {
			Plugin.Logger.LogError($"[Hook返回值转换错误] Hook '{hook.Id}' 返回了意外的类型，无法转换为 {typeof(T).Name}: {ex.Message}");
			return default;
		}
	}

	[MoonSharpHidden]
	public static T InvokeHook<T>(string eventName, params object[] args) {
		if (Plugin.safeLuaSandbox != null && Plugin.safeLuaSandbox.Api != null) {
			return Plugin.safeLuaSandbox.Api.Hooks.Invoke<T>(eventName, args);
		}
		return default;
	}
}
