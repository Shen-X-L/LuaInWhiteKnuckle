using MoonSharp.Interpreter;
using System.Collections.Generic;
using UnityEngine;
using Coroutine = MoonSharp.Interpreter.Coroutine;

namespace LuaInWhiteKnuckle.Core;

public class LuaTask {
	public Coroutine Coroutine;
	public float ResumeTime;
	public string DebugName;
	public object[] StartArgs;
	public bool Started;
}

public class LuaTaskManager : MonoBehaviour {
	// 全局最大允许的并发 Lua 任务数。超过此数直接熔断拒绝。
	private const int MAX_CONCURRENT_TASKS = 500;

	// 简单脚本的指令阈值，压低到 2000 足以完成距离计算和物品发放
	private const int MAX_INSTRUCTIONS_PER_FRAME = 2000;

	// 任务列表和死任务列表
	private Dictionary<string, LuaTask> _tasks = new Dictionary<string, LuaTask>();
	private List<LuaTask> _deadList = new List<LuaTask>(16);

	public List<string> TasksName { get { return new List<string>(_tasks.Keys); } }

	public bool AddTask(Coroutine luaCoroutine, string debugName, params object[] args) {
		// 并发防御
		if (_tasks.Count >= MAX_CONCURRENT_TASKS) {
			Plugin.Logger.LogWarning($"[沙箱限流] 当前 Lua 任务数已达上限 ({MAX_CONCURRENT_TASKS})，已拒绝加载新脚本: {debugName}");
			return false;
		}

		if (_tasks.ContainsKey(debugName)) {
			Plugin.Logger.LogWarning($"Lua任务 {debugName} 已存在");
			return false;
		}

		_tasks[debugName] = new LuaTask {
			Coroutine = luaCoroutine,
			ResumeTime = 0f,
			DebugName = debugName,
			StartArgs = args,
			Started= false
		};

		return true;
	}

	/// <summary>
	/// 终止特定名字的Lua任务
	/// </summary>
	/// <param name="debugName"></param>
	public void KillTask(string debugName) {
		if (_tasks.Remove(debugName, out var task)) {
			Plugin.Logger.LogInfo($"[脚本管理] 已手动终止脚本: {debugName}");
		}
	}

	/// <summary>
	/// 执行指定的 Lua 函数
	/// </summary>
	/// <param name="luaCode"></param>
	/// <param name="debugName"></param>
	public void Execute(string luaCode, string debugName) {
		try {
			var script = Plugin.safeLuaSandbox.GetScript();
			// 1. 在沙箱环境中编译代码，包装在函数内以支持作用域隔离
			DynValue chunk = script.DoString($"return function()\n{luaCode}\nend");

			if (chunk.Type == DataType.Function) {
				// 2. 创建协程
				Coroutine luaCoroutine = script.CreateCoroutine(chunk).Coroutine;

				// 3. 加入管理队列
				AddTask(luaCoroutine, debugName);
			}
		} catch (SyntaxErrorException) {
			Plugin.Logger.LogError($"[脚本语法错误] 无法加载脚本 {debugName}");
		} catch (System.Exception ex) {
			Plugin.Logger.LogError($"[任务执行错误] 无法加载脚本 {debugName}: {ex.Message}");
		}
	}

	public void Execute(Closure callback, string debugName, params object[] args) {
		if (callback == null) return;
		if (callback.OwnerScript == null) {
			Plugin.Logger.LogError($"[任务执行错误] 无法加载脚本 {debugName}: 回调脚本环境为空");
			return;
		}
		Coroutine coroutine = callback.OwnerScript.CreateCoroutine(callback).Coroutine;

		AddTask(coroutine, debugName, args);
	}

	private void Update() {
		if (_tasks.Count == 0) return;

		float currentTime = Time.time;
		_deadList.Clear();

		foreach (var kvp in _tasks) {
			var task = kvp.Value;

			if (currentTime < task.ResumeTime) continue;

			// 2. CPU 死循环防御：每帧充能极少量的安全指令数
			task.Coroutine.AutoYieldCounter = MAX_INSTRUCTIONS_PER_FRAME;

			DynValue result;

			try {
				if (!task.Started) {
					task.Started = true;
					result = task.Coroutine.Resume(task.StartArgs);
				} else {
					result = task.Coroutine.Resume();
				}
			} catch (ScriptRuntimeException ex) {
				Plugin.Logger.LogError($"[脚本执行错误] {task.DebugName}: {ex.DecoratedMessage}");
				_deadList.Add(task);
				continue;
			}

			// 结束执行
			if (task.Coroutine.State == CoroutineState.Dead) {
				_deadList.Add(task);
				continue;
			}

			// 超过执行次数的协程暂停
			if (result.Type == DataType.YieldRequest) {
				Plugin.Logger.LogError($"[沙箱安全击杀] 脚本 {task.DebugName} 指令超限或疑似死循环！");

				// 视情况决定是否要调用 Plugin.safeLuaSandbox.InitSandbox() 重置全局环境
				_deadList.Add(task);
				continue;
			}

			// 合法的协程挂起
			if (task.Coroutine.State == CoroutineState.Suspended) {
				if (result.Type == DataType.Number)
					task.ResumeTime = currentTime + (float)result.Number;
				else
					task.ResumeTime = 0f;
			}
		}

		// 集中清理垃圾
		if (_deadList.Count > 0) {
			for (int i = 0; i < _deadList.Count; i++) {
				_tasks.Remove(_deadList[i].DebugName);
			}
		}
	}
}