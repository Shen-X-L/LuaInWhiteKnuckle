using LuaInWhiteKnuckle.Extensions;
using LuaInWhiteKnuckle.Game;
using MoonSharp.Interpreter;
using System.Collections.Generic;
using UnityEngine;
using Coroutine = MoonSharp.Interpreter.Coroutine;

namespace LuaInWhiteKnuckle.Runtime;

public class LuaTask {
	public int Id;// task Id
	public Coroutine Coroutine;// 实际函数
	public float ResumeTime;// 运行时间
	public string DebugName;// debug用名称
	public object[] StartArgs;// 启动参数
	public bool Started;// 是否启动
	public bool IsKilled;// 是否强制停止
}

public class LuaTaskManager : MonoBehaviour {
	// 全局最大允许的并发 Lua 任务数超过此数直接熔断拒绝
	private const int MAX_CONCURRENT_TASKS = 500;

	// 简单脚本的指令阈值, 压低到 2000 足以完成距离计算和物品发放
	private const int MAX_INSTRUCTIONS_PER_FRAME = 2000;


	private List<LuaTask> _tasks = new List<LuaTask>(); // 任务列表
	private List<LuaTask> _deadList = new List<LuaTask>(16);// 结束任务列表

	// 提取名字时去重即可
	public List<string> TasksName {
		get {
			var names = new List<string>();
			for (int i = 0; i < _tasks.Count; i++) 
				if (!_tasks[i].IsKilled && !names.Contains(_tasks[i].DebugName))
					names.Add(_tasks[i].DebugName);
			
			return names;
		}
	}

	private int _nextTaskId;// 任务Id

	#region[Task任务]

	public bool AddTask(Coroutine luaCoroutine, string debugName, params object[] args) {
		// 并发防御
		if (_tasks.Count >= MAX_CONCURRENT_TASKS) {
			Plugin.Logger.LogWarning($"[沙箱限流] 当前 Lua 任务数已达上限 ({MAX_CONCURRENT_TASKS}), 已拒绝加载新脚本: {debugName}");
			return false;
		}

		_tasks.Add(new LuaTask {
			Id = _nextTaskId,
			Coroutine = luaCoroutine,
			ResumeTime = 0f,
			DebugName = debugName,
			StartArgs = args,
			Started = false,
			IsKilled = false,
		});

		++ _nextTaskId;

		return true;
	}

	/// <summary>
	/// 终止特定名字的Lua任务
	/// </summary>
	/// <param name="debugName"></param>
	private void KillTaskImpl(string debugName) {
		for (int i = 0; i < _tasks.Count; i++) 
			if (_tasks[i].DebugName == debugName) _tasks[i].IsKilled = true;
	}

	public static void KillTask(string debugName) => Plugin.luaTaskManager.KillTaskImpl(debugName);

	#endregion

	#region[可异步执行]

	/// <summary>
	/// 执行指定的 Lua 函数
	/// </summary>
	/// <param name="luaCode"></param>
	/// <param name="debugName"></param>
	private void ExecuteImpl(string luaCode, string debugName) {
		try {
			var script = Plugin.safeLuaSandbox.GetScript();
			// 在沙箱环境中编译代码, 包装在函数内以支持作用域隔离
			DynValue chunk = script.DoString($"return function()\n{luaCode}\nend");

			if (chunk.Type == DataType.Function) {
				// 创建协程
				Coroutine luaCoroutine = script.CreateCoroutine(chunk).Coroutine;
				// 加入管理队列
				AddTask(luaCoroutine, debugName);
			}
		} catch (SyntaxErrorException) {
			Plugin.Logger.LogError($"[脚本语法错误] 无法加载脚本 {debugName}");
		} catch (System.Exception ex) {
			Plugin.Logger.LogError($"[任务执行错误] 无法加载脚本 {debugName}: {ex.Message}");
		}
	}

	public static void Execute(string luaCode, string debugName) =>
		Plugin.luaTaskManager.ExecuteImpl(luaCode, debugName);

	/// <summary>
	/// 执行Lua的回调函数
	/// </summary>
	/// <param name="callback"></param>
	/// <param name="debugName"></param>
	/// <param name="args"></param>
	private void ExecuteImpl(Closure callback, string debugName, params object[] args) {
		if (callback == null) return;
		if (callback.OwnerScript == null) {
			Plugin.Logger.LogError($"[任务执行错误] 无法加载脚本 {debugName}: 回调脚本环境为空");
			return;
		}
		Coroutine coroutine = callback.OwnerScript.CreateCoroutine(callback).Coroutine;
		AddTask(coroutine, debugName, args);
	}

	public static void Execute(Closure callback, string debugName, params object[] args) =>
		Plugin.luaTaskManager.ExecuteImpl(callback, debugName, args);

	#endregion

	#region[立刻同步执行]

	#region[	返回DynValue结果]

	/// <summary>
	/// 同步执行 Lua Closure 并立即返回结果专供 Hook 拦截使用
	/// 带有严格的防死循环保护, 一旦触发死循环将被强制中断
	/// </summary>
	private DynValue InvokeSyncImpl(Closure callback, string debugName, params object[] args) {
		if (callback == null || callback.OwnerScript == null) {
			Plugin.Logger.LogError($"[Hook执行错误] 无法执行 Hook {debugName}: 回调为空");
			return DynValue.Nil;
		}

		try {
			// 将回调包装为一个独立的协程, 仅仅是为了施加指令数限制
			Coroutine coroutine = callback.OwnerScript.CreateCoroutine(callback).Coroutine;

			coroutine.AutoYieldCounter = MAX_INSTRUCTIONS_PER_FRAME;

			// 立即同步恢复执行
			DynValue result = coroutine.Resume(args);

			// 检查是否因为超出了指令限制而被强制挂起
			// (注意: Hook 中不应该使用主动的 coroutine.yield, 因为我们需要同步结果)
			if (result.Type == DataType.YieldRequest || coroutine.State == CoroutineState.Suspended) {
				Plugin.Logger.LogError($"[Hook安全拦截] Hook '{debugName}' 试图执行异步挂起或存在死循环指令超限, 已被强制熔断");
				// 视情况可以在这里调用 safeLuaSandbox.InitSandbox() 重置环境
				return DynValue.Nil;
			}

			return result;

		} catch (ScriptRuntimeException ex) {
			Plugin.Logger.LogError($"[Hook运行时错误] {debugName}: {ex.DecoratedMessage}");
			return DynValue.Nil;
		} catch (System.Exception ex) {
			Plugin.Logger.LogError($"[Hook系统错误] 驱动 {debugName} 时发生异常: {ex.Message}");
			return DynValue.Nil;
		}
	}

	public static DynValue InvokeSync(Closure callback, string debugName, params object[] args) =>
		Plugin.luaTaskManager.InvokeSyncImpl(callback, debugName, args);

	/// <summary>
	/// 同步执行 Lua Closure 并立即返回结果专供 Hook 拦截使用
	/// 带有严格的防死循环保护, 一旦触发死循环将被强制中断
	/// </summary>
	private DynValue InvokeSyncImplFast(Closure callback, params object[] args) {
		if (callback == null || callback.OwnerScript == null) {
			Plugin.Logger.LogError($"[Hook执行错误] 无法执行 Hook 回调为空");
			return DynValue.Nil;
		}

		try {
			// 将回调包装为一个独立的协程, 仅仅是为了施加指令数限制
			Coroutine coroutine = callback.OwnerScript.CreateCoroutine(callback).Coroutine;

			coroutine.AutoYieldCounter = MAX_INSTRUCTIONS_PER_FRAME;

			// 立即同步恢复执行
			DynValue result = coroutine.Resume(args);

			// 检查是否因为超出了指令限制而被强制挂起
			// (注意: Hook 中不应该使用主动的 coroutine.yield, 因为我们需要同步结果)
			if (result.Type == DataType.YieldRequest || coroutine.State == CoroutineState.Suspended) {
				Plugin.Logger.LogError($"[Hook安全拦截] Hook 试图执行异步挂起或存在死循环指令超限, 已被强制熔断");
				// 视情况可以在这里调用 safeLuaSandbox.InitSandbox() 重置环境
				return DynValue.Nil;
			}

			return result;

		} catch (ScriptRuntimeException ex) {
			Plugin.Logger.LogError($"[Hook运行时错误] 发生异常: {ex.DecoratedMessage}");
			return DynValue.Nil;
		} catch (System.Exception ex) {
			Plugin.Logger.LogError($"[Hook系统错误] 发生异常: {ex.Message}");
			return DynValue.Nil;
		}
	}

	public static DynValue InvokeSyncFast(Closure callback, params object[] args) =>
		Plugin.luaTaskManager.InvokeSyncImplFast(callback, args);

	#endregion

	#region[	返回泛型结果]

	/// <summary>
	/// 同步执行 通过泛型包装返回值
	/// </summary>
	private T InvokeSyncImpl<T>(Closure callback, string debugName, params object[] args) {
		DynValue value = InvokeSyncImpl(callback, debugName, args);
		return value.ToClr<T>();
	}

	public static T InvokeSync<T>(Closure callback, string debugName, params object[] args)=>
		Plugin.luaTaskManager.InvokeSyncImpl<T>(callback, debugName, args);

	/// <summary>
	/// 同步执行 通过泛型包装返回值
	/// </summary>
	private T InvokeSyncImplFast<T>(Closure callback, params object[] args) {
		DynValue value = InvokeSyncImplFast(callback, args);
		return value.ToClr<T>();
	}

	public static T InvokeSyncFast<T>(Closure callback, params object[] args) =>
		Plugin.luaTaskManager.InvokeSyncImplFast<T>(callback, args);

	#endregion

	#region[	返回是否执行正常]

	/// <summary>
	/// 同步执行 返回Lua脚本是否被正常执行
	/// </summary>
	private bool InvokeImpl(Closure callback, string debugName, params object[] args) {
		if (callback == null || callback.OwnerScript == null) {
			Plugin.Logger.LogError($"[Hook执行错误] 无法执行 Hook {debugName}: 回调为空");
			return false;
		}

		try {
			// 将回调包装为一个独立的协程, 仅仅是为了施加指令数限制
			Coroutine coroutine = callback.OwnerScript.CreateCoroutine(callback).Coroutine;

			coroutine.AutoYieldCounter = MAX_INSTRUCTIONS_PER_FRAME;

			// 立即同步恢复执行
			DynValue result = coroutine.Resume(args);

			// 检查是否因为超出了指令限制而被强制挂起
			// (注意: Hook 中不应该使用主动的 coroutine.yield, 因为我们需要同步结果)
			if (result.Type == DataType.YieldRequest || coroutine.State == CoroutineState.Suspended) {
				Plugin.Logger.LogError($"[Hook安全拦截] Hook '{debugName}' 试图执行异步挂起或存在死循环指令超限, 已被强制熔断");
				// 视情况可以在这里调用 safeLuaSandbox.InitSandbox() 重置环境
				return false;
			}

			return true;

		} catch (ScriptRuntimeException ex) {
			Plugin.Logger.LogError($"[Hook运行时错误] {debugName}: {ex.DecoratedMessage}");
			return false;
		} catch (System.Exception ex) {
			Plugin.Logger.LogError($"[Hook系统错误] 驱动 {debugName} 时发生异常: {ex.Message}");
			return false;
		}
	}

	public static bool Invoke(Closure callback, string debugName, params object[] args) =>
		Plugin.luaTaskManager.InvokeImpl(callback, debugName, args);

	/// <summary>
	/// 同步执行 返回Lua脚本是否被正常执行
	/// </summary>
	private bool InvokeImplFast(Closure callback, params object[] args) {
		if (callback == null || callback.OwnerScript == null) {
			Plugin.Logger.LogError($"[Hook执行错误] 无法执行 Hook 回调为空");
			return false;
		}

		try {
			// 将回调包装为一个独立的协程, 仅仅是为了施加指令数限制
			Coroutine coroutine = callback.OwnerScript.CreateCoroutine(callback).Coroutine;

			coroutine.AutoYieldCounter = MAX_INSTRUCTIONS_PER_FRAME;

			// 立即同步恢复执行
			DynValue result = coroutine.Resume(args);

			// 检查是否因为超出了指令限制而被强制挂起
			// (注意: Hook 中不应该使用主动的 coroutine.yield, 因为我们需要同步结果)
			if (result.Type == DataType.YieldRequest || coroutine.State == CoroutineState.Suspended) {
				Plugin.Logger.LogError($"[Hook安全拦截] Hook 试图执行异步挂起或存在死循环指令超限, 已被强制熔断");
				// 视情况可以在这里调用 safeLuaSandbox.InitSandbox() 重置环境
				return false;
			}

			return true;

		} catch (ScriptRuntimeException ex) {
			Plugin.Logger.LogError($"[Hook运行时错误] 发生异常: {ex.DecoratedMessage}");
			return false;
		} catch (System.Exception ex) {
			Plugin.Logger.LogError($"[Hook系统错误] 发生异常: {ex.Message}");
			return false;
		}
	}

	public static bool InvokeFast(Closure callback, params object[] args) =>
		Plugin.luaTaskManager.InvokeImplFast(callback, args);

	#endregion

	#endregion

	private void Update() {
		int taskCount = _tasks.Count;
		if (taskCount == 0) return;

		float currentTime = Time.time;

		for (int i = 0; i < taskCount; i++) {
			var task = _tasks[i];
			// 已标记删除或结束
			if (task.IsKilled || task.Coroutine.State == CoroutineState.Dead) continue;
			// 协程等待时间未到
			if (currentTime < task.ResumeTime) continue;
			// CPU 死循环防御: 每帧充能极少量的安全指令数
			task.Coroutine.AutoYieldCounter = MAX_INSTRUCTIONS_PER_FRAME;

			DynValue result;
			// 执行并获取结果
			try {
				if (!task.Started) {
					task.Started = true;
					result = task.Coroutine.Resume(task.StartArgs);
				} else result = task.Coroutine.Resume();
			} catch (ScriptRuntimeException ex) {
				Plugin.Logger.LogError($"[脚本执行错误] {task.DebugName}: {ex.DecoratedMessage}");
				task.IsKilled = true;
				continue;
			}

			// 结束执行
			if (task.Coroutine.State == CoroutineState.Dead) {
				continue;
			}

			// 超过执行次数的协程暂停
			if (result.Type == DataType.YieldRequest) {
				Plugin.Logger.LogError($"[沙箱控制] 脚本 {task.DebugName} 指令超限或疑似死循环");

				// 视情况决定是否要调用 Plugin.safeLuaSandbox.InitSandbox() 重置全局环境
				task.IsKilled = true;
				continue;
			}

			// 合法的协程挂起
			if (task.Coroutine.State == CoroutineState.Suspended) 
				task.ResumeTime = result.Type == DataType.Number 
					? currentTime + (float)result.Number: 0f;
		}

		// 集中清理垃圾
		_tasks.RemoveAll(t => t.IsKilled || t.Coroutine.State == CoroutineState.Dead);
	}
}