using DG.Tweening.Plugins.Core.PathCore;
using LuaInWhiteKnuckle.Game;
using LuaInWhiteKnuckle.Registry;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using System;
using System.IO;
using System.Text;
using UnityEngine;


namespace LuaInWhiteKnuckle.Runtime;

public class SafeLuaSandbox {
	private Script _env;
	private LuaIncludeGuard _includeGuard = new LuaIncludeGuard();
	public ModRootApi Api { get; private set; }
	public bool IsInitialized { get; private set; } = false;

	#region[生命周期函数]
	public void InitSandbox() {
		if (_env != null) CloseSandbox();
		// 强制采用硬沙箱预设, 仅保留纯计算模块, 额外开放一个安全的时间库
		_env = new Script(CoreModules.Preset_HardSandbox | CoreModules.OS_Time);
		// 限制执行次数
		_env.Options.TailCallOptimizationThreshold = 10000;
		// 严格限制互操作策略为默认 (非白名单注册的类一律无法被 Lua 访问) 
		UserData.RegistrationPolicy = InteropRegistrationPolicy.Default;
		// 实例化当前沙箱生命周期内的 API 根节点
		Api = new ModRootApi(_env);
		// 把 Game 全局变量和 C# 实例绑定
		_env.Globals["print"] = new CallbackFunction((ctx, args) => {
			StringBuilder sb = new();
			sb.Append("[LuaInWK]");
			for (int i = 0; i < args.Count; i++) {
				sb.Append(args[i].ToPrintString());
			}
			Plugin.LogInfo(sb.ToString());
			return DynValue.Nil;
		});
		_env.Globals["com_print"] = new CallbackFunction((ctx, args) => {
			StringBuilder sb = new();
			sb.Append("[Lua]");
			for (int i = 0; i < args.Count; i++) {
				sb.Append(args[i].ToPrintString());
			}
			CommandConsole.Log(sb.ToString());
			return DynValue.Nil;
		});
		_includeGuard.Clear();
		// 注册给 MoonSharp / Lua 的 include 函数
		_env.Globals["include"] = (Action<string>)((filePath) => {
			// 尝试获取安全 Execute 权限
			using (var handle = _includeGuard.TryInclude(filePath, out string error)) {
				if (!string.IsNullOrEmpty(error)) {
					Plugin.LogWarning(error);
					return;
				}

				// 如果已经加载过或触发阻断，直接返回
				if (!handle.ShouldExecute) return;

				// 安全执行 Lua 文件
				if (Plugin.luaFileManager.TryReadLuaFile(filePath,out var luaScript)) {
					DynValue chunk = _env.DoString($"return function()\n{luaScript}\nend");
					DynValue moduleResult = LuaTaskManager.InvokeSyncFast(chunk.Function);
				} else {
					Plugin.LogError($"[include] 文件不存在: {filePath}");
				}
			} // using 结束时自动弹栈
		});
		IsInitialized = true;
	}

	/// <summary>
	/// 关闭并彻底清空当前沙箱环境
	/// </summary>
	public void CloseSandbox() {
		if (_env == null) return;
		// 清空 事件监听 和 HOOK
		Api?.Dispose();
		// 将 Api 和虚拟机置为空
		Api = null;
		_env = null;

		// 清空Perk模块缓存
		LuaPerkRegistry.Clear();

		// 提示: 此时这个 Lua 虚拟机由于在 C# 端没有任何常驻对象引用它了
		Plugin.Logger.LogInfo("[LuaInWK] Sandbox Closed");
		IsInitialized = false;
	}

	/// <summary>
	/// 重置沙箱
	/// </summary>
	public void ResetSandbox() {
		CloseSandbox();
		InitSandbox();
	}

	#endregion

	/// <summary>
	/// 公开注册接口
	/// </summary>
	public static void RegisterType<T>() {
		UserData.RegisterType<T>();
	}

	public Script GetScript() {
		return _env;
	}

	/// <summary>
	/// 执行 Main.lua 入口并注入场景上下文
	/// </summary>
	public void ExecuteMainScript(string sceneName) {
		if (_env == null || !IsInitialized) return;

		// 将当前场景名注入给全局变量，供 Lua 随时读取
		_env.Globals["CurrentScene"] = sceneName;

		// 获取 Main.lua 路径
		string mainPath = "Main.lua"; 

		if (Plugin.luaFileManager.TryReadLuaFile(mainPath,out var luaScript)) {
			// 可以直接执行 Main.lua, 或者传入 sceneName 作为入口参数
			DynValue chunk = _env.DoString($"return function(sceneName)\n{luaScript}\nend");
			LuaTaskManager.InvokeSyncFast(chunk.Function, sceneName);

			Plugin.LogInfo($"[Lua] Main.lua 已在场景 '{sceneName}' 中成功执行");
		} else {
			Plugin.LogWarning($"[Lua] 未找到入口文件: {mainPath}");
		}
	}
}